namespace circlebot.MissAnalyser.Analysis;

public static class ModeHitErrors
{
    public static double TaikoGreatWindow(double od) => Math.Clamp(50 - 3 * od, 20, 50);

    public static double ManiaGreatWindow(double od) => 64 - 3 * od;

    // Greedily pairs each note (in time order) to the nearest unused press within the window.
    // Returns signed hit errors (press - note) for matched notes only.
    public static double[] Match(IReadOnlyList<double> noteTimes, IReadOnlyList<double> pressTimes, double window)
    {
        var errors = new List<double>();
        var used = new bool[pressTimes.Count];
        foreach (var note in noteTimes)
        {
            var bestIdx = -1;
            var bestAbs = double.MaxValue;
            for (var i = 0; i < pressTimes.Count; i++)
            {
                if (used[i])
                    continue;
                var diff = pressTimes[i] - note;
                var abs = Math.Abs(diff);
                if (abs <= window && abs < bestAbs)
                {
                    bestAbs = abs;
                    bestIdx = i;
                }
            }
            if (bestIdx >= 0)
            {
                used[bestIdx] = true;
                errors.Add(pressTimes[bestIdx] - note);
            }
        }
        return errors.ToArray();
    }
}
