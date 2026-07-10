namespace circlebot.MissAnalyser.Models;

public class UploadAnalysisModel
{
    public required IFormFile Replay { get; init; }
    public IFormFile? Replay2 { get; init; }
    public required string BeatmapMd5 { get; init; }
    public string? BeatmapMd52 { get; init; }
    public bool IsStable { get; init; } = true;
}
