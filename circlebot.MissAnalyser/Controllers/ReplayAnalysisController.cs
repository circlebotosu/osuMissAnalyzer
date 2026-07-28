using BMAPI.v1;
using circlebot.MissAnalyser.Analysis;
using circlebot.MissAnalyser.Helpers;
using circlebot.MissAnalyser.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using osuDodgyMomentsFinder;
using ReplayAPI;
using SysFile = System.IO.File;

namespace circlebot.MissAnalyser.Controllers;

[ApiController]
public class ReplayAnalysisController(ILogger<ReplayAnalysisController> logger) : ControllerBase
{
    private static MemoryCache Sessions { get; } = new(new MemoryCacheOptions());
    private static MemoryCacheEntryOptions CacheEntryOptions { get; } = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(10),
    };

    private const string BeatmapsPath = "/beatmaps";
    private const string ReplaysPath = "/replays";

    [HttpPost("/api/replay/analysis/{sessionId}")]
    public async Task<IActionResult> Upload([FromForm] UploadAnalysisModel request, [FromRoute] string sessionId)
    {
        if (Sessions.TryGetValue(sessionId, out _))
            return BadRequest("found");

        Directory.CreateDirectory(BeatmapsPath);
        Directory.CreateDirectory(ReplaysPath);

        var first = await LoadAsync(request.Replay, request.ReplayCacheFile, request.BeatmapMd5);
        if (first is null)
            return BadRequest("beatmap");

        LoadedReplay? second = null;
        if ((request.Replay2 is not null || request.Replay2CacheFile is not null) && request.BeatmapMd52 is not null)
        {
            second = await LoadAsync(request.Replay2, request.Replay2CacheFile, request.BeatmapMd52);
            if (second is null)
                return BadRequest("beatmap");
        }

        var replay = first.Replay;
        var mode = ModeName(replay.GameMode);
        var relax = replay.Mods.HasFlag(Mods.Relax);
        var clockRate = ReplayStats.ClockRate(
            replay.Mods.HasFlag(Mods.DoubleTime) || replay.Mods.HasFlag(Mods.NightCore),
            replay.Mods.HasFlag(Mods.HalfTime));

        var frametimes = ReplayFrameStats.Frametimes(replay.ReplayFrames);
        // RealFrametime filters RX/AP injected frames; divide by clock rate (deltas are gameplay-time).
        var avgFrametime = ReplayStats.RealFrametime(frametimes) / clockRate;

        double? ur = null;
        double? cvUr = null;
        if (!relax)
        {
            var od = first.Beatmap.OverallDifficulty;
            double[] errors = replay.GameMode switch
            {
                GameModes.osu => first.Analyzer?.hits
                    .Select(h => (double)h.frame.Time - h.note.StartTime).ToArray()
                    ?? Array.Empty<double>(),
                GameModes.Taiko => ModeHitErrors.Match(
                    ModeReplayReader.NoteTimes(first.Beatmap),
                    ModeReplayReader.TaikoPressTimes(replay.ReplayFrames),
                    ModeHitErrors.TaikoGreatWindow(od)),
                GameModes.Mania => ModeHitErrors.Match(
                    ModeReplayReader.NoteTimes(first.Beatmap),
                    ModeReplayReader.ManiaPressTimes(replay.ReplayFrames),
                    ModeHitErrors.ManiaGreatWindow(od)),
                _ => Array.Empty<double>(), // catch: no UR
            };
            if (errors.Length > 0)
            {
                ur = ReplayStats.UnstableRate(errors);
                // converted UR normalises to 1.0x timing: raw real-time UR divided by the clock rate
                cvUr = ur / clockRate;
            }
        }

        double? medianHoldTime = null;
        if (replay.GameMode == GameModes.osu)
        {
            var (k1, k2) = ReplayFrameStats.HoldTimes(replay.ReplayFrames);
            var all = k1.Concat(k2).ToArray();
            if (all.Length > 0)
                medianHoldTime = ReplayStats.Median(all);
        }

        var session = new ReplayAnalysisSession
        {
            First = first,
            Second = second,
            Mode = mode,
            Ur = ur,
            CvUr = cvUr,
            AvgFrametime = avgFrametime,
            MedianHoldTime = medianHoldTime,
            ClockRate = clockRate,
            Relax = relax,
        };
        Sessions.Set(sessionId, session, CacheEntryOptions);

        logger.LogInformation("Loaded analysis session {sessionId} ({mode})", sessionId, mode);
        return Ok(new
        {
            mode = session.Mode,
            ur = session.Ur,
            cvUr = session.CvUr,
            avgFrametime = session.AvgFrametime,
            medianHoldTime = session.MedianHoldTime,
            hasSecondReplay = session.HasSecondReplay,
            relax = session.Relax,
        });
    }

    [HttpGet("/api/replay/analysis/{sessionId}/frametime")]
    public IActionResult Frametime([FromRoute] string sessionId)
    {
        var session = GetSession(sessionId);
        if (session is null)
            return BadRequest("missing");
        // drop RX/AP injected sub-real frames so the graph shows the true distribution, and
        // normalise gameplay-time deltas to real wall-clock time so DT/HT graphs read correctly
        var frametimes = ReplayFrameStats.Frametimes(session.First.Replay.ReplayFrames)
            .Where(f => f >= 4)
            .Select(f => f / session.ClockRate).ToArray();
        return Ok(new { frametimes });
    }

    [HttpGet("/api/replay/analysis/{sessionId}/holdtime")]
    public IActionResult HoldTime([FromRoute] string sessionId)
    {
        var session = GetSession(sessionId);
        if (session is null)
            return BadRequest("missing");
        if (session.Mode != "std")
            return BadRequest("mode");
        var (k1, k2) = ReplayFrameStats.HoldTimes(session.First.Replay.ReplayFrames);
        return Ok(new { k1, k2 });
    }

    [HttpGet("/api/replay/analysis/{sessionId}/compare")]
    public IActionResult Compare([FromRoute] string sessionId)
    {
        var session = GetSession(sessionId);
        if (session is null)
            return BadRequest("missing");
        if (session.Mode != "std")
            return BadRequest("mode");
        if (session.Second is null)
            return BadRequest("missing");

        var comparator = new ReplayComparator(session.First.Replay, session.Second.Replay);
        var cursorSimilarity = comparator.compareReplays();

        var firstPresses = ReplayFrameStats.PressTimes(session.First.Replay.ReplayFrames);
        var secondPresses = ReplayFrameStats.PressTimes(session.Second.Replay.ReplayFrames);
        var count = Math.Min(firstPresses.Length, secondPresses.Length);
        var tapDiffs = new List<double>(count);
        for (var i = 0; i < count; i++)
            tapDiffs.Add(Math.Abs(firstPresses[i] - secondPresses[i]));
        var keytapSimilarity = ReplayStats.Median(tapDiffs);

        var verdict = (cursorSimilarity, keytapSimilarity) switch
        {
            ( < 12, < 15) => "very similar",
            ( < 30, < 40) => "similar",
            _ => "distinct",
        };
        return Ok(new { cursorSimilarity, keytapSimilarity, verdict });
    }

    private async Task<LoadedReplay?> LoadAsync(IFormFile? file, string? cacheFileName, string beatmapMd5)
    {
        var bytes = await ReplayInputHelper.ReadAsync(file, cacheFileName);
        if (bytes is null)
            return null;

        var md5 = CryptoHelper.GetMd5String(bytes);
        var replayPath = Path.Combine(ReplaysPath, $"{md5}.osr");
        if (!SysFile.Exists(replayPath))
            await SysFile.WriteAllBytesAsync(replayPath, bytes);

        var beatmapPath = Path.Combine(BeatmapsPath, $"{beatmapMd5}.osu");
        if (!SysFile.Exists(beatmapPath))
            return null;

        var replay = new Replay(replayPath);
        var beatmap = new Beatmap(beatmapPath);
        ReplayAnalyzer? analyzer = null;
        if (replay.GameMode == GameModes.osu)
        {
            // std hit-detection can throw on unusual maps; UR is optional, so degrade gracefully
            try { analyzer = new ReplayAnalyzer(beatmap, replay); }
            catch (Exception e) { logger.LogWarning(e, "ReplayAnalyzer failed for {md5}", beatmapMd5); }
        }
        return new LoadedReplay(replay, beatmap, analyzer);
    }

    internal static string ModeName(GameModes mode) => mode switch
    {
        GameModes.osu => "std",
        GameModes.Taiko => "taiko",
        GameModes.CtB => "catch",
        GameModes.Mania => "mania",
        _ => "std",
    };

    internal static ReplayAnalysisSession? GetSession(string sessionId) =>
        Sessions.Get<ReplayAnalysisSession>(sessionId);
}
