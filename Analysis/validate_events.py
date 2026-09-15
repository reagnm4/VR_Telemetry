"""Structural event checks; do not certify stimulus display timing or task compliance."""
import csv
import hashlib
import json
import math


def check_events(folder, manifest, report):
    if not manifest.get('events_file'):
        return  # Legacy pose-only sessions remain supported.
    if manifest.get('events_schema_version') != '0.1.0':
        report['errors'].append('unsupported_events_schema')
        return
    path = (folder / manifest['events_file']).resolve()
    if path.parent != folder or path.name in ('manifest.json', manifest.get('telemetry_file')):
        raise ValueError('Events must be a separate file directly inside the session folder')
    with path.open(encoding='utf-8-sig', newline='') as stream:
        reader = csv.DictReader(stream)
        columns = reader.fieldnames or []
        rows = list(reader)
    report['source_sha256'][path.name] = hashlib.sha256(path.read_bytes()).hexdigest()
    errors = report['errors']
    if columns != ['t_sec', 'sequence', 'event_type', 'trial_number', 'payload_json']:
        errors.append('invalid_event_columns')
        return
    if manifest.get('event_count') != len(rows):
        errors.append('event_count_mismatch')
    if not rows or rows[0]['event_type'] != 'session_started' or rows[-1]['event_type'] != 'session_stopped':
        errors.append('missing_session_event_boundaries')
    previous = 0.0
    active = None
    visible = None
    outcomes = {}
    started_trials = set()
    for index, row in enumerate(rows):
        try:
            t = float(row['t_sec'])
            sequence = int(row['sequence'])
            trial = int(row['trial_number'])
            data = json.loads(row['payload_json'], parse_constant=lambda value: (_ for _ in ()).throw(ValueError(value)))
            if not isinstance(data, dict) or None in row or any(v is None for v in row.values()):
                raise ValueError('Malformed event')
            duration = float(manifest['duration_sec'])
            if not math.isfinite(t) or t < previous or not math.isfinite(duration) or t > duration + 0.002:
                errors.append('invalid_event_time')
            previous = t
            if sequence != index or trial < 0:
                errors.append('invalid_event_sequence_or_trial')
            kind = row['event_type']
            if kind == 'trial_started':
                if active is not None or trial <= 0 or trial in started_trials:
                    errors.append('invalid_trial_start')
                active = trial
                started_trials.add(trial)
            elif kind == 'trial_ended':
                if active != trial or trial <= 0:
                    errors.append('unmatched_trial_end')
                active = None
                reason = data.get('reason')
                if not isinstance(reason, str) or not reason:
                    errors.append('missing_trial_end_reason')
                else:
                    outcomes[reason] = outcomes.get(reason, 0) + 1
            elif kind == 'target_appeared':
                if active != trial or visible is not None:
                    errors.append('invalid_target_appearance')
                visible = trial
            elif kind == 'target_disappeared':
                if visible != trial or trial <= 0:
                    errors.append('unmatched_target_disappearance')
                visible = None
            if kind in ('target_appeared', 'target_disappeared'):
                pos = data.get('target_position_world', {})
                if not all(isinstance(pos.get(k), (int, float)) and math.isfinite(pos[k]) for k in ('x','y','z')):
                    errors.append('invalid_event_target_position')
        except (ValueError, TypeError, KeyError, OverflowError):
            errors.append('malformed_event')
    if active is not None or visible is not None:
        errors.append('unfinished_trial_or_visible_target')
    report['metrics']['event_count'] = len(rows)
    report['metrics']['trial_outcomes'] = outcomes
    report['warnings'].append('event_times_are_application_commands_not_verified_display_onsets')
    report['errors'] = list(dict.fromkeys(errors))
