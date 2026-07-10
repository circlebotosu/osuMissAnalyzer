using ReplayAPI;

namespace circlebot.MissAnalyser.Analysis;

public static class ReplayFrameStats
{
    public static double[] Frametimes(IReadOnlyList<ReplayFrame> frames) =>
        frames.Where(f => f.TimeDiff > 0).Select(f => (double)f.TimeDiff).ToArray();

    // Hold durations (ms) for K1 and K2, from the frame a key transitions to pressed
    // until the frame it transitions to released. Unreleased-at-end presses are dropped.
    public static (double[] K1, double[] K2) HoldTimes(IReadOnlyList<ReplayFrame> frames)
    {
        var k1 = ExtractHolds(frames, Keys.K1);
        var k2 = ExtractHolds(frames, Keys.K2);
        return (k1, k2);
    }

    // Sorted timestamps of every K1/K2 key-down transition. Used for keytap comparison.
    public static double[] PressTimes(IReadOnlyList<ReplayFrame> frames)
    {
        var presses = new List<double>();
        var prev = Keys.None;
        foreach (var frame in frames)
        {
            if (frame.Keys.HasFlag(Keys.K1) && !prev.HasFlag(Keys.K1))
                presses.Add(frame.Time);
            if (frame.Keys.HasFlag(Keys.K2) && !prev.HasFlag(Keys.K2))
                presses.Add(frame.Time);
            prev = frame.Keys;
        }
        presses.Sort();
        return presses.ToArray();
    }

    private static double[] ExtractHolds(IReadOnlyList<ReplayFrame> frames, Keys key)
    {
        var holds = new List<double>();
        int? pressTime = null;
        var wasDown = false;
        foreach (var frame in frames)
        {
            var isDown = frame.Keys.HasFlag(key);
            if (isDown && !wasDown)
                pressTime = frame.Time;
            else if (!isDown && wasDown && pressTime.HasValue)
            {
                holds.Add(frame.Time - pressTime.Value);
                pressTime = null;
            }
            wasDown = isDown;
        }
        return holds.ToArray();
    }
}
