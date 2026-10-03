import csv
import json
from pathlib import Path
import tempfile
import unittest
from validate_session import validate_folder


class EventValidationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.folder = Path(self.temp.name)
        self.manifest = dict(schema_version='0.2.0', sample_count=3, sample_rate_hz=72,
                             duration_sec=10, telemetry_file='telemetry.csv',
                             events_schema_version='0.1.0', events_file='events.csv',
                             session_id='test', participant_id='SYNTHETIC',
                             environment_id='object_approach_room_v1', condition='test', trial_number=1,
                             start_utc='2026-01-01T00:00:00+00:00',
                             end_utc='2026-01-01T00:00:10+00:00',
                             coordinate_system='Unity left-handed, Y-up, meters. Floor plane = X by Z.',
                             rotation_format='quaternion (x,y,z,w)', missed_sample_deadlines=0,
                             sampling_policy='one_observation_per_LateUpdate_no_backfill',
                             timestamp_source='monotonic application observation',
                             frame_semantics='Unity Time.frameCount', origin_reference_assigned=True,
                             tracking_validity='not_recorded', unity_version='6000.5.2f1')
        columns = ['t_sec', 'frame'] + [f'{p}_{c}' for p in ('hmd','lc','rc','origin')
                                       for c in ('px','py','pz','rx','ry','rz','rw')]
        with (self.folder/'telemetry.csv').open('w', newline='') as f:
            writer = csv.DictWriter(f, fieldnames=columns); writer.writeheader()
            for i in range(3):
                row = {c: int(c.endswith('_rw')) for c in columns}
                row.update(t_sec=i/72, frame=i)
                writer.writerow(row)
        target = {'target_position_world': {'x':0,'y':1.1,'z':1.5}}
        self.events = [
            [0,0,'session_started',0,{}],
            [1,1,'trial_started',1,{}],
            [1,2,'target_appeared',1,target],
            [2,3,'trial_ended',1,{'reason':'manual_confirmation'}],
            [2,4,'target_disappeared',1,target],
            [2,5,'session_stopped',0,{'reason':'sequence_finished'}]]

    def report(self):
        if 'events_file' in self.manifest:
            self.manifest.setdefault('event_count', len(self.events))
        (self.folder/'manifest.json').write_text(json.dumps(self.manifest))
        with (self.folder/'events.csv').open('w', newline='') as f:
            writer = csv.writer(f)
            writer.writerow(['t_sec','sequence','event_type','trial_number','payload_json'])
            writer.writerows([*row[:4], json.dumps(row[4])] for row in self.events)
        return validate_folder(self.folder)

    def test_valid_equal_timestamp_events(self):
        r = self.report()
        self.assertTrue(r['integrity_pass'], r['errors'])
        self.assertEqual(r['metrics']['trial_outcomes'], {'manual_confirmation':1})
        self.assertIn('events.csv', r['source_sha256'])

    def test_timeout_reason_preserved(self):
        self.events[3][4] = {'reason':'timeout'}
        self.assertEqual(self.report()['metrics']['trial_outcomes'], {'timeout':1})

    def test_csv_quoted_payload(self):
        self.events[-1][4]['note'] = 'quote " comma,\nand newline'
        self.assertTrue(self.report()['integrity_pass'])

    def test_out_of_order_time(self):
        self.events[-1][0] = 0
        self.assertIn('invalid_event_time', self.report()['errors'])

    def test_nonfinite_time(self):
        self.events[2][0] = float('nan')
        self.assertIn('invalid_event_time', self.report()['errors'])

    def test_past_session_end(self):
        self.events[-1][0] = 11
        self.assertIn('invalid_event_time', self.report()['errors'])

    def test_missing_terminal_event(self):
        self.events.pop()
        self.assertIn('missing_session_event_boundaries', self.report()['errors'])

    def test_bad_sequence(self):
        self.events[3][1] = 9
        self.assertIn('invalid_event_sequence_or_trial', self.report()['errors'])

    def test_unmatched_trial(self):
        self.events[3][3] = 2
        self.assertIn('unmatched_trial_end', self.report()['errors'])

    def test_missing_reason(self):
        self.events[3][4] = {}
        self.assertIn('missing_trial_end_reason', self.report()['errors'])

    def test_invalid_target(self):
        self.events[2][4] = {}
        self.assertIn('invalid_event_target_position', self.report()['errors'])

    def test_unfinished_target(self):
        self.events.pop(4)
        for i, row in enumerate(self.events): row[1] = i
        self.assertIn('unfinished_trial_or_visible_target', self.report()['errors'])

    def test_trial_must_show_target(self):
        self.events.pop(2)
        for i, row in enumerate(self.events): row[1] = i
        errors = self.report()['errors']
        self.assertIn('trial_ended_without_visible_target', errors)
        self.assertIn('incomplete_trial_lifecycle', errors)

    def test_target_cannot_disappear_before_trial_end(self):
        self.events[3], self.events[4] = self.events[4], self.events[3]
        for i, row in enumerate(self.events): row[1] = i
        self.assertIn('unmatched_target_disappearance', self.report()['errors'])

    def test_trial_numbers_are_consecutive(self):
        for row in self.events:
            if row[3] == 1: row[3] = 2
        self.assertIn('nonconsecutive_trial_numbers', self.report()['errors'])

    def test_non_target_trial_schema_remains_supported(self):
        self.manifest['environment_id'] = 'generic_trial_room'
        self.events = [self.events[0], self.events[1], self.events[3], self.events[-1]]
        for i, row in enumerate(self.events): row[1] = i
        self.assertTrue(self.report()['integrity_pass'])

    def test_count_mismatch(self):
        self.manifest['event_count'] = 100
        self.assertIn('event_count_mismatch', self.report()['errors'])

    def test_unknown_event_schema(self):
        self.manifest['events_schema_version'] = '9'
        self.assertIn('unsupported_events_schema', self.report()['errors'])

    def test_legacy_no_events(self):
        self.manifest.pop('events_file')
        self.manifest.pop('events_schema_version')
        self.manifest.pop('event_count', None)
        self.assertTrue(self.report()['integrity_pass'])

    def test_path_traversal(self):
        self.manifest['events_file'] = '../events.csv'
        with self.assertRaises(ValueError): self.report()


if __name__ == '__main__':
    unittest.main()
