using circlebot.MissAnalyser.Analysis;
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
}
