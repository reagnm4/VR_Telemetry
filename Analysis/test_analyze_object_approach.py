import csv
import json
from pathlib import Path
import tempfile
import unittest

from analyze_object_approach import analyze_folder


class ObjectApproachAnalysisTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.columns = ['t_sec', 'frame'] + [f'{p}_{c}' for p in ('hmd','lc','rc','origin')
                                                  for c in ('px','py','pz','rx','ry','rz','rw')]
        with (self.folder / 'telemetry.csv').open('w', newline='') as stream:
            writer = csv.DictWriter(stream, fieldnames=self.columns)
            writer.writeheader()
            for index, z in enumerate((0.0, 0.25, 0.5, 0.75, 1.0)):
                row = {column: 0 for column in self.columns}
                row.update(t_sec=index * 0.5, frame=index, hmd_pz=z,
                           hmd_rw=1, lc_rw=1, rc_rw=1, origin_rw=1)
                writer.writerow(row)
        target = {'target_position_world': {'x': 0, 'y': 1.1, 'z': 1.5}}
        events = [
            [0, 0, 'session_started', 0, {}],
            [0, 1, 'trial_started', 1, {}],
            [0, 2, 'target_appeared', 1, target],
            [2, 3, 'trial_ended', 1, {'reason': 'manual_confirmation'}],
            [2, 4, 'target_disappeared', 1, target],
            [2, 5, 'session_stopped', 0, {'reason': 'sequence_finished'}],
        ]
        with (self.folder / 'events.csv').open('w', newline='') as stream:
            writer = csv.writer(stream)
            writer.writerow(['t_sec','sequence','event_type','trial_number','payload_json'])
            writer.writerows([*row[:4], json.dumps(row[4])] for row in events)
        manifest = {
            'schema_version': '0.2.0', 'session_id': 'synthetic',
            'participant_id': 'SYNTHETIC', 'environment_id': 'object_approach_room_v1',
            'condition': 'unit_test', 'sample_count': 5, 'sample_rate_hz': 2,
            'duration_sec': 2, 'telemetry_file': 'telemetry.csv',
            'events_schema_version': '0.1.0', 'events_file': 'events.csv', 'event_count': 6,
            'trial_number': 1, 'start_utc': '2026-01-01T00:00:00+00:00',
            'end_utc': '2026-01-01T00:00:02+00:00',
            'coordinate_system': 'Unity left-handed, Y-up, meters. Floor plane = X by Z.',
            'rotation_format': 'quaternion (x,y,z,w)', 'missed_sample_deadlines': 0,
            'sampling_policy': 'one_observation_per_LateUpdate_no_backfill',
            'timestamp_source': 'monotonic application observation',
            'frame_semantics': 'Unity Time.frameCount', 'origin_reference_assigned': True,
            'tracking_validity': 'not_recorded', 'unity_version': '6000.5.2f1',
        }
        (self.folder / 'manifest.json').write_text(json.dumps(manifest), encoding='utf-8')

    def test_straight_approach_summary(self):
        report = analyze_folder(self.folder)
        trial = report['trials'][0]
        self.assertEqual(report['trial_count'], 1)
        self.assertAlmostEqual(trial['event_duration_sec'], 2)
        self.assertAlmostEqual(trial['horizontal_path_length_m'], 1)
        self.assertAlmostEqual(trial['start_horizontal_center_distance_m'], 1.5)
        self.assertAlmostEqual(trial['closest_horizontal_center_distance_m'], 0.5)
        self.assertAlmostEqual(trial['horizontal_approach_change_m'], 1)
        self.assertAlmostEqual(trial['median_horizontal_speed_mps'], 0.5)
        self.assertAlmostEqual(trial['head_aligned_sample_fraction'], 1)
        self.assertEqual(trial['outcome'], 'manual_confirmation')

    def test_rejects_failed_integrity(self):
        manifest = json.loads((self.folder / 'manifest.json').read_text())
        manifest['sample_count'] = 99
        (self.folder / 'manifest.json').write_text(json.dumps(manifest))
        with self.assertRaisesRegex(ValueError, 'failed structural validation'):
            analyze_folder(self.folder)

    def test_rejects_invalid_alignment_threshold(self):
        with self.assertRaises(ValueError):
            analyze_folder(self.folder, 0)

    def test_short_trial_is_retained_and_labeled(self):
        with (self.folder / 'telemetry.csv').open('w', newline='') as stream:
            writer = csv.DictWriter(stream, fieldnames=self.columns)
            writer.writeheader()
            row = {column: 0 for column in self.columns}
            row.update(t_sec=0, frame=0, hmd_rw=1, lc_rw=1, rc_rw=1, origin_rw=1)
            writer.writerow(row)
        manifest = json.loads((self.folder / 'manifest.json').read_text())
        manifest.update(sample_count=1)
        (self.folder / 'manifest.json').write_text(json.dumps(manifest))
        # The structural validator correctly rejects a one-row session, so retain a
        # valid session but make the trial window shorter than the later pose rows.
        self.setUp_for_short_window()
        report = analyze_folder(self.folder)
        trial = report['trials'][0]
        self.assertEqual(trial['analysis_status'], 'insufficient_pose_samples')
        self.assertEqual(trial['pose_sample_count'], 1)
        self.assertIsNone(trial['horizontal_path_length_m'])

    def setUp_for_short_window(self):
        manifest = json.loads((self.folder / 'manifest.json').read_text())
        manifest.update(sample_count=5)
        (self.folder / 'manifest.json').write_text(json.dumps(manifest))
        with (self.folder / 'telemetry.csv').open('w', newline='') as stream:
            writer = csv.DictWriter(stream, fieldnames=self.columns)
            writer.writeheader()
            for index in range(5):
                row = {column: 0 for column in self.columns}
                row.update(t_sec=index * 0.5, frame=index,
                           hmd_rw=1, lc_rw=1, rc_rw=1, origin_rw=1)
                writer.writerow(row)
        with (self.folder / 'events.csv').open(newline='') as stream:
            rows = list(csv.reader(stream))
        rows[4][0] = '0.1'
        rows[5][0] = '0.1'
        rows[6][0] = '2'
        with (self.folder / 'events.csv').open('w', newline='') as stream:
            csv.writer(stream).writerows(rows)


if __name__ == '__main__':
    unittest.main()
