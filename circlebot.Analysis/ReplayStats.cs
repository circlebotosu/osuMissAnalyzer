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

    // Most common value.
    public static double Mode(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
            return 0;
        return values
            .GroupBy(v => v)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .First().Key;
    }

    // The player's real (gameplay-time) frame interval. RX floods a consistent ~1ms delta and AP
    // injects many sub-real interpolation frames; both wreck median AND mode. Real vsync frames sit
    // in a plausible window (~10-250fps), so take the mode there. Fall back to the raw mode only if
    // the window is empty (genuine ultra-high fps with no injected frames to filter out).
    public static double RealFrametime(IReadOnlyList<double> frametimes)
    {
        var plausible = frametimes.Where(f => f is >= 4 and <= 100).ToList();
        return Mode(plausible.Count > 0 ? plausible : frametimes);
    }
}
