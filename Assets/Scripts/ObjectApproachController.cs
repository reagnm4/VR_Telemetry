using System;
using UnityEngine;

/// <summary>Connects the trial sequence to a stationary target and session events.</summary>
public class ObjectApproachController : MonoBehaviour
{
    public SessionManager session;
    public GameObject target;
    public int numberOfTrials = 3;
    public float countdownSeconds = 3;
    public float trialTimeoutSeconds = 30;
    public float minimumRestSeconds = 5;
    [Tooltip("Record physical, thumbstick, or mixed for this run. No automatic inference.")]
    public string locomotionMode = "unspecified";
    public TrialSequence Sequence { get; private set; }
    public string LastError { get; private set; } = "";
    public bool CanBegin => session != null && !session.IsActive && !session.HasPendingExport;

    [Serializable]
    private class Payload
    {
        public string reason;
        public Vector3 target_position_world, target_scale_world;
        public Quaternion target_rotation_world;
        public int planned_trials;
        public double countdown_seconds, timeout_seconds, minimum_rest_seconds;
        public string locomotion_mode;
    }

    private void OnEnable() { if (session != null) session.SessionStopping += BeforeStop; }
    private void Start() { if (target != null) target.SetActive(false); }
    private void OnDisable()
    {
        if (session != null)
        {
            if (session.IsActive) session.StopSessionWithReason("task_disabled");
            session.SessionStopping -= BeforeStop;
        }
        if (target != null) target.SetActive(false);
    }

    private void Safely(Action action)
    {
        try { action(); LastError = ""; }
        catch (Exception ex) { LastError = ex.Message; Debug.LogException(ex, this); }
    }

    [ContextMenu("Begin Object Approach Session")]
    public void BeginSession() => Safely(() =>
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !CanBegin) return;
        if (target == null || session.telemetry == null || session.telemetry.hmd == null)
            throw new InvalidOperationException("Assign target, telemetry and HMD before starting.");
        Sequence = new TrialSequence(numberOfTrials, countdownSeconds, trialTimeoutSeconds, minimumRestSeconds);
        Sequence.Transition += OnTransition;
        // The task owns session completion; use task timeout for individual trials.
        session.autoStopAfterSeconds = 0;
        session.StartSession();
        if (!session.IsActive) return;
        session.RecordEvent("task_configured", 0, JsonUtility.ToJson(MakePayload("object_approach_v1")));
        Sequence.Begin(session.ElapsedSeconds);
    });

    [ContextMenu("Ready - Start Next Trial")]
    public void NextTrial() => Safely(() => { if (session.IsActive) Sequence?.Next(session.ElapsedSeconds); });
    [ContextMenu("Confirm Trial Complete")]
    public void CompleteTrial() => Safely(() => { if (session.IsActive) Sequence?.Complete(session.ElapsedSeconds); });
    [ContextMenu("Stop Task and Save")]
    public void StopTask() => Safely(() => session.StopSessionWithReason("operator_stop"));

    private void Update()
    {
        if (session != null && session.IsActive && Sequence != null)
            Safely(() => Sequence.Tick(session.ElapsedSeconds));
    }
    private Payload MakePayload(string reason) => new Payload {
        reason = reason,
        target_position_world = target.transform.position,
        target_rotation_world = target.transform.rotation,
        target_scale_world = target.transform.lossyScale,
        planned_trials = Sequence.TrialCount, countdown_seconds = Sequence.CountdownSeconds,
        timeout_seconds = Sequence.TimeoutSeconds, minimum_rest_seconds = Sequence.RestSeconds,
        locomotion_mode = locomotionMode
    };
    private void OnTransition(string kind, int trial, string reason)
    {
        session.RecordEvent(kind, trial, JsonUtility.ToJson(MakePayload(reason)));
        if (kind == "trial_started")
        {
            target.SetActive(true);
            session.RecordEvent("target_appeared", trial, JsonUtility.ToJson(MakePayload("activated")));
        }
        if (kind == "trial_ended")
        {
            target.SetActive(false);
            session.RecordEvent("target_disappeared", trial, JsonUtility.ToJson(MakePayload(reason)));
        }
        if (kind == "sequence_finished") session.StopSessionWithReason("sequence_finished");
    }
    private void BeforeStop(string reason)
    {
        Sequence?.Stop(session.ElapsedSeconds, reason);
        if (target != null) target.SetActive(false);
    }
}
