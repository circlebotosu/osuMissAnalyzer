using System.Diagnostics;
using circlebot.MissAnalyser.Helpers;
using circlebot.MissAnalyser.Models;
using Microsoft.AspNetCore.Mvc;
using OsuMissAnalyzer.Core;
using ReplayAPI;
using SixLabors.ImageSharp;
using SysFile = System.IO.File;

namespace circlebot.MissAnalyser.Controllers;

[ApiController]
[Route("[controller]")]
public class ReplayApiController(ILogger<ReplayApiController> logger) : ControllerBase
{
    private static Dictionary<string, MissAnalyzer> MissAnalyzers { get; } = new();
    private static Rectangle Area => new(0, 0, 512, 512);

    // just link the volume in docker
    private const string BeatmapsPath = "/beatmaps";
    private const string ReplaysPath = "/replays";

    [HttpPost("/api/replay/{replayId}")]
    public async Task<IActionResult> UploadReplay([FromForm] UploadReplayModel request, [FromRoute]string replayId)
    {
        if (MissAnalyzers.ContainsKey(replayId))
        {
            logger.LogWarning("Rejecting replay {replayId}: already loaded", replayId);
            return BadRequest("found");
        }
        
        if (!Directory.Exists(BeatmapsPath) || !Directory.Exists(ReplaysPath))
        {
            Directory.CreateDirectory(BeatmapsPath);
            Directory.CreateDirectory(ReplaysPath);
        }
        
        var replayPath = Path.Combine(ReplaysPath, $"{replayId}.osr");
        logger.LogDebug("Replay path: {replayPath}", replayPath);
        if (!SysFile.Exists(replayPath))
        {
            await using var replayStream = request.Replay.OpenReadStream();
            await using var replayFile = SysFile.Create(replayPath);
            await replayStream.CopyToAsync(replayFile);
        }
        
        var beatmapPath = Path.Combine(BeatmapsPath, $"{request.BeatmapMd5}.osu");
        logger.LogDebug("Beatmap path: {beatmapPath}", beatmapPath);
        if (!SysFile.Exists(beatmapPath))
        {
            // with circlebot's setup,
            // we don't have access to beatmaps from the client.
            return BadRequest("beatmap");
        }

        var replayLoader = new ServerReplayLoader(replayPath, beatmapPath);
        
        if (replayLoader.Replay.GameMode != GameModes.osu)
        {
            logger.LogWarning("Rejecting replay {replayId}: invalid game mode", replayId);
            return BadRequest("mode");
        }
        
        if (replayLoader.Replay.Mods.HasFlag(Mods.Relax | Mods.AutoPilot))
        {
            logger.LogWarning("Rejecting replay {replayId}: invalid mods", replayId);
            return BadRequest("mods");
        }

        
        if (replayLoader.Replay.CountMiss == 0)
        {
            logger.LogWarning("Rejecting replay {replayId}: no misses", replayId);
            return BadRequest("fc");
        }

        logger.LogInformation("Uploaded replay {replayId}!", replayId);
        var analyzer = new MissAnalyzer(replayLoader);
        MissAnalyzers[replayId] = analyzer;
        return Ok(analyzer.MissCount);
    }
    
    [HttpGet("/api/replay/{replayId}")]
    public async Task<IActionResult> GetReplayMiss([FromRoute]string replayId, [FromQuery]int index)
    {
        var sw = new Stopwatch();
        sw.Start();
        
        if (!MissAnalyzers.TryGetValue(replayId, out var analyzer))
        {
            logger.LogWarning("Rejecting miss request for {replayId}: not found", replayId);
            return BadRequest("missing");
        }

        if (index < 0 || index >= analyzer.MissCount)
        {
            logger.LogWarning("Rejecting miss request for {replayId}: index out of bounds", replayId);
            return BadRequest("index");
        }
        
        var miss = analyzer.DrawHitObject(index, Area);
        
        if (miss is null)
        {
            logger.LogWarning("Rejecting miss request for {replayId}: miss not found", replayId);
            return BadRequest("miss");
        }

        var ms = new MemoryStream();
        await miss.SaveAsPngAsync(ms);
        ms.Seek(0, SeekOrigin.Begin);
        sw.Stop();
        logger.LogInformation("Miss request for {replayId} took {time}ms", replayId, sw.ElapsedMilliseconds);
        return File(ms, "image/png");
    }
    
    [HttpDelete("/api/replay/{replayId}")]
    public IActionResult DeleteReplay([FromRoute]string replayId)
    {
        if (!MissAnalyzers.Remove(replayId))
        {
            logger.LogWarning("Rejecting delete request for {replayId}: not found", replayId);
            return BadRequest("missing");
        }
        
        logger.LogInformation("Deleted replay {replayId}", replayId);
        return Ok("deleted");
    }
}