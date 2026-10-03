import unittest
from validate_session import validate, COMPONENTS


class ValidationTests(unittest.TestCase):
    def setUp(self):
        self.manifest = dict(schema_version='0.1.0', sample_count=3,
                             duration_sec=0.1, sample_rate_hz=72,
                             session_id='test', participant_id='P000',
                             environment_id='unit_test', condition='test', trial_number=1,
                             start_utc='2026-01-01T00:00:00+00:00',
                             end_utc='2026-01-01T00:00:01+00:00',
                             coordinate_system='Unity left-handed, Y-up, meters. Floor plane = X by Z.',
                             rotation_format='quaternion (x,y,z,w)', telemetry_file='telemetry.csv')
        self.rows = []
        for i in range(3):
            row = {'t_sec': i/72, 'frame': i}
            for p in ('hmd', 'lc', 'rc'):
                row.update({f'{p}_{c}': int(c == 'rw') for c in COMPONENTS})
            self.rows.append(row)
        self.columns = list(self.rows[0])

    def report(self):
        return validate(self.manifest, self.rows, self.columns)

    def test_valid_is_not_square_certificate(self):
        r = self.report()
        self.assertTrue(r['integrity_pass'])
        self.assertEqual(r['square_test_status'], 'not_assessed')

    def test_duplicate_time_rejected(self):
        self.rows[1]['t_sec'] = 0
        self.assertIn('non_increasing_timestamps', self.report()['errors'])

    def test_reverse_time_rejected(self):
        self.rows[2]['t_sec'] = 0.001
        self.assertFalse(self.report()['integrity_pass'])

    def test_nan_timestamp_rejected(self):
        self.rows[1]['t_sec'] = float('nan')
        self.assertIn('invalid_timestamp', self.report()['errors'])

    def test_count_mismatch(self):
        self.manifest['sample_count'] = 4
        self.assertIn('manifest_count_mismatch', self.report()['errors'])

    def test_partial_head_pose_rejected(self):
        self.rows[1]['hmd_rz'] = float('nan')
        self.assertIn('hmd_nonfinite_pose', self.report()['errors'])

    def test_controller_missing_warns(self):
        self.rows[1]['lc_py'] = float('nan')
        self.assertIn('lc_nonfinite_pose', self.report()['warnings'])

    def test_bad_quaternion_rejected(self):
        self.rows[1]['hmd_rw'] = 2
        self.assertIn('hmd_quaternion_not_unit', self.report()['errors'])

    def test_empty(self):
        self.rows = []
        self.assertIn('insufficient_samples', self.report()['errors'])

    def test_missing_column(self):
        self.columns.remove('hmd_px')
        self.assertIn('missing_columns', self.report()['errors'])

    def test_zero_rate(self):
        self.manifest['sample_rate_hz'] = 0
        self.assertIn('invalid_target_rate', self.report()['errors'])

    def test_frame_duplicate(self):
        self.rows[1]['frame'] = 0
        self.assertIn('non_increasing_frames', self.report()['errors'])

    def test_unknown_schema(self):
        self.manifest['schema_version'] = '9.0.0'
        self.assertIn('unsupported_schema', self.report()['errors'])

    def test_new_schema_optional_origin(self):
        self.manifest['schema_version'] = '0.2.0'
        self.manifest.update(missed_sample_deadlines=0,
                             sampling_policy='one_observation_per_LateUpdate_no_backfill',
                             timestamp_source='monotonic application observation',
                             frame_semantics='Unity Time.frameCount',
                             origin_reference_assigned=False,
                             tracking_validity='not_recorded', unity_version='6000.5.2f1')
        for row in self.rows:
            row.update({f'origin_{c}': float('nan') for c in COMPONENTS})
        self.columns = list(self.rows[0])
        self.assertTrue(self.report()['integrity_pass'])
        self.assertIn('origin_nonfinite_pose', self.report()['warnings'])

    def test_end_bound(self):
        self.manifest['duration_sec'] = 0.001
        self.assertIn('sample_after_manifest_end', self.report()['errors'])

    def test_manifest_identity_required(self):
        self.manifest['participant_id'] = '   '
        self.assertIn('missing_or_invalid_manifest_identity', self.report()['errors'])

    def test_manifest_utc_required_and_ordered(self):
        self.manifest['end_utc'] = 'not-a-time'
        self.assertIn('invalid_manifest_utc', self.report()['errors'])
        self.manifest['end_utc'] = '2025-01-01T00:00:00+00:00'
        self.assertIn('manifest_end_before_start', self.report()['errors'])

    def test_event_manifest_is_all_or_none(self):
        self.manifest['events_file'] = 'events.csv'
        self.assertIn('incomplete_event_manifest', self.report()['errors'])

    def test_legacy_coordinate_wording_remains_supported(self):
        self.manifest['coordinate_system'] = (
            'Unity left-handed, Y-up, meters. Floor plane = X (right) by Z (forward).')
        self.assertTrue(self.report()['integrity_pass'])

    def test_invalid_provenance_is_rejected_when_present(self):
        self.manifest['schema_version'] = '0.2.0'
        self.manifest.update(missed_sample_deadlines=0,
                             sampling_policy='one_observation_per_LateUpdate_no_backfill',
                             timestamp_source='monotonic application observation',
                             frame_semantics='Unity Time.frameCount',
                             origin_reference_assigned=False,
                             tracking_validity='not_recorded', unity_version='6000.5.2f1',
                             scene_name='Scene', application_version='0.1', build_guid='',
                             runtime_platform='WindowsEditor', execution_context='unknown')
        for row in self.rows:
            row.update({f'origin_{c}': float('nan') for c in COMPONENTS})
        self.columns = list(self.rows[0])
        self.assertIn('invalid_software_provenance', self.report()['errors'])


if __name__ == '__main__':
    unittest.main()
