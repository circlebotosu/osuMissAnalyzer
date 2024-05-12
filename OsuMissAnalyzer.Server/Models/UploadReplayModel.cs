namespace OsuMissAnalyzer.Server.Models;

public class UploadReplayModel
{
    public IFormFile Replay { get; set; } = null!;
    public IFormFile Beatmap { get; set; } = null!;
}