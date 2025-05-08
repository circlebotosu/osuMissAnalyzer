namespace circlebot.MissAnalyser.Models;

public class UploadReplayModel
{
    public IFormFile Replay { get; set; } = null!;
    public string BeatmapMd5 { get; set; } = null!;
}