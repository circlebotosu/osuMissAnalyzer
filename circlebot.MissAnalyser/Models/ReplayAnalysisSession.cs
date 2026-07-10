namespace circlebot.MissAnalyser.Models;

public class ReplayAnalysisSession
{
    public required LoadedReplay First { get; init; }
    public LoadedReplay? Second { get; init; }
    public required string Mode { get; init; } // "std" | "taiko" | "mania" | "catch"
    public double? Ur { get; init; }
    public double? CvUr { get; init; }
    public double AvgFrametime { get; init; }
    public double? MedianHoldTime { get; init; }
    public double ClockRate { get; init; } = 1.0;
    public bool Relax { get; init; }
    public bool HasSecondReplay => Second is not null;
}
