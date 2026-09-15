using System;
using System.IO;
using UnityEngine;

/// <summary>Session lifecycle and export. UTC labels; monotonic elapsed time.</summary>
public class SessionManager : MonoBehaviour
{
    public string participantId = "P000";
    public string environmentId = "empty_room_test";
    public string condition = "test";
    public int trialNumber = 1;
    public TelemetryLogger telemetry;
    [Tooltip("Leave disabled for headset validation; start manually once tracking has settled.")]
    public bool autoStartOnPlay = false;
    public float autoStopAfterSeconds = 60f;

    private bool active;
    private bool pendingExport;
    private string sessionFolder;
    private Manifest manifest;
    private readonly SessionEvents events = new SessionEvents();
    private bool stopping;
    public bool IsActive => active;
    public bool HasPendingExport => pendingExport;
    public string LastSessionFolder => sessionFolder;
    public double ElapsedSeconds => telemetry != null ? telemetry.ElapsedSeconds : 0;
    public event Action<string> SessionStopping;

    public void RecordEvent(string eventType, int trial = 0, string payloadJson = "{}")
    {
        if (!active) throw new InvalidOperationException("Cannot log events outside a recording.");
        events.Add(ElapsedSeconds, eventType, trial, payloadJson);
    }

    [Serializable]
    private class Manifest
    {
        public string schema_version = "0.2.0";
        public string session_id, participant_id, environment_id, condition;
        public int trial_number;
        public string start_utc, end_utc;
        public double duration_sec;
        public float sample_rate_hz;
        public int sample_count;
        public long missed_sample_deadlines;
        public string coordinate_system = "Unity left-handed, Y-up, meters. Floor plane = X by Z.";
        public string rotation_format = "quaternion (x,y,z,w)";
        public string telemetry_file = "telemetry.csv";
        public string sampling_policy = "one_observation_per_LateUpdate_no_backfill";
        public string timestamp_source = "Time.realtimeSinceStartupAsDouble; application observation, not sensor capture";
        public string frame_semantics = "Unity Time.frameCount";
        public bool origin_reference_assigned;
        public string tracking_validity = "not_recorded; finite poses do not prove tracking validity";
        public string unity_version;
        public string events_file = "events.csv";
        public string events_schema_version = "0.1.0";
        public int event_count;
    }

    private void Start() { if (autoStartOnPlay) StartSession(); }

    [ContextMenu("Start Recording")]
    public void StartSession()
    {
        if (!Application.isPlaying || !isActiveAndEnabled)
        {
            Debug.LogWarning("[SessionManager] Enter Play Mode and enable this component before starting a recording.");
            return;
        }
        if (active) return;
        if (pendingExport) throw new InvalidOperationException("Retry StopSession to export the previous session first.");
        if (telemetry == null) throw new InvalidOperationException("Assign a TelemetryLogger before recording.");
        if (telemetry.IsLogging) throw new InvalidOperationException("Logger is already recording.");
        manifest = new Manifest {
            session_id = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N"),
            participant_id = participantId, environment_id = environmentId,
            condition = condition, trial_number = trialNumber,
            unity_version = Application.unityVersion,
            origin_reference_assigned = telemetry.xrOrigin != null
        };
        sessionFolder = Path.Combine(Application.persistentDataPath, "sessions", manifest.session_id);
        Directory.CreateDirectory(sessionFolder);
        telemetry.StartLogging();
        events.Clear();
        manifest.start_utc = DateTime.UtcNow.ToString("o");
        manifest.sample_rate_hz = telemetry.TargetRateHz;
        active = true;
        RecordEvent("session_started");
        Debug.Log($"[SessionManager] Recording to {sessionFolder}");
    }

    [ContextMenu("Stop Recording and Save")]
    public void StopSession()
    {
        StopSessionWithReason("operator_stop");
    }

    public void StopSessionWithReason(string reason)
    {
        if (stopping || (!active && !pendingExport)) return;
        if (active)
        {
            stopping = true;
            try
            {
                SessionStopping?.Invoke(reason);
                RecordEvent("session_stopped", 0, JsonUtility.ToJson(new StopPayload { reason = reason }));
            }
            finally { stopping = false; }
            telemetry.StopLogging();
            manifest.end_utc = DateTime.UtcNow.ToString("o");
            manifest.duration_sec = telemetry.ElapsedSeconds;
            manifest.sample_count = telemetry.SampleCount;
            manifest.missed_sample_deadlines = telemetry.MissedSampleCount;
            manifest.event_count = events.Count;
            active = false;
            pendingExport = true;
        }
        // Manifest last: its presence indicates these writes completed.
        // On failure the buffer survives and StopSession can retry.
        File.WriteAllText(Path.Combine(sessionFolder, "telemetry.csv"), telemetry.GetCsv());
        File.WriteAllText(Path.Combine(sessionFolder, "events.csv"), events.GetCsv());
        File.WriteAllText(Path.Combine(sessionFolder, "manifest.json"), JsonUtility.ToJson(manifest, true));
        pendingExport = false;
        Debug.Log($"[SessionManager] Session stopped. {manifest.sample_count} samples written to {sessionFolder}");
    }

    [Serializable] private class StopPayload { public string reason; }

    private void Update()
    {
        if (active && autoStopAfterSeconds > 0 && telemetry.ElapsedSeconds >= autoStopAfterSeconds)
            StopSessionWithReason("session_timeout");
    }
    private void OnApplicationQuit() { StopSessionWithReason("application_quit"); }
    private void OnDisable() { StopSessionWithReason("component_disabled"); }
}
