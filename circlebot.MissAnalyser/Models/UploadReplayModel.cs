namespace circlebot.MissAnalyser.Models;

public class UploadReplayModel
{
    public IFormFile? Replay { get; set; }

    /// <summary>Filename in the shared replay cache volume (e.g. "12345-3.osr"), used instead of Replay
    /// when Backend has already fetched and cached this replay.</summary>
    public string? ReplayCacheFile { get; set; }

    public string BeatmapMd5 { get; set; } = null!;
    public bool IsStable { get; set; }
}