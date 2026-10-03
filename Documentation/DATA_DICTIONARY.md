# VR Telemetry data dictionary

Updated 2026-10-03, America/New_York.

This document defines the files and fields currently produced or derived by the
platform. It describes software semantics, not a validated research protocol. Raw
sessions are immutable evidence: never edit a CSV or manifest to make validation pass.

## Session folder contract

A completed session folder contains:

| File | Role | Completion meaning |
|---|---|---|
| `telemetry.csv` | Raw pose observations | Data file; insufficient by itself to declare a complete session |
| `events.csv` | Ordered application events | Optional for legacy pose-only sessions; required by current Object-Approach sessions |
| `manifest.json` | Session identity, context, counts, and schema declarations | Written last; its presence means the application completed all three final-name writes |

Temporary `*.tmp` files or a folder without `manifest.json` indicate an incomplete
export. They are evidence of a failure and should be retained and noted. The in-app
retry can rewrite final files in the same unique folder while its buffers still exist.
No current guarantee covers process crashes, power loss, storage failure, or manual
file modification.

The integrity report and Object-Approach report are derived artifacts. Store them
outside the raw session folder when practical. Both include SHA-256 hashes that bind
their conclusions to the exact source bytes they evaluated.

## Manifest — raw pose schema 0.2.0

JSON types below are the intended serialized types. Additive metadata may appear in a
0.2.0 manifest without changing the telemetry columns. Consumers must reject an
unsupported `schema_version` rather than guessing.

| Field | Type / unit | Meaning and constraints |
|---|---|---|
| `schema_version` | string | Raw telemetry contract; currently `0.2.0` |
| `session_id` | string | UTC timestamp plus random GUID; unique folder identifier, not a participant identifier |
| `participant_id` | string | Operator-assigned pseudonymous label such as `P000`; current software does not enforce a registry |
| `environment_id` | string | Operator/task-assigned environment label |
| `condition` | string | Operator/task-assigned condition label; descriptive until a protocol defines allowed values |
| `trial_number` | integer | Legacy session-level metadata; repeated-trial identity comes from `events.csv` |
| `start_utc` | ISO-8601 string | Wall-clock time recorded immediately after the logger starts |
| `end_utc` | ISO-8601 string | Wall-clock time captured during orderly stop |
| `duration_sec` | seconds, number | Monotonic logger elapsed time at stop; event/pose upper bound with small serialization tolerance |
| `sample_rate_hz` | Hz, number | Requested observation rate snapshotted at start; not proof of achieved rate |
| `sample_count` | integer | Number of telemetry data rows, excluding the header |
| `missed_sample_deadlines` | integer | Target schedule opportunities missed because Unity could not observe them; no rows are fabricated |
| `coordinate_system` | string | Declares Unity left-handed, Y-up, meters, X-Z floor plane |
| `rotation_format` | string | Declares quaternion component order `(x,y,z,w)` |
| `telemetry_file` | filename string | Raw pose CSV; must resolve directly inside the session folder |
| `sampling_policy` | string | Currently one observation at most per `LateUpdate`, with no backfill |
| `timestamp_source` | string | Unity monotonic application time; observation time, not hardware sensor capture time |
| `frame_semantics` | string | `frame` stores Unity `Time.frameCount` |
| `origin_reference_assigned` | boolean | Whether an XR Origin transform reference existed; not proof it was valid or stationary |
| `tracking_validity` | string | Currently declares that tracking validity is not recorded |
| `unity_version` | string | Unity Editor/runtime version reported by the application |
| `scene_name` | string | Active Unity scene snapshotted at session start |
| `application_version` | string | Unity Player Settings application version; useful only if maintained intentionally |
| `build_guid` | string | Unity player build GUID when available; may be empty in Editor |
| `runtime_platform` | string | Unity runtime platform, such as Windows Editor or Android |
| `execution_context` | enum string | `editor` or `player` |
| `events_file` | filename string | Event CSV path for sessions that declare the event extension |
| `events_schema_version` | string | Independent event contract; currently `0.1.0` |
| `event_count` | integer | Number of event data rows, excluding the header |

Participant, environment, condition, locomotion, and protocol labels are entered
metadata. Their spelling is not evidence that the procedure was followed.

## `telemetry.csv` — raw pose observations

Each row is one application observation. Rows must have strictly increasing `t_sec`
and `frame`; the logger never emits multiple catch-up observations in one frame.

