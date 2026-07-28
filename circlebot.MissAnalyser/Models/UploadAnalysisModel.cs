namespace circlebot.MissAnalyser.Models;

public class UploadAnalysisModel
{
    public IFormFile? Replay { get; init; }

    /// <summary>Filename in the shared replay cache volume (e.g. "12345-3.osr"), used instead of Replay
    /// when Backend has already fetched and cached this replay.</summary>
    public string? ReplayCacheFile { get; init; }

    public IFormFile? Replay2 { get; init; }
    public string? Replay2CacheFile { get; init; }
    public required string BeatmapMd5 { get; init; }
    public string? BeatmapMd52 { get; init; }
    public bool IsStable { get; init; } = true;
}
