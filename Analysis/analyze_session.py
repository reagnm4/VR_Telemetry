"""
analyze_session.py

Loads one recording session (manifest.json + telemetry.csv) produced by the Unity
TelemetryLogger, runs sanity checks, and plots the user's path and head height.

This is the VALIDATION tool. Before trusting ANY behavioral data, do the square test:
walk a roughly 2 x 2 meter square in the empty room, then run this. The printed X span
and Z span should each be about 2.0 m, and the top-down plot should look like a square.
If it does not, fix the Unity side before building anything else on top of it.

Usage:
    python analyze_session.py /path/to/sessions/<session_id>

Coordinate note: Unity is left-handed, Y up, meters. Floor plane is X by Z.
We plot X vs Z for the top-down path. Y is height.

Requires: pandas, numpy, matplotlib
    pip install pandas numpy matplotlib
"""

import json
import sys
from pathlib import Path

import numpy as np
import pandas as pd
import matplotlib.pyplot as plt


def load_session(folder: Path):
    manifest = json.loads((folder / "manifest.json").read_text())
    df = pd.read_csv(folder / "telemetry.csv")
    return manifest, df


def quaternion_forward(qx, qy, qz, qw):
    """Rotate Unity local forward (0,0,1) by the quaternion -> world forward vectors.

    Useful later for "approximate line of sight" (head-direction raycasts).
    Returns an (N, 3) array of forward vectors.
    """
    fx = 2 * (qx * qz + qw * qy)
    fy = 2 * (qy * qz - qw * qx)
    fz = 1 - 2 * (qx * qx + qy * qy)
    return np.stack([fx, fy, fz], axis=-1)


def sanity_report(manifest, df):
    t = df["t_sec"].to_numpy()
    duration = (t[-1] - t[0]) if len(t) > 1 else 0.0
    eff_rate = (len(t) - 1) / duration if duration > 0 else 0.0

    px = df["hmd_px"].to_numpy()
    py = df["hmd_py"].to_numpy()
    pz = df["hmd_pz"].to_numpy()

    # Total horizontal path length (ignore vertical head bob).
    step = np.sqrt(np.diff(px) ** 2 + np.diff(pz) ** 2)
    path_len = float(np.nansum(step))

    x_span = np.nanmax(px) - np.nanmin(px)
    z_span = np.nanmax(pz) - np.nanmin(pz)

    print("=== SESSION SANITY REPORT ===")
    print(f"session_id      : {manifest.get('session_id')}")
    print(f"environment     : {manifest.get('environment_id')}  condition={manifest.get('condition')}")
    print(f"samples         : {len(df)}")
    print(f"duration (s)    : {duration:.2f}")
    print(f"target rate (Hz): {manifest.get('sample_rate_hz')}")
    print(f"effective rate  : {eff_rate:.1f} Hz")
    print(f"X span (m)      : {x_span:.2f}  ({np.nanmin(px):.2f} to {np.nanmax(px):.2f})")
    print(f"Z span (m)      : {z_span:.2f}  ({np.nanmin(pz):.2f} to {np.nanmax(pz):.2f})")
    print(f"head height (m) : {np.nanmin(py):.2f} to {np.nanmax(py):.2f}")
    print(f"horiz path len  : {path_len:.2f} m")

    # Tracking dropouts: the logger writes NaN when a transform is missing.
    # Surface them here so flaky tracking can't hide inside a passing report.
    n = len(df)
    for label, col in [("HMD", "hmd_px"), ("left ctrl", "lc_px"), ("right ctrl", "rc_px")]:
        missing = int(df[col].isna().sum())
        pct = 100.0 * missing / n if n else 0.0
        flag = "  <-- CHECK TRACKING" if pct > 1.0 else ""
        print(f"dropout {label:10s}: {missing} samples ({pct:.1f}%){flag}")

    # Timing gaps: a large max dt means Unity hitched and samples are missing.
    if len(t) > 1:
        dt = np.diff(t)
        expected = 1.0 / manifest.get("sample_rate_hz", 72.0)
        max_dt = float(np.max(dt))
        flag = "  <-- HITCH? gaps > 3x sample period" if max_dt > 3 * expected else ""
        print(f"sample dt (s)   : median {np.median(dt):.4f}, max {max_dt:.4f}{flag}")
    print()
    print(">> SQUARE TEST: after walking a 2x2 m square, X span and Z span should")
    print("   each be about 2.0 m. Effective rate should be close to the target rate.")
    print()


def plot_session(manifest, df):
    px = df["hmd_px"].to_numpy()
    py = df["hmd_py"].to_numpy()
    pz = df["hmd_pz"].to_numpy()
    t = df["t_sec"].to_numpy()

    fig, axes = plt.subplots(1, 2, figsize=(12, 5))

    # Top-down path (X vs Z). Equal aspect so a real square looks square.
    ax = axes[0]
    ax.plot(px, pz, linewidth=1)
    ax.scatter([px[0]], [pz[0]], c="green", s=40, label="start", zorder=3)
    ax.scatter([px[-1]], [pz[-1]], c="red", s=40, label="end", zorder=3)
    ax.set_xlabel("X (m)")
    ax.set_ylabel("Z (m)")
    ax.set_title(f"Top-down path  [{manifest.get('condition')}]")
    ax.set_aspect("equal", adjustable="datalim")
    ax.legend()
    ax.grid(True, alpha=0.3)

    # Head height over time (catches tracking dropouts and crouches).
    ax = axes[1]
    ax.plot(t, py, linewidth=1, color="purple")
    ax.set_xlabel("time (s)")
    ax.set_ylabel("head height Y (m)")
    ax.set_title("Head height over time")
    ax.grid(True, alpha=0.3)

    fig.tight_layout()
    plt.show()


def main():
    if len(sys.argv) < 2:
        print("Usage: python analyze_session.py /path/to/sessions/<session_id>")
        sys.exit(1)
    folder = Path(sys.argv[1])
    manifest, df = load_session(folder)
    sanity_report(manifest, df)
    plot_session(manifest, df)


if __name__ == "__main__":
    main()