| Column | Type / unit | Meaning |
|---|---|---|
| `t_sec` | seconds, floating point | Monotonic elapsed time since logger start |
| `frame` | non-negative integer | Unity frame in which transforms were observed |
| `<prefix>_px` | meters | World-space X position |
| `<prefix>_py` | meters | World-space Y position (height axis) |
| `<prefix>_pz` | meters | World-space Z position |
| `<prefix>_rx` | quaternion component | World-space rotation X |
| `<prefix>_ry` | quaternion component | World-space rotation Y |
| `<prefix>_rz` | quaternion component | World-space rotation Z |
| `<prefix>_rw` | quaternion component | World-space rotation W |

Pose prefixes:

| Prefix | Transform |
|---|---|
| `hmd` | Main headset camera; required for an integrity pass |
| `lc` | Left controller; missing rows produce a warning |
| `rc` | Right controller; missing rows produce a warning |
| `origin` | XR Origin root; introduced in schema 0.2.0; optional reference with warning if missing |

A missing transform is written as seven `NaN` values for that pose. Finite values mean
only that Unity returned numbers. They do not prove headset tracking was valid. A
quaternion norm outside the validator tolerance is a structural error. Positions are
world-space application transforms, not raw device-driver coordinates.

Sampling is observation-based. If the application stalls, the next row records the
actual later time and `missed_sample_deadlines` increases. Analysis must use `t_sec`
rather than assuming `1 / sample_rate_hz` between rows.

## `events.csv` — event schema 0.1.0

| Column | Type / unit | Meaning and constraints |
|---|---|---|
| `t_sec` | seconds, floating point | Same logger elapsed clock as telemetry; nondecreasing |
| `sequence` | zero-based integer | Strict row order and tie-breaker for equal timestamps |
| `event_type` | string | Application event name |
| `trial_number` | non-negative integer | `0` for session/configuration events; `1..N` for repeated trials |
| `payload_json` | JSON object encoded as one CSV field | Event-specific context; parse CSV first, then JSON |

Equal event times are valid because several ordered application commands can occur in
one frame. Event times identify command execution, not photons reaching the display,
hardware sensor time, participant perception, or reaction onset.

Current Object-Approach event order and meaning:

| Event | Trial | Meaning |
|---|---:|---|
| `session_started` | 0 | Pose/event session became active |
| `task_configured` | 0 | Task geometry, timing, planned trials, and locomotion label were snapshotted |
| `session_ready` | 0 | State machine is waiting for explicit Ready |
| `trial_countdown_started` | 1..N | Ready was accepted and countdown began |
| `trial_started` | 1..N | Countdown ended; active timeout window began |
| `target_appeared` | 1..N | Logged after `SetActive(true)` was commanded |
| `trial_ended` | 1..N | Active trial ended; payload `reason` gives its termination path |
| `target_disappeared` | 1..N | Logged after `SetActive(false)` was commanded |
| `rest_started` | 1..N | Minimum rest began after a non-final trial |
| `rest_ended` | 1..N | Minimum rest elapsed; explicit Ready is still required |
| `trial_cancelled` | 1..N | Countdown was cancelled before the target appeared |
| `sequence_finished` | final trial | All planned trials ended |
| `sequence_stopped` | current/last trial | Operator, disable, or shutdown stopped the sequence early |
| `session_stopped` | 0 | Terminal session event with stop reason |

Current `trial_ended` reasons include `manual_confirmation`, `timeout`,
`operator_stop`, `task_disabled`, `component_disabled`, and `application_quit` as
applicable. A reason describes the software path. For example, manual confirmation is
a button declaration, not proof of distance, gaze, understanding, or compliance.

Target payload fields use world-space meters/quaternions and include
`target_position_world`, `target_rotation_world`, and `target_scale_world`. Task
configuration payloads also include planned trial count, countdown, timeout, minimum
rest, and the operator-selected locomotion label.

## Integrity report — validator version 0.3.1

The validator is separate from raw schemas. Its `integrity_pass` means no structural
errors were detected under its current rules. It does not certify tracking, physical
scale, task compliance, stimulus display timing, or research validity.

Version 0.3.x also requires core session/participant/environment/condition identity,
timezone-aware start/end timestamps in valid order, declared coordinate and quaternion
semantics, a non-negative trial number, complete event-extension declarations, and the
schema 0.2.0 sampling/context metadata. Older schema 0.1.0 coordinate wording remains
explicitly supported. Pre-provenance 0.2.0 sessions remain valid but receive
`software_provenance_incomplete`.

