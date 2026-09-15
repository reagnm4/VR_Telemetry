using System;

/// <summary>Target-rate deadlines; never invents observations to catch up.</summary>
public sealed class SampleSchedule
{
    private double start, period;
    private long nextIndex;
    private double lastObservation;
    public long MissedDeadlines { get; private set; }

    public void Reset(double startSeconds, double rateHz)
    {
        if (double.IsNaN(startSeconds) || double.IsInfinity(startSeconds) ||
            double.IsNaN(rateHz) || double.IsInfinity(rateHz) || rateHz <= 0)
            throw new ArgumentOutOfRangeException(nameof(rateHz));
        start = startSeconds;
        period = 1.0 / rateHz;
        nextIndex = 0;
        lastObservation = double.NegativeInfinity;
        MissedDeadlines = 0;
    }

    public bool TrySample(double now)
    {
        if (period <= 0) throw new InvalidOperationException("Reset before sampling.");
        if (double.IsNaN(now) || double.IsInfinity(now) || now <= lastObservation ||
            now < start + nextIndex * period) return false;
        long dueIndex = Math.Max(nextIndex, (long)Math.Floor((now - start) / period));
        MissedDeadlines += dueIndex - nextIndex;
        nextIndex = dueIndex + 1;
        lastObservation = now;
        return true;
    }
}
