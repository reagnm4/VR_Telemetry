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
    public bool autoStartOnPlay = true;
    public float autoStopAfterSeconds = 60f;

    private bool active;
    private bool pendingExport;
    private string sessionFolder;
    private Manifest manifest;

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
    }

    private void Start() { if (autoStartOnPlay) StartSession(); }

    public void StartSession()
    {
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
        manifest.start_utc = DateTime.UtcNow.ToString("o");
        manifest.sample_rate_hz = telemetry.TargetRateHz;
        active = true;
        Debug.Log($"[SessionManager] Recording to {sessionFolder}");
    }

    public void StopSession()
    {
        if (!active && !pendingExport) return;
        if (active)
        {
            telemetry.StopLogging();
            manifest.end_utc = DateTime.UtcNow.ToString("o");
            manifest.duration_sec = telemetry.ElapsedSeconds;
            manifest.sample_count = telemetry.SampleCount;
            manifest.missed_sample_deadlines = telemetry.MissedSampleCount;
            active = false;
            pendingExport = true;
        }
        // Manifest last: its presence indicates these writes completed.
        // On failure the buffer survives and StopSession can retry.
        File.WriteAllText(Path.Combine(sessionFolder, "telemetry.csv"), telemetry.GetCsv());
        File.WriteAllText(Path.Combine(sessionFolder, "manifest.json"), JsonUtility.ToJson(manifest, true));
        pendingExport = false;
        Debug.Log($"[SessionManager] Session stopped. {manifest.sample_count} samples written to {sessionFolder}");
    }

    private void Update()
    {
        if (active && autoStopAfterSeconds > 0 && telemetry.ElapsedSeconds >= autoStopAfterSeconds)
            StopSession();
    }
    private void OnApplicationQuit() { StopSession(); }
    private void OnDisable() { StopSession(); }
}
