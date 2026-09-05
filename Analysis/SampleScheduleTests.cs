using System;

// Standalone test entry point for the exact production SampleSchedule source.
public static class SampleScheduleTests
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    public static void Main()
    {
        var s = new SampleSchedule();
        s.Reset(0, 10);
        Check(s.TrySample(0), "First sample");
        Check(!s.TrySample(0), "Duplicate time must not emit");
        Check(!s.TrySample(0.05), "Early observation must not emit");
        Check(s.TrySample(0.35), "A stall emits one observation");
        Check(s.MissedDeadlines == 2, "Stall must count two missed deadlines");
        Check(!s.TrySample(0.35), "Stall must not backfill");
        Check(s.TrySample(0.4), "Resume deadline grid");
        Check(!s.TrySample(0.3), "Reject clock reversal");
        Check(!s.TrySample(double.NaN), "Reject NaN");
        Check(!s.TrySample(double.PositiveInfinity), "Reject infinity");
        s.Reset(100, 72);
        int count = 0;
        for (int i = 0; i < 600; i++) if (s.TrySample(100+i/60.0)) count++;
        Check(count == 600, "60 updates/sec cannot emit 72 observations/sec");
        Check(s.MissedDeadlines > 100, "Lower frame rate exposes missed deadlines");
        s.Reset(0, 72);
        count = 0;
        for (int i = 0; i < 1440; i++) if (s.TrySample(i/144.0)) count++;
        Check(count >= 719 && count <= 721, "144 updates/sec should approach 72 observations/sec");
        Check(s.MissedDeadlines == 0, "Higher frame rate should not miss deadlines");
        bool threw = false;
        try { s.Reset(0, 0); } catch (ArgumentOutOfRangeException) { threw = true; }
        Check(threw, "Invalid rate must throw");
        Console.WriteLine("SampleSchedule: all assertions passed.");
    }
}
