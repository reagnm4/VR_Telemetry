using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// Samples raw pose data (position + rotation) for the HMD and both controllers
/// at a target rate bounded by Unity updates, and buffers it for export to CSV.
///
/// Design choices (read these, they matter for the analysis side):
/// - We log RAW poses only. Velocity, acceleration, pauses, head-turn rate, path
///   length, etc. are all derived later in Python. Do NOT compute features here.
/// - Rotation is stored as a quaternion (x,y,z,w), not Euler angles, to avoid
///   wrap-around and gimbal issues. Python derives forward vectors / yaw from it.
/// - Coordinate system is Unity's: left-handed, Y up, units = meters.
///   Top-down floor path in Python is X (right) vs Z (forward). NOT X vs Y.
/// - At most one observation per LateUpdate; missed deadlines are not backfilled.
///   Timestamps are monotonic application observation times, not sensor capture times.
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
    [Tooltip("Optional XR Origin root pose; recorded in world space.")]
    public Transform xrOrigin;

    [Header("Sampling")]
    [Tooltip("Target observations per second. Actual rate is bounded by Unity updates.")]
    public float sampleRateHz = 72f;

    public bool IsLogging { get; private set; }

    private double startTimeSec;
    private double stoppedElapsedSec;
    private readonly SampleSchedule schedule = new SampleSchedule();
    public double ElapsedSeconds => IsLogging
        ? Time.realtimeSinceStartupAsDouble - startTimeSec : stoppedElapsedSec;
    public float TargetRateHz { get; private set; }
    public long MissedSampleCount => schedule.MissedDeadlines;

    // In-memory buffer. For pilot-length sessions (a few minutes) this is fine.
    // At 72 Hz, ~10 min is ~43k rows, which is trivially small.
    private readonly List<string> rows = new List<string>();

    private const string Header =
        "t_sec,frame," +
        "hmd_px,hmd_py,hmd_pz,hmd_rx,hmd_ry,hmd_rz,hmd_rw," +
        "lc_px,lc_py,lc_pz,lc_rx,lc_ry,lc_rz,lc_rw," +
        "rc_px,rc_py,rc_pz,rc_rx,rc_ry,rc_rz,rc_rw," +
        "origin_px,origin_py,origin_pz,origin_rx,origin_ry,origin_rz,origin_rw";

    public void StartLogging()
    {
        if (IsLogging) return;
        if (float.IsNaN(sampleRateHz) || float.IsInfinity(sampleRateHz) || sampleRateHz <= 0)
            throw new System.ArgumentOutOfRangeException(nameof(sampleRateHz));
        rows.Clear();
        rows.Add(Header);
        TargetRateHz = sampleRateHz;
        stoppedElapsedSec = 0;
        startTimeSec = Time.realtimeSinceStartupAsDouble;
        schedule.Reset(startTimeSec, TargetRateHz);
        IsLogging = true;
    }

    public void StopLogging()
    {
        if (!IsLogging) return;
        stoppedElapsedSec = ElapsedSeconds;
        IsLogging = false;
        if (MissedSampleCount > 0)
            Debug.LogWarning($"[TelemetryLogger] {MissedSampleCount} sampling deadlines missed; no observations fabricated.");
    }

    private void LateUpdate()
    {
        if (!IsLogging) return;
        double now = Time.realtimeSinceStartupAsDouble;
        if (schedule.TrySample(now)) RecordSample(now);
    }

    private void RecordSample(double nowSec)
    {
        double t = nowSec - startTimeSec;
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder(256);

        sb.Append(t.ToString("R", ci)).Append(',');
        sb.Append(Time.frameCount).Append(',');

        AppendPose(sb, hmd, ci);
        AppendPose(sb, leftController, ci);
        AppendPose(sb, rightController, ci);
        AppendPose(sb, xrOrigin, ci);

        // AppendPose leaves a trailing comma; remove the final one.
        rows.Add(sb.ToString().TrimEnd(','));

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
