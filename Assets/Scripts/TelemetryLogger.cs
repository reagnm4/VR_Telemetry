using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// Samples raw pose data (position + rotation) for the HMD and both controllers
/// at a fixed rate, independent of frame rate, and buffers it for export to CSV.
///
/// Design choices (read these, they matter for the analysis side):
/// - We log RAW poses only. Velocity, acceleration, pauses, head-turn rate, path
///   length, etc. are all derived later in Python. Do NOT compute features here.
/// - Rotation is stored as a quaternion (x,y,z,w), not Euler angles, to avoid
///   wrap-around and gimbal issues. Python derives forward vectors / yaw from it.
/// - Coordinate system is Unity's: left-handed, Y up, units = meters.
///   Top-down floor path in Python is X (right) vs Z (forward). NOT X vs Y.
/// - Sampling is decoupled from frame rate: samples are scheduled against the
///   same real-time clock used for timestamps, so effective rate matches the
///   target. Hitches > 1 s leave an honest gap (no stale duplicate poses).
///   Each row stores
///   its real elapsed time so Python never assumes even spacing.
///
/// Attach this to any GameObject (e.g. an empty object named "TelemetrySystem").
/// Drag the HMD camera + both controller transforms into the inspector slots.
/// </summary>
public class TelemetryLogger : MonoBehaviour
{
    [Header("Tracked transforms (drag these in)")]
    [Tooltip("The Main Camera under XR Origin (this is the headset).")]
    public Transform hmd;
    [Tooltip("Left controller transform under XR Origin.")]
    public Transform leftController;
    [Tooltip("Right controller transform under XR Origin.")]
    public Transform rightController;

    [Header("Sampling")]
    [Tooltip("Target samples per second. 72 matches Quest 2 default refresh. 50 is plenty.")]
    public float sampleRateHz = 72f;

    public bool IsLogging { get; private set; }

    /// <summary>If the gap since the last due sample exceeds this, resync instead of
    /// burst-recording stale duplicate poses. Real gaps stay visible in the data
    /// (the analyzer flags them via max dt) rather than being papered over.</summary>
    private const double HitchResyncSeconds = 1.0;

    private double startTimeSec;
    private double nextSampleDueSec;   // absolute time (same clock as timestamps)
    private double samplePeriod;
    private int frameCounter;
    private int hitchCount;

    // In-memory buffer. For pilot-length sessions (a few minutes) this is fine.
    // At 72 Hz, ~10 min is ~43k rows, which is trivially small.
    private readonly List<string> rows = new List<string>();

    private const string Header =
        "t_sec,frame," +
        "hmd_px,hmd_py,hmd_pz,hmd_rx,hmd_ry,hmd_rz,hmd_rw," +
        "lc_px,lc_py,lc_pz,lc_rx,lc_ry,lc_rz,lc_rw," +
        "rc_px,rc_py,rc_pz,rc_rx,rc_ry,rc_rz,rc_rw";

    public void StartLogging()
    {
        rows.Clear();
        rows.Add(Header);
        samplePeriod = 1.0 / Mathf.Max(1f, sampleRateHz);
        frameCounter = 0;
        hitchCount = 0;
        startTimeSec = Time.realtimeSinceStartupAsDouble;
        nextSampleDueSec = startTimeSec;
        IsLogging = true;
    }

    public void StopLogging()
    {
        IsLogging = false;
        if (hitchCount > 0)
            Debug.LogWarning($"[TelemetryLogger] {hitchCount} hitch(es) > {HitchResyncSeconds}s during session; gaps left in data (check max dt in analysis).");
    }

    private void Update()
    {
        if (!IsLogging) return;

        // Schedule against the SAME clock the timestamps use, so effective rate
        // always matches the target rate over any hitch-free stretch.
        double now = Time.realtimeSinceStartupAsDouble;

        // Editor stall / app pause: don't burst-write stale duplicate poses to
        // "catch up". Resync and leave an honest gap in the timestamps.
        if (now - nextSampleDueSec > HitchResyncSeconds)
        {
            hitchCount++;
            nextSampleDueSec = now;
        }

        while (now >= nextSampleDueSec)
        {
            RecordSample(now);
            nextSampleDueSec += samplePeriod;
        }
    }

    private void RecordSample(double nowSec)
    {
        double t = nowSec - startTimeSec;
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder(256);

        sb.Append(t.ToString("F5", ci)).Append(',');
        sb.Append(frameCounter).Append(',');

        AppendPose(sb, hmd, ci);
        AppendPose(sb, leftController, ci);
        AppendPose(sb, rightController, ci);

        // AppendPose leaves a trailing comma; remove the final one.
        rows.Add(sb.ToString().TrimEnd(','));
        frameCounter++;
    }

    private static void AppendPose(StringBuilder sb, Transform tf, CultureInfo ci)
    {
        if (tf == null)
        {
            // Missing transform writes NaNs so Python can detect it cleanly.
            sb.Append("NaN,NaN,NaN,NaN,NaN,NaN,NaN,");
            return;
        }
        Vector3 p = tf.position;
        Quaternion r = tf.rotation;
        sb.Append(p.x.ToString("F5", ci)).Append(',');
        sb.Append(p.y.ToString("F5", ci)).Append(',');
        sb.Append(p.z.ToString("F5", ci)).Append(',');
        sb.Append(r.x.ToString("F6", ci)).Append(',');
        sb.Append(r.y.ToString("F6", ci)).Append(',');
        sb.Append(r.z.ToString("F6", ci)).Append(',');
        sb.Append(r.w.ToString("F6", ci)).Append(',');
    }

    /// <summary>Returns the buffered CSV (header + rows) as one string.</summary>
    public string GetCsv()
    {
        return string.Join("\n", rows);
    }

    public int SampleCount => Mathf.Max(0, rows.Count - 1);
}
