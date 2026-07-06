using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Owns the lifecycle of a recording session: assigns IDs, writes a JSON manifest,
/// tells the TelemetryLogger when to start/stop, and exports files to disk.
///
/// Files go to Application.persistentDataPath. On Quest 2 (Android) that is:
///   /sdcard/Android/data/&lt;your.package.name&gt;/files/sessions/&lt;session_id&gt;/
/// See README_SETUP.md for how to pull them off the headset.
///
/// FIRST TEST: leave autoStartOnPlay on and set autoStopAfterSeconds (e.g. 60).
/// Press Play, put on the headset, walk a 2x2 m square, and the files write
/// themselves when the timer ends. Fully hands-free, guaranteed to produce data.
/// Later you can wire StopSession() to a controller button via the Input System.
/// </summary>
public class SessionManager : MonoBehaviour
{
    [Header("Session metadata")]
    [Tooltip("Anonymous participant ID. Do NOT use real names. e.g. P001")]
    public string participantId = "P000";
    [Tooltip("Which environment this session ran in. e.g. empty_room_test")]
    public string environmentId = "empty_room_test";
    [Tooltip("Condition label for later analysis. e.g. neutral / eerie")]
    public string condition = "test";
    [Tooltip("Trial number if the same participant runs multiple sessions.")]
    public int trialNumber = 1;

    [Header("References")]
    public TelemetryLogger telemetry;

    [Header("Control")]
    public bool autoStartOnPlay = true;
    [Tooltip("Auto-stop and save after this many seconds. 0 = never (call StopSession yourself).")]
    public float autoStopAfterSeconds = 60f;

    private const string SchemaVersion = "0.1.0";

    private string sessionId;
    private DateTime startUtc;
    private bool active;
    private string sessionFolder;

    private void Start()
    {
        if (autoStartOnPlay) StartSession();
    }

    public void StartSession()
    {
        if (active) return;
        sessionId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + participantId;
        startUtc = DateTime.UtcNow;

        sessionFolder = Path.Combine(Application.persistentDataPath, "sessions", sessionId);
        Directory.CreateDirectory(sessionFolder);

        if (telemetry != null) telemetry.StartLogging();
        active = true;
        Debug.Log($"[SessionManager] Session started: {sessionId}\nWriting to: {sessionFolder}");
    }

    public void StopSession()
    {
        if (!active) return;
        active = false;
        if (telemetry != null) telemetry.StopLogging();

        DateTime endUtc = DateTime.UtcNow;

        File.WriteAllText(
            Path.Combine(sessionFolder, "telemetry.csv"),
            telemetry != null ? telemetry.GetCsv() : "");

        File.WriteAllText(
            Path.Combine(sessionFolder, "manifest.json"),
            BuildManifestJson(endUtc));

        int n = telemetry != null ? telemetry.SampleCount : 0;
        Debug.Log($"[SessionManager] Session stopped. {n} samples written to {sessionFolder}");
    }

    private string BuildManifestJson(DateTime endUtc)
    {
        var ci = CultureInfo.InvariantCulture;
        double durationSec = (endUtc - startUtc).TotalSeconds;
        float rate = telemetry != null ? telemetry.sampleRateHz : 0f;
        int count = telemetry != null ? telemetry.SampleCount : 0;

        return
"{\n" +
$"  \"schema_version\": \"{SchemaVersion}\",\n" +
$"  \"session_id\": \"{sessionId}\",\n" +
$"  \"participant_id\": \"{participantId}\",\n" +
$"  \"environment_id\": \"{environmentId}\",\n" +
$"  \"condition\": \"{condition}\",\n" +
$"  \"trial_number\": {trialNumber},\n" +
$"  \"start_utc\": \"{startUtc.ToString("o", ci)}\",\n" +
$"  \"end_utc\": \"{endUtc.ToString("o", ci)}\",\n" +
$"  \"duration_sec\": {durationSec.ToString("F3", ci)},\n" +
$"  \"sample_rate_hz\": {rate.ToString("F1", ci)},\n" +
$"  \"sample_count\": {count},\n" +
"  \"coordinate_system\": \"Unity left-handed, Y-up, meters. Floor plane = X (right) by Z (forward).\",\n" +
"  \"rotation_format\": \"quaternion (x,y,z,w)\",\n" +
"  \"telemetry_file\": \"telemetry.csv\"\n" +
"}\n";
    }

    private void Update()
    {
        if (!active) return;
        if (autoStopAfterSeconds > 0f &&
            (DateTime.UtcNow - startUtc).TotalSeconds >= autoStopAfterSeconds)
        {
            StopSession();
        }
    }

    private void OnApplicationQuit()
    {
        // Safety net: if the app closes mid-session, still try to save.
        if (active) StopSession();
    }
}
