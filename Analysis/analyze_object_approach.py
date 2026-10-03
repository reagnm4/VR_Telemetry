"""Summarize measured Object-Approach trials after structural validation.

This tool deliberately reports descriptive geometry and timing only. It does not
classify attention, intent, emotion, or cognition. Head alignment is a headset-
direction proxy and is not eye tracking.

Usage:
    python Analysis/analyze_object_approach.py SESSION [--output REPORT.json]
"""

import argparse
import csv
import json
import math
from pathlib import Path
from statistics import median

from validate_session import validate_folder


HEAD_ALIGNMENT_DEGREES = 30.0


def _finite(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def _percentile(values, fraction):
    """Linear percentile matching the common (n - 1) interpolation definition."""
    if not values:
        return None
    ordered = sorted(values)
    index = (len(ordered) - 1) * fraction
    lower = math.floor(index)
    upper = math.ceil(index)
    if lower == upper:
        return ordered[lower]
    return ordered[lower] + (ordered[upper] - ordered[lower]) * (index - lower)


def _horizontal_head_angle_deg(row, target):
    """Angle between horizontal headset-forward and target direction, or None."""
    qx, qy, qz, qw = (float(row[f'hmd_r{axis}']) for axis in ('x', 'y', 'z', 'w'))
    # Unity quaternion rotating local forward (0, 0, 1).
    fx = 2 * (qx * qz + qw * qy)
    fz = 1 - 2 * (qx * qx + qy * qy)
    tx = target['x'] - float(row['hmd_px'])
    tz = target['z'] - float(row['hmd_pz'])
    forward_norm = math.hypot(fx, fz)
    target_norm = math.hypot(tx, tz)
    if forward_norm <= 1e-9 or target_norm <= 1e-9:
        return None
    cosine = (fx * tx + fz * tz) / (forward_norm * target_norm)
    return math.degrees(math.acos(max(-1.0, min(1.0, cosine))))


def _load_events(folder, manifest):
    path = folder / manifest['events_file']
    with path.open(encoding='utf-8-sig', newline='') as stream:
        rows = list(csv.DictReader(stream))
    events = []
    for row in rows:
        events.append({
            'time': float(row['t_sec']),
            'sequence': int(row['sequence']),
            'type': row['event_type'],
            'trial': int(row['trial_number']),
            'payload': json.loads(row['payload_json']),
        })
    return events


def _load_poses(folder, manifest):
    path = folder / manifest.get('telemetry_file', 'telemetry.csv')
    with path.open(encoding='utf-8-sig', newline='') as stream:
        return list(csv.DictReader(stream))


def _target_from_event(event):
    target = event['payload'].get('target_position_world')
    if not isinstance(target, dict) or not all(_finite(target.get(axis)) for axis in ('x', 'y', 'z')):
        raise ValueError(f"Trial {event['trial']} has no finite target position")
    return {axis: float(target[axis]) for axis in ('x', 'y', 'z')}


def _trial_windows(events):
    windows = {}
    for event in events:
        trial = event['trial']
        if trial <= 0:
            continue
        window = windows.setdefault(trial, {'trial_number': trial})
        if event['type'] == 'target_appeared':
            window['start_sec'] = event['time']
            window['target'] = _target_from_event(event)
        elif event['type'] == 'trial_ended':
            window['end_sec'] = event['time']
            window['outcome'] = event['payload'].get('reason')
    complete = []
    for trial in sorted(windows):
        window = windows[trial]
        if not all(key in window for key in ('start_sec', 'end_sec', 'target', 'outcome')):
            raise ValueError(f'Trial {trial} has incomplete event boundaries')
        complete.append(window)
    if not complete:
        raise ValueError('No completed Object-Approach trials found')
    return complete


def _summarize_trial(window, poses, alignment_degrees):
    start, end, target = window['start_sec'], window['end_sec'], window['target']
    selected = [row for row in poses if start <= float(row['t_sec']) <= end]
    positions = [(float(row['hmd_px']), float(row['hmd_pz'])) for row in selected]
    distances = [math.hypot(target['x'] - x, target['z'] - z) for x, z in positions]
    steps, speeds = [], []
    for previous, current, p0, p1 in zip(selected, selected[1:], positions, positions[1:]):
        dt = float(current['t_sec']) - float(previous['t_sec'])
        step = math.hypot(p1[0] - p0[0], p1[1] - p0[1])
        steps.append(step)
        if dt > 0:
            speeds.append(step / dt)

    angles = [_horizontal_head_angle_deg(row, target) for row in selected]
    angles = [angle for angle in angles if angle is not None]
    aligned = sum(angle <= alignment_degrees for angle in angles)
    closest_index = min(range(len(distances)), key=distances.__getitem__) if distances else None

    return {
        'trial_number': window['trial_number'],
        'outcome': window['outcome'],
        'analysis_status': 'complete' if len(selected) >= 2 else 'insufficient_pose_samples',
        'event_start_sec': start,
        'event_end_sec': end,
        'event_duration_sec': end - start,
        'first_pose_sec': float(selected[0]['t_sec']) if selected else None,
        'last_pose_sec': float(selected[-1]['t_sec']) if selected else None,
        'pose_sample_count': len(selected),
        'target_position_world_m': target,
        'start_horizontal_center_distance_m': distances[0] if distances else None,
        'closest_horizontal_center_distance_m': distances[closest_index] if distances else None,
        'closest_distance_time_sec': float(selected[closest_index]['t_sec']) if distances else None,
        'end_horizontal_center_distance_m': distances[-1] if distances else None,
        'horizontal_approach_change_m': distances[0] - min(distances) if distances else None,
        'horizontal_path_length_m': sum(steps) if len(selected) >= 2 else None,
        'median_horizontal_speed_mps': median(speeds) if len(selected) >= 2 and speeds else None,
        'p95_horizontal_speed_mps': _percentile(speeds, 0.95) if len(selected) >= 2 else None,
        'head_target_angle_median_deg': median(angles) if angles else None,
        'head_aligned_sample_fraction': aligned / len(angles) if angles else None,
        'head_alignment_threshold_deg': alignment_degrees,
    }


def analyze_folder(folder, alignment_degrees=HEAD_ALIGNMENT_DEGREES):
    folder = Path(folder).resolve()
    if not _finite(alignment_degrees) or not 0 < alignment_degrees <= 180:
        raise ValueError('Head-alignment threshold must be in (0, 180] degrees')
    validation = validate_folder(folder)
    if not validation['integrity_pass']:
        raise ValueError('Session failed structural validation: ' + ', '.join(validation['errors']))

    manifest = json.loads((folder / 'manifest.json').read_text(encoding='utf-8-sig'))
    if not manifest.get('events_file'):
        raise ValueError('Object-Approach analysis requires an event stream')
    events = _load_events(folder, manifest)
    poses = _load_poses(folder, manifest)
    trials = [_summarize_trial(window, poses, float(alignment_degrees))
              for window in _trial_windows(events)]
    return {
        'analysis_version': '0.1.0',
        'analysis_type': 'descriptive_object_approach',
        'session_id': manifest.get('session_id'),
        'participant_id': manifest.get('participant_id'),
        'environment_id': manifest.get('environment_id'),
        'condition': manifest.get('condition'),
        'source_sha256': validation['source_sha256'],
        'validator_version': validation['validator_version'],
        'validation_warnings': validation['warnings'],
        'measurement_notes': [
            'Distances and path lengths use the horizontal X-Z plane and the target center.',
            'Event duration uses application-command times; display onset is not verified.',
            'Pose summaries include samples whose timestamps fall inside the event window.',
            'Trials with fewer than two included poses are retained but cannot support path or speed metrics.',
            'Head alignment is a headset-direction proxy, not eye gaze or attention.',
            'No cognitive, emotional, or clinical state is inferred.',
        ],
        'trial_count': len(trials),
        'trials': trials,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('session', type=Path)
    parser.add_argument('--head-alignment-degrees', type=float, default=HEAD_ALIGNMENT_DEGREES)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    try:
        report = analyze_folder(args.session, args.head_alignment_degrees)
        encoded = json.dumps(report, indent=2, allow_nan=False)
        if args.output:
            with args.output.open('x', encoding='utf-8') as stream:
                stream.write(encoded + '\n')
        print(encoded)
        return 0
    except (OSError, ValueError, TypeError, KeyError, json.JSONDecodeError) as exc:
        print(json.dumps({'analysis_complete': False, 'input_error': str(exc)}))
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
