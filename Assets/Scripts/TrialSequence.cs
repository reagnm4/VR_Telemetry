using System;

/// <summary>Pure trial state machine. The caller supplies session-relative time.</summary>
public sealed class TrialSequence
{
    public enum Phase { Idle, Ready, Countdown, Active, Rest, Finished, Stopped }
    public Phase State { get; private set; } = Phase.Idle;
    public int Trial { get; private set; }
    public string LastOutcome { get; private set; } = "";
    public double Deadline { get; private set; }
    public readonly int TrialCount;
    public readonly double CountdownSeconds, TimeoutSeconds, RestSeconds;
    private double lastTime = -1;
    public event Action<string, int, string> Transition;

    public TrialSequence(int trialCount, double countdown, double timeout, double rest)
    {
        if (trialCount < 1 || !Finite(countdown) || countdown < 0 ||
            !Finite(timeout) || timeout <= 0 || !Finite(rest) || rest < 0)
            throw new ArgumentOutOfRangeException("Trial configuration is invalid.");
        TrialCount = trialCount; CountdownSeconds = countdown;
        TimeoutSeconds = timeout; RestSeconds = rest;
    }
    private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    private void Observe(double now)
    {
        if (!Finite(now) || now < 0 || now < lastTime)
            throw new ArgumentOutOfRangeException(nameof(now));
        lastTime = now;
    }
    private void Emit(string kind, string reason = "") => Transition?.Invoke(kind, Trial, reason);

    public void Begin(double now)
    {
        Observe(now);
        if (State != Phase.Idle) return;
        State = Phase.Ready;
        Emit("session_ready");
    }
    public void Next(double now)
    {
        Observe(now);
        Tick(now);
        if (State != Phase.Ready) return;
        Trial++;
        State = Phase.Countdown;
        Deadline = now + CountdownSeconds;
        Emit("trial_countdown_started");
    }
    public void Tick(double now)
    {
        Observe(now);
        // One transition per tick: never fabricate a stimulus that should have
        // occurred during an application stall.
        if (State == Phase.Countdown && now >= Deadline)
        {
            State = Phase.Active;
            Deadline = now + TimeoutSeconds;
            Emit("trial_started");
        }
        else if (State == Phase.Active && now >= Deadline) End(now, "timeout");
        else if (State == Phase.Rest && now >= Deadline)
        {
            State = Phase.Ready;
            Emit("rest_ended");
        }
    }
    public void Complete(double now)
    {
        Observe(now);
        if (State != Phase.Active) return;
        // At the exact deadline, timeout takes precedence over confirmation.
        End(now, now >= Deadline ? "timeout" : "manual_confirmation");
    }
    private void End(double now, string outcome)
    {
        LastOutcome = outcome;
        State = Trial == TrialCount ? Phase.Finished : Phase.Rest;
        Deadline = now + RestSeconds;
        Emit("trial_ended", outcome);
        Emit(State == Phase.Finished ? "sequence_finished" : "rest_started", outcome);
    }
    public void Stop(double now, string reason)
    {
        Observe(now);
        if (State == Phase.Stopped || State == Phase.Finished || State == Phase.Idle) return;
        var previous = State;
        State = Phase.Stopped;
        if (previous == Phase.Active) Emit("trial_ended", reason);
        else if (previous == Phase.Countdown) Emit("trial_cancelled", reason);
        Emit("sequence_stopped", reason);
    }
}
