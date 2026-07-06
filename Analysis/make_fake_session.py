"""
make_fake_session.py

Generates a SYNTHETIC session (manifest.json + telemetry.csv) that mimics what
TelemetryLogger.cs / SessionManager.cs write, so the Python analysis pipeline
can be tested end-to-end BEFORE any real headset data exists.

The fake participant walks a 2x2 m square (the square test), with:
  - ~72 Hz sampling with realistic timing jitter (t_sec is NOT evenly spaced)
  - head-bob on Y and small tracking noise on X/Z
  - a short controller-tracking dropout (NaN rows) to exercise NaN handling
  - quaternion head rotation that faces the walking direction

Expected analyzer output on this data:
  X span ~2.0 m, Z span ~2.0 m, effective rate ~72 Hz, square-ish top-down plot.

Usage:
    python make_fake_session.py [output_dir]     # default ./sessions
Then:
    python analyze_session.py ./sessions/<printed_session_id>
"""

import json
import sys
from datetime import datetime, timedelta, timezone
from pathlib import Path

import numpy as np

RATE_HZ = 72.0
WALK_SPEED = 0.8          # m/s, casual walking pace
SQUARE = 2.0              # side length in meters
EYE_HEIGHT = 1.68         # m
RNG = np.random.default_rng(42)


def yaw_to_quat(yaw_rad):
    """Unity-style quaternion (x,y,z,w) for a rotation of yaw around Y (up)."""
    half = yaw_rad / 2.0
    return 0.0, np.sin(half), 0.0, np.cos(half)


def build_path():
    """Waypoints of a 2x2 m square starting at origin, Unity floor plane X/Z."""
    corners = [(0, 0), (SQUARE, 0), (SQUARE, SQUARE), (0, SQUARE), (0, 0)]
    side_time = SQUARE / WALK_SPEED
    total_time = 4 * side_time + 4.0  # + brief pauses at corners

    ts, xs, zs, yaws = [], [], [], []
    t = 0.0
    dt_nominal = 1.0 / RATE_HZ

    for i in range(4):
        (x0, z0), (x1, z1) = corners[i], corners[i + 1]
        heading = np.arctan2(x1 - x0, z1 - z0)  # Unity yaw: 0 = +Z, CW positive
        # walk the side
        n = int(side_time * RATE_HZ)
        for k in range(n):
            frac = k / n
            ts.append(t)
            xs.append(x0 + (x1 - x0) * frac)
            zs.append(z0 + (z1 - z0) * frac)
            yaws.append(heading)
            t += dt_nominal * (1.0 + RNG.normal(0, 0.03))  # timing jitter
        # pause ~1 s at the corner
        for k in range(int(1.0 * RATE_HZ)):
            ts.append(t)
            xs.append(x1)
            zs.append(z1)
            yaws.append(heading)
            t += dt_nominal * (1.0 + RNG.normal(0, 0.03))

    ts = np.array(ts)
    xs = np.array(xs) + RNG.normal(0, 0.005, len(xs))       # tracking noise
    zs = np.array(zs) + RNG.normal(0, 0.005, len(zs))
    ys = EYE_HEIGHT + 0.02 * np.sin(2 * np.pi * 1.8 * ts)   # head bob
    ys += RNG.normal(0, 0.002, len(ys))
    return ts, xs, ys, zs, np.array(yaws)


def controller_offset(side):
    """Rough resting controller position relative to the head."""
    dx = 0.20 if side == "right" else -0.20
    return dx, -0.55, 0.25


def main():
    out_root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("sessions")
    start = datetime.now(timezone.utc)
    session_id = start.strftime("%Y%m%d_%H%M%S") + "_P000_FAKE"
    folder = out_root / session_id
    folder.mkdir(parents=True, exist_ok=True)

    ts, xs, ys, zs, yaws = build_path()
    n = len(ts)

    # Simulate a left-controller tracking dropout for ~0.5 s mid-session.
    drop_start = n // 2
    drop_end = drop_start + int(0.5 * RATE_HZ)

    header = ("t_sec,frame,"
              "hmd_px,hmd_py,hmd_pz,hmd_rx,hmd_ry,hmd_rz,hmd_rw,"
              "lc_px,lc_py,lc_pz,lc_rx,lc_ry,lc_rz,lc_rw,"
              "rc_px,rc_py,rc_pz,rc_rx,rc_ry,rc_rz,rc_rw")
    rows = [header]
    for i in range(n):
        qx, qy, qz, qw = yaw_to_quat(yaws[i])
        hmd = f"{xs[i]:.5f},{ys[i]:.5f},{zs[i]:.5f},{qx:.6f},{qy:.6f},{qz:.6f},{qw:.6f}"

        if drop_start <= i < drop_end:
            lc = "NaN,NaN,NaN,NaN,NaN,NaN,NaN"
        else:
            ox, oy, oz = controller_offset("left")
            lc = (f"{xs[i]+ox:.5f},{ys[i]+oy:.5f},{zs[i]+oz:.5f},"
                  f"{qx:.6f},{qy:.6f},{qz:.6f},{qw:.6f}")

        ox, oy, oz = controller_offset("right")
        rc = (f"{xs[i]+ox:.5f},{ys[i]+oy:.5f},{zs[i]+oz:.5f},"
              f"{qx:.6f},{qy:.6f},{qz:.6f},{qw:.6f}")

        rows.append(f"{ts[i]:.5f},{i},{hmd},{lc},{rc}")

    (folder / "telemetry.csv").write_text("\n".join(rows))

    end = start + timedelta(seconds=float(ts[-1]))
    manifest = {
        "schema_version": "0.1.0",
        "session_id": session_id,
        "participant_id": "P000",
        "environment_id": "synthetic_square_test",
        "condition": "test",
        "trial_number": 1,
        "start_utc": start.isoformat(),
        "end_utc": end.isoformat(),
        "duration_sec": round(float(ts[-1]), 3),
        "sample_rate_hz": RATE_HZ,
        "sample_count": n,
        "coordinate_system": "Unity left-handed, Y-up, meters. Floor plane = X (right) by Z (forward).",
        "rotation_format": "quaternion (x,y,z,w)",
        "telemetry_file": "telemetry.csv",
    }
    (folder / "manifest.json").write_text(json.dumps(manifest, indent=2))

    print(f"Wrote synthetic session: {folder}")
    print(f"  samples: {n}, duration: {ts[-1]:.1f} s")
    print(f"Now run:  python analyze_session.py {folder}")


if __name__ == "__main__":
    main()