Version 0.3.1 makes the declared telemetry column list/order and sample-count type
strict, and rejects duplicate keys in manifests or event payload JSON instead of
letting a parser silently keep only the final value.

Important fields:

| Field | Meaning |
|---|---|
| `integrity_pass` | `true` only when `errors` is empty |
| `square_test_status` | Currently always `not_assessed`; human physical validation is separate |
| `errors` | Structural failures that block analysis |
| `warnings` | Limitations or quality signals that remain visible but do not automatically block descriptive analysis |
| `metrics` | Counts, timing intervals/rates, spans, missing poses, quaternion checks, and trial outcomes |
| `thresholds` | Numeric rules used by this validator version |
| `source_sha256` | Hash of each raw file actually evaluated |

Common warnings:

- `gap_over_three_periods`: at least one observation gap exceeded three requested
  sample periods.
- `effective_rate_below_90_percent_target`: full observed span averaged below 90% of
  the requested rate.
- `hardware_tracking_validity_unverified`: finite poses are not tracking-status data.
- `event_times_are_application_commands_not_verified_display_onsets`: event timing
  has not been measured against rendered/displayed onset.
- `software_provenance_incomplete`: the recording predates one or more additive scene
  or build-context fields; raw data may still be structurally valid.

Warnings require review in context. They must not be removed from a copied report to
make a run appear clean.

## Object-Approach report — analysis version 0.1.0

The analyzer first requires the integrity validator to pass. It creates one record per
completed trial and carries source hashes and validation warnings forward.

| Field | Unit | Definition |
|---|---|---|
| `analysis_status` | enum | `complete` with at least two included poses; otherwise `insufficient_pose_samples` |
| `outcome` | string | Exact `trial_ended` reason |
| `event_start_sec` | seconds | `target_appeared` application-command time |
| `event_end_sec` | seconds | `trial_ended` application-command time |
| `event_duration_sec` | seconds | End minus start event time |
| `first_pose_sec`, `last_pose_sec` | seconds or null | Bounds of included pose observations |
| `pose_sample_count` | integer | Pose rows whose timestamps lie inside the inclusive event window |
| `target_position_world_m` | meters | Target center used for the trial |
| `start_horizontal_center_distance_m` | meters or null | First included HMD-to-target-center X-Z distance |
| `closest_horizontal_center_distance_m` | meters or null | Minimum included HMD-to-target-center X-Z distance |
| `closest_distance_time_sec` | seconds or null | Pose timestamp of that minimum |
| `end_horizontal_center_distance_m` | meters or null | Last included X-Z center distance |
| `horizontal_approach_change_m` | meters or null | Start distance minus closest distance; descriptive displacement toward target |
| `horizontal_path_length_m` | meters or null | Sum of consecutive HMD X-Z step lengths; null with fewer than two poses |
| `median_horizontal_speed_mps` | m/s or null | Median consecutive X-Z step length divided by actual timestamp delta |
| `p95_horizontal_speed_mps` | m/s or null | Linearly interpolated 95th percentile of the same step speeds |
| `head_target_angle_median_deg` | degrees or null | Median horizontal angle between HMD forward and target-center direction |
| `head_aligned_sample_fraction` | fraction 0..1 or null | Included, geometrically defined samples within the selected angle threshold |
| `head_alignment_threshold_deg` | degrees | Explicit analysis parameter; engineering default is 30 degrees |

Distance is to the target center in the floor plane. It is not distance to the sphere
surface, hand reach, body distance, collision distance, or perceived distance. Path
length is HMD horizontal travel, not foot trajectory. Head alignment is an approximate
line-of-sight proxy and must never be renamed gaze, fixation, attention, interest, or
awareness. No current derived field identifies avoidance, anxiety, cognitive load,
emotion, impairment, diagnosis, or any other latent state.

Slowdown point, dwell, trial exclusion, cross-trial aggregation, and participant-level
statistics remain intentionally undefined until Reagan reviews operational choices and
headset evidence.

## Schema and analysis change policy

- Changing raw column names, units, coordinate meaning, timestamp meaning, or missing-
  value semantics requires a new raw schema version and validator support.
- Changing event columns, ordering semantics, or required payload meaning requires a
  new event schema version.
- Changing a derived metric definition or default threshold requires a new analysis
  version and a chronological decision entry.
- Additive manifest provenance may remain compatible when older consumers ignore
  unknown fields and required existing meanings do not change.
- Never reinterpret an old field silently. Preserve the old definition, add a new
  version or field, and record the migration and revisit conditions.
