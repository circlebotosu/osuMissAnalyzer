using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OsuMissAnalyzer.Core;
using OsuMissAnalyzer.Server.Helpers;
using OsuMissAnalyzer.Server.Models;
using ReplayAPI;
using SixLabors.ImageSharp;
using SysFile = System.IO.File;

namespace OsuMissAnalyzer.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ReplayApiController(ILogger<ReplayApiController> logger) : ControllerBase
{
    private static Dictionary<string, MissAnalyzer> MissAnalyzers { get; } = new();
    private static Rectangle Area => new(0, 0, 512, 512);
    
    private readonly string beatmapsPath = Path.Combine(Path.Combine(Environment.CurrentDirectory, ".data"), "osu");
    private readonly string replaysPath = Path.Combine(Path.Combine(Environment.CurrentDirectory, ".data"), "osr");
    
    [HttpPost("/api/replay/{replayId}")]
    public async Task<IActionResult> UploadReplay([FromForm] UploadReplayModel request, [FromRoute]string replayId)
    {
        if (MissAnalyzers.ContainsKey(replayId))
        {
            logger.LogWarning("Rejecting replay {replayId}: already loaded", replayId);
            return BadRequest("found");
        }
        
        if (!Directory.Exists(beatmapsPath) || !Directory.Exists(replaysPath))
        {
            Directory.CreateDirectory(beatmapsPath);
            Directory.CreateDirectory(replaysPath);
        }
        
        var replayPath = Path.Combine(replaysPath, $"{replayId}.osr");
        logger.LogDebug("Replay path: {replayPath}", replayPath);
        if (!SysFile.Exists(replayPath))
        {
            await using var replayStream = request.Replay.OpenReadStream();
            await using var replayFile = SysFile.Create(replayPath);
            await replayStream.CopyToAsync(replayFile);
        }
        
        await using var beatmapStream = request.Beatmap.OpenReadStream();
        var beatmapBytes = new byte[beatmapStream.Length];
        var readAsync = await beatmapStream.ReadAsync(beatmapBytes);
        beatmapStream.Close();
        
        if (readAsync != beatmapStream.Length)
        {
            logger.LogError($"Failed to read beatmap {request.Beatmap.FileName}");
            return Problem("Failed to read beatmap");
        }
        
        var beatmapMd5 = CryptoHelper.GetMd5String(beatmapBytes);
        var beatmapPath = Path.Combine(beatmapsPath, $"{beatmapMd5}.osu");
        logger.LogDebug("Beatmap path: {beatmapPath}", beatmapPath);
        if (!SysFile.Exists(beatmapPath))
        {
            await using var beatmapFile = SysFile.Create(beatmapPath);
            await beatmapFile.WriteAsync(beatmapBytes);
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
        return Ok("uploaded");
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