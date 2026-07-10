using BMAPI.v1;
using ReplayAPI;

namespace circlebot.MissAnalyser.Analysis;

public static class ModeReplayReader
{
    public static double[] NoteTimes(Beatmap beatmap) =>
        beatmap.HitObjects.Select(h => (double)h.StartTime).OrderBy(t => t).ToArray();

    // Mania encodes held columns as a bitmask in the replay frame's X field.
    // A press is any column bit going 0 -> 1.
    public static double[] ManiaPressTimes(IReadOnlyList<ReplayFrame> frames)
    {
        var presses = new List<double>();
        var prevMask = 0;
        foreach (var frame in frames)
        {
            var mask = (int)frame.X;
            for (var col = 0; col < 18; col++)
            {
                var bit = 1 << col;
                if ((mask & bit) != 0 && (prevMask & bit) == 0)
                    presses.Add(frame.Time);
            }
            prevMask = mask;
        }
        presses.Sort();
        return presses.ToArray();
    }

    // Taiko encodes presses in the frame Keys bitfield. Keys flags overlap (K1 includes the
    // M1 bit), so count a press whenever any raw key bit newly turns on (bit-delta), not per-flag.
    public static double[] TaikoPressTimes(IReadOnlyList<ReplayFrame> frames)
    {
        var presses = new List<double>();
        var prev = 0;
        foreach (var frame in frames)
        {
            var cur = (int)frame.Keys;
            if ((cur & ~prev) != 0)
                presses.Add(frame.Time);
            prev = cur;
        }
        presses.Sort();
        return presses.ToArray();
    }
}
