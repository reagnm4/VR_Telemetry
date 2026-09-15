"""Non-destructive integrity validation, not a human square-test certificate.

python Analysis/validate_session.py SESSION --output REPORT.json
Exit codes: 0 no integrity errors (warnings may remain), 1 invalid, 2 input error.
Uses the standard library. Supports schema 0.1.0 and 0.2.0 explicitly.
"""
import argparse
import csv
import hashlib
import json
import math
from pathlib import Path
from statistics import median
from validate_events import check_events

COMPONENTS = ('px', 'py', 'pz', 'rx', 'ry', 'rz', 'rw')


def validate(manifest, rows, columns):
    errors, warnings, metrics = [], [], {'sample_count': len(rows)}
    version = manifest.get('schema_version')
    if version not in ('0.1.0', '0.2.0'):
        errors.append('unsupported_schema')
    prefixes = ['hmd', 'lc', 'rc'] + (['origin'] if version == '0.2.0' else [])
    required = ['t_sec', 'frame'] + [f'{p}_{c}' for p in prefixes for c in COMPONENTS]
    if len(columns) != len(set(columns)):
        errors.append('duplicate_columns')
    if set(required) - set(columns):
        errors.append('missing_columns')
    if manifest.get('sample_count') != len(rows):
        errors.append('manifest_count_mismatch')
    if len(rows) < 2:
        errors.append('insufficient_samples')
    rate = manifest.get('sample_rate_hz')
    valid_rate = isinstance(rate, (float, int)) and not isinstance(rate, bool) and math.isfinite(rate) and rate > 0
    if not valid_rate:
        errors.append('invalid_target_rate')
    duration = manifest.get('duration_sec')
    valid_duration = isinstance(duration, (float, int)) and not isinstance(duration, bool) and math.isfinite(duration) and duration >= 0
    if not valid_duration:
        errors.append('invalid_manifest_duration')
    if 'missing_columns' not in errors:
        try:
            values = [{k: float(row[k]) for k in required} for row in rows]
            if any(None in row or any(v is None for v in row.values()) for row in rows):
                errors.append('malformed_csv_row')
        except (ValueError, TypeError, KeyError):
            values = []
            errors.append('nonnumeric_or_missing_value')
        times = [r['t_sec'] for r in values]
        if any(not math.isfinite(t) or t < 0 for t in times):
            errors.append('invalid_timestamp')
        elif len(times) >= 2:
            dt = [b-a for a, b in zip(times, times[1:])]
            metrics['non_increasing_intervals'] = sum(d <= 0 for d in dt)
            if metrics['non_increasing_intervals']:
                errors.append('non_increasing_timestamps')
            span = times[-1]-times[0]
            metrics.update(median_dt_sec=median(dt), max_dt_sec=max(dt),
                           observed_duration_sec=span,
                           effective_rate_hz=(len(times)-1)/span if span > 0 else None)
            if valid_rate and max(dt) > 3/rate:
                warnings.append('gap_over_three_periods')
            if valid_rate and span > 0 and metrics['effective_rate_hz'] < 0.9*rate:
                warnings.append('effective_rate_below_90_percent_target')
            if valid_duration and times[-1] > duration + 0.002:
                errors.append('sample_after_manifest_end')
        frames = [r['frame'] for r in values]
        if any(not math.isfinite(f) or f < 0 or f != int(f) for f in frames):
            errors.append('invalid_frame')
        elif any(b <= a for a, b in zip(frames, frames[1:])):
            errors.append('non_increasing_frames')
        for p in prefixes:
            poses = [[r[f'{p}_{c}'] for c in COMPONENTS] for r in values]
            missing = sum(any(not math.isfinite(x) for x in pose) for pose in poses)
            metrics[f'{p}_nonfinite_pose_rows'] = missing
            if missing:
                (errors if p == 'hmd' else warnings).append(f'{p}_nonfinite_pose')
            finite = [pose for pose in poses if all(math.isfinite(x) for x in pose)]
            bad_q = sum(abs(math.sqrt(sum(q*q for q in pose[3:]))-1) > 0.01 for pose in finite)
            metrics[f'{p}_invalid_quaternions'] = bad_q
            if bad_q:
                errors.append(f'{p}_quaternion_not_unit')
            if p == 'hmd' and finite:
                metrics['x_span_m'] = max(pose[0] for pose in finite)-min(pose[0] for pose in finite)
                metrics['z_span_m'] = max(pose[2] for pose in finite)-min(pose[2] for pose in finite)
    warnings.append('hardware_tracking_validity_unverified')
    return {'validator_version': '0.2.0', 'schema_version': version,
            'integrity_pass': not errors, 'square_test_status': 'not_assessed',
            'errors': errors, 'warnings': warnings, 'metrics': metrics,
            'thresholds': {'quaternion_norm_tolerance': 0.01,
                           'gap_warning_periods': 3, 'rate_warning_fraction': 0.9,
                           'manifest_rounding_tolerance_sec': 0.002}}


def validate_folder(folder):
    folder = Path(folder).resolve()
    manifest_path = folder / 'manifest.json'
    manifest = json.loads(manifest_path.read_text(encoding='utf-8-sig'))
    if not isinstance(manifest, dict):
        raise ValueError('Manifest must be an object')
    telemetry_path = (folder / manifest.get('telemetry_file', 'telemetry.csv')).resolve()
    if telemetry_path.parent != folder:
        raise ValueError('Telemetry file must be directly inside the session folder')
    with telemetry_path.open(encoding='utf-8-sig', newline='') as stream:
        reader = csv.DictReader(stream)
        rows = list(reader)
        columns = reader.fieldnames or []
    report = validate(manifest, rows, columns)
    report['source_sha256'] = {p.name: hashlib.sha256(p.read_bytes()).hexdigest()
                               for p in (manifest_path, telemetry_path)}
    check_events(folder, manifest, report)
    report['integrity_pass'] = not report['errors']
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('session', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    try:
        report = validate_folder(args.session)
    except (OSError, ValueError, TypeError) as exc:
        print(json.dumps({'integrity_pass': False, 'input_error': str(exc)}))
        return 2
    encoded = json.dumps(report, indent=2, allow_nan=False)
    if args.output:
        # Never overwrite a raw session or a previous report.
        with args.output.open('x', encoding='utf-8') as stream:
            stream.write(encoded + '\n')
    print(encoded)
    return 0 if report['integrity_pass'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
