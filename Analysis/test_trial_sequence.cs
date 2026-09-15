using System;
using System.Collections.Generic;

public static class TrialSequenceTests
{
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); }
    public static void Main()
    {
        var events = new List<string>();
        var s = new TrialSequence(2, 3, 10, 5);
        s.Transition += (kind, trial, reason) => events.Add(kind + ":" + trial + ":" + reason);
        s.Begin(0); s.Begin(0); s.Next(1); s.Next(1); s.Complete(2);
        Check(s.State == TrialSequence.Phase.Countdown && s.Trial == 1, "Double clicks must not skip trials");
        s.Tick(4); Check(s.State == TrialSequence.Phase.Active, "Countdown");
        s.Complete(6); s.Complete(6);
        Check(s.State == TrialSequence.Phase.Rest && s.LastOutcome == "manual_confirmation", "Manual completion");
        s.Next(7); Check(s.Trial == 1, "Minimum rest");
        s.Tick(11); Check(s.State == TrialSequence.Phase.Ready, "Rest does not auto-start");
        s.Next(20); s.Tick(23); s.Complete(33);
        Check(s.State == TrialSequence.Phase.Finished && s.LastOutcome == "timeout", "Timeout wins at boundary");
        int eventCount = events.Count; s.Stop(34, "operator_stop");
        Check(events.Count == eventCount, "Final stop cannot duplicate terminal trial events");
        Check(events.FindAll(e => e.StartsWith("trial_started:")).Count == 2, "Exactly two trials started");
        Check(events.FindAll(e => e.StartsWith("trial_ended:")).Count == 2, "Exactly two trials ended");

        var cancelled = new TrialSequence(1, 3, 10, 0);
        events.Clear(); cancelled.Transition += (k,t,r) => events.Add(k);
        cancelled.Begin(0); cancelled.Next(0); cancelled.Stop(1, "operator_stop"); cancelled.Stop(2, "operator_stop");
        Check(events.Contains("trial_cancelled") && !events.Contains("trial_started"), "Cancelled countdown has no stimulus");
        Check(events.FindAll(e => e == "sequence_stopped").Count == 1, "Idempotent stop");
        var interrupted = new TrialSequence(1, 0, 10, 0);
        interrupted.Begin(0); interrupted.Next(0); interrupted.Tick(0); interrupted.Stop(1, "component_disabled");
        Check(interrupted.State == TrialSequence.Phase.Stopped, "Disable aborts active trial");
        var stall = new TrialSequence(1, 3, 10, 0);
        stall.Begin(0); stall.Next(0); stall.Tick(100);
        Check(stall.State == TrialSequence.Phase.Active && stall.Deadline == 110, "No fabricated past stimulus after stall");
        stall.Tick(111); Check(stall.LastOutcome == "timeout", "Late timeout");
        bool threw = false;
        try { new TrialSequence(0, 3, 10, 5); } catch (ArgumentOutOfRangeException) { threw = true; }
        Check(threw, "Invalid config rejected");
        threw = false; try { stall.Tick(0); } catch (ArgumentOutOfRangeException) { threw = true; }
        Check(threw, "Reversed clock rejected");

        var log = new SessionEvents();
        log.Add(0, "session_started", 0, "{}");
        log.Add(0, "note", 1, "{\"text\":\"comma, quote\\\" and newline\\n\"}");
        Check(log.Count == 2 && log.GetCsv().Contains("\"\"text\"\""), "CSV JSON escaping");
        threw = false; try { log.Add(-1, "note", 0, "{}"); } catch (ArgumentException) { threw = true; }
        Check(threw, "Invalid event time");
        log.Clear(); Check(log.Count == 0, "New session clears event stream");
        Console.WriteLine("TrialSequence and SessionEvents: all assertions passed.");
    }
}
