namespace circlebot.MissAnalyser.Analysis;

// Pure replay statistics. Primitive arrays only, so it is unit-testable in isolation.
public static class ReplayStats
{
    public static double UnstableRate(IReadOnlyList<double> hitErrors)
    {
        if (hitErrors.Count == 0)
            return 0;
        var mean = hitErrors.Average();
        var stdDev = Math.Sqrt(hitErrors.Average(e => (e - mean) * (e - mean)));
        return 10 * stdDev;
    }

    public static double ClockRate(bool dt, bool ht)
    {
        if (dt) return 1.5;
        if (ht) return 0.75;
        return 1.0;
    }

    public static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}
