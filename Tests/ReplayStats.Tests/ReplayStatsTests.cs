using System.Collections.Generic;
using circlebot.MissAnalyser.Analysis;
using ReplayAPI;
using Xunit;

namespace ReplayAnalysis.Tests;

public class ReplayStatsTests
{
    [Fact]
    public void UnstableRate_IsTenTimesStdDev()
    {
        // hit errors with known stddev of 2 -> UR 20
        var errors = new double[] { -2, -2, 2, 2 };
        Assert.Equal(20, ReplayStats.UnstableRate(errors), 3);
    }

    [Fact]
    public void UnstableRate_EmptyInput_ReturnsZero()
    {
        Assert.Equal(0, ReplayStats.UnstableRate(System.Array.Empty<double>()));
    }

    [Fact]
    public void ClockRate_DoubleTime_Is1point5()
    {
        Assert.Equal(1.5, ReplayStats.ClockRate(dt: true, ht: false));
    }

    [Fact]
    public void ClockRate_HalfTime_Is0point75()
    {
        Assert.Equal(0.75, ReplayStats.ClockRate(dt: false, ht: true));
    }

    [Fact]
    public void ClockRate_NoMods_Is1()
    {
        Assert.Equal(1.0, ReplayStats.ClockRate(dt: false, ht: false));
    }

    [Fact]
    public void Median_OddCount_ReturnsMiddle()
    {
        Assert.Equal(5, ReplayStats.Median(new double[] { 1, 5, 9 }));
    }

    [Fact]
    public void Median_EmptyInput_ReturnsZero()
    {
        Assert.Equal(0, ReplayStats.Median(System.Array.Empty<double>()));
    }

    [Fact]
    public void Mode_ReturnsMostCommonValue()
    {
        Assert.Equal(25, ReplayStats.Mode(new double[] { 25, 25, 25, 3, 4, 5, 6, 7 }));
    }

    [Fact]
    public void Mode_EmptyInput_ReturnsZero()
    {
        Assert.Equal(0, ReplayStats.Mode(System.Array.Empty<double>()));
    }

}

public class ReplayFrameStatsTests
{
    private static ReplayFrame Frame(int time, int timeDiff, Keys keys) =>
        new ReplayFrame { Time = time, TimeDiff = timeDiff, Keys = keys };

    [Fact]
    public void Frametimes_DropsNonPositiveTimeDiffs()
    {
        var frames = new List<ReplayFrame>
        {
            Frame(0, -12345, Keys.None),
            Frame(0, 0, Keys.None),
            Frame(16, 16, Keys.None),
            Frame(33, 17, Keys.None),
        };
        var result = ReplayFrameStats.Frametimes(frames);
        Assert.Equal(new double[] { 16, 17 }, result);
    }

    [Fact]
    public void HoldTimes_MeasuresPressToRelease_PerKey()
    {
        var frames = new List<ReplayFrame>
        {
            Frame(0, 1, Keys.K1),
            Frame(16, 16, Keys.K1 | Keys.K2),
            Frame(40, 24, Keys.K1),
            Frame(48, 8, Keys.None),
        };
        var (k1, k2) = ReplayFrameStats.HoldTimes(frames);
        Assert.Equal(new double[] { 48 }, k1);
        Assert.Equal(new double[] { 24 }, k2);
    }

    [Fact]
    public void HoldTimes_UnreleasedKeyAtEnd_Ignored()
    {
        var frames = new List<ReplayFrame>
        {
            Frame(0, 1, Keys.K1),
            Frame(16, 16, Keys.K1),
        };
        var (k1, _) = ReplayFrameStats.HoldTimes(frames);
        Assert.Empty(k1);
    }

    [Fact]
    public void PressTimes_RecordsEachKeyDownTransition_Sorted()
    {
        var frames = new List<ReplayFrame>
        {
            Frame(0, 1, Keys.K1),
            Frame(16, 16, Keys.K1 | Keys.K2),
            Frame(40, 24, Keys.None),
            Frame(48, 8, Keys.K1),
        };
        var result = ReplayFrameStats.PressTimes(frames);
        Assert.Equal(new double[] { 0, 16, 48 }, result);
    }
}

public class ModeHitErrorsTests
{
    [Fact]
    public void TaikoGreatWindow_OD5_Is35()
    {
        Assert.Equal(35, ModeHitErrors.TaikoGreatWindow(5), 3);
    }

    [Fact]
    public void ManiaGreatWindow_OD5_Is49()
    {
        Assert.Equal(49, ModeHitErrors.ManiaGreatWindow(5), 3);
    }

    [Fact]
    public void Match_PairsPressesToNotesWithinWindow()
    {
        var noteTimes = new double[] { 100, 200 };
        var pressTimes = new double[] { 105, 190 };
        var errors = ModeHitErrors.Match(noteTimes, pressTimes, window: 50);
        Assert.Equal(new double[] { 5, -10 }, errors);
    }

    [Fact]
    public void Match_PressOutsideWindow_NotCounted()
    {
        var errors = ModeHitErrors.Match(new double[] { 100 }, new double[] { 400 }, window: 50);
        Assert.Empty(errors);
    }
}

public class ModeReplayReaderTests
{
    private static ReplayFrame ManiaFrame(int time, int mask) =>
        new ReplayFrame { Time = time, TimeDiff = 1, X = mask };
    private static ReplayFrame TaikoFrame(int time, Keys keys) =>
        new ReplayFrame { Time = time, TimeDiff = 1, Keys = keys };

    [Fact]
    public void ManiaPressTimes_CountsPerColumnOnsets()
    {
        var frames = new List<ReplayFrame>
        {
            ManiaFrame(0, 1),
            ManiaFrame(16, 3),
            ManiaFrame(40, 0),
            ManiaFrame(48, 2),
        };
        Assert.Equal(new double[] { 0, 16, 48 }, ModeReplayReader.ManiaPressTimes(frames));
    }

    [Fact]
    public void TaikoPressTimes_CountsNewKeyBitsWithoutDoubleCounting()
    {
        var frames = new List<ReplayFrame>
        {
            TaikoFrame(0, Keys.K1),
            TaikoFrame(16, Keys.K1 | Keys.K2),
            TaikoFrame(40, Keys.None),
            TaikoFrame(48, Keys.M1),
        };
        Assert.Equal(new double[] { 0, 16, 48 }, ModeReplayReader.TaikoPressTimes(frames));
    }
}
