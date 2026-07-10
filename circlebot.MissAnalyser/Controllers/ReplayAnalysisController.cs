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

        var first = await LoadAsync(request.Replay, request.BeatmapMd5);
        if (first is null)
            return BadRequest("beatmap");

        LoadedReplay? second = null;
        if (request.Replay2 is not null && request.BeatmapMd52 is not null)
        {
            second = await LoadAsync(request.Replay2, request.BeatmapMd52);
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
        var avgFrametime = frametimes.Length > 0 ? frametimes.Average() : 0;

        double? ur = null;
        double? cvUr = null;
        if (!relax && replay.GameMode == GameModes.osu && first.Analyzer is not null)
        {
            var errors = first.Analyzer.hits
                .Select(h => (double)h.frame.Time - h.note.StartTime).ToList();
            if (errors.Count > 0)
            {
                ur = ReplayStats.UnstableRate(errors);
                cvUr = ur * clockRate;
            }
        }
        // taiko/mania UR is wired in Task 8. catch has no UR.

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

    private async Task<LoadedReplay?> LoadAsync(IFormFile file, string beatmapMd5)
    {
        await using var stream = file.OpenReadStream();
        var bytes = new byte[stream.Length];
        _ = await stream.ReadAsync(bytes);

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
