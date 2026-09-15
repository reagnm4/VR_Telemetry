# Object-Approach prototype — build and use

Updated 2026-09-14, America/New_York.

This implements the agreed neutral target task, in-headset controls, manual trial
confirmation with timeout, and timestamped events. It is an engineering prototype.
The manual-start stationary repeat and physical square validation remain pending.

## Open the room

Open Assets/Scenes/ObjectApproachRoom.unity. It is separate from SampleScene and
ValidationRoom2x2. If regenerating, use Tools > VR Telemetry > Create Object Approach
Room outside Play Mode. Unity asks about saving open modified scenes. The builder
uses unique filenames, so repeated builds do not overwrite an existing room.

The scene reuses the installed Starter Assets XR rig with Floor tracking and unit
scale. It retains continuous movement/turn setup and disables the named teleport,
climb, grab-move, and jump locomotion objects. It has a 6 x 8 m supporting floor,
three walls, a blue start marker at world X/Z=0, and a 0.3 m neutral sphere centered
at world (0, 1.1, 1.5). This room does NOT replace the exact 2 x 2 validation platform.

Virtual floor dimensions do not establish available physical room size. Choose
physical or thumbstick movement explicitly before a run, and label it in the task's
Locomotion Mode field. Do not mix modes without labeling the session accordingly.
The mode label is descriptive; it does not automatically change input bindings.

## Before Play

Select TelemetrySystem:
- Session Manager: participant P000 for self-test, environment object_approach_room_v1,
  condition neutral_prototype; Auto Start On Play OFF; session auto-stop zero.
- Telemetry Logger: headset, both controllers, and XR Origin assigned; target rate 72.
- Object Approach Controller: three trials, three-second countdown, 30-second maximum
  trial duration, minimum five-second rest. Set Locomotion Mode to physical, thumbstick,
  or mixed to describe your planned movement.
- The defaults are adjustable engineering settings, not research-validated durations.

Save edits outside Play. Do not alter the target, rig scale, or task configuration
during a recording. Target location and timing settings are exported as event payloads.
Session metadata is snapshotted when starting; trial indices come from events.

## In the headset

Connect through your usual Air Link workflow, enter Play, and let tracking settle.
The control panel appears below the central view and follows the headset. Aim the
existing controller UI ray at its buttons and use the UI select action.

1. **Begin session** starts raw pose and event recording. It does not show the target.
2. Return to the blue start marker at your own pace. **Ready / next trial** starts
   the countdown. A known starting point is an instruction, not an enforced threshold.
3. The target appears after the countdown. Approach and look at it.
4. **I have finished** ends the trial with manual_confirmation. This is a declaration,
   not an automatically verified distance or gaze measure.
5. If the duration expires first, the trial ends with timeout. At the exact deadline,
   timeout wins even if confirmation is delivered in that frame.
6. The target disappears. Rest for at least five seconds, return toward the marker,
   and select Ready again when prepared. Rest expiration never starts a trial by itself.
7. After the final trial, the session exports automatically.
8. **STOP AND SAVE** stops during Ready, countdown, active trial, or rest. An active
   trial gets an end reason; a countdown gets a cancellation. Failed exports can
   be retried with the same button.

Desktop fallback: right-click Object Approach Controller's Inspector header for
Begin Object Approach Session, Ready - Start Next Trial, Confirm Trial Complete,
and Stop Task and Save. These call the same methods as the panel.
Use these during engineering/debugging, not as silently interchangeable task actions.

The Session Manager's existing direct Start Recording control remains available for
standalone validation. Use the TASK's Begin Session for this room so its sequence and
configuration are initialized.

## What is recorded

Each new session exports manifest.json, telemetry.csv, and events.csv. Existing
schema 0.2.0 pose columns are unchanged. The optional event extension has independent
events_schema_version 0.1.0, events_file, and event_count manifest fields.
Older pose-only 0.1.0/0.2.0 files remain readable.

events.csv columns:
```
t_sec,sequence,event_type,trial_number,payload_json
```

- t_sec uses the SAME logger elapsed clock as poses.
- sequence is zero-based and orders events, including equal-timestamp events.
- trial_number is zero for session/configuration events and 1..N for trials.
- payload_json is properly CSV-escaped JSON; parse with a CSV reader, then JSON.
- Target events include world position, world rotation, and world scale.
- trial_ended payload reason separates manual_confirmation, timeout, operator_stop,
  task_disabled, component_disabled, or application_quit as applicable.
- Other events include session_started, task_configured, session_ready,
  trial_countdown_started, trial_started, rest_started, rest_ended,
  trial_cancelled, sequence_finished/sequence_stopped, and session_stopped.
- target_appeared/target_disappeared are timestamped AFTER the activation command.

Event times are application command times, NOT measured display/photon onset times.
Pose recording continues during countdowns and rests; use event boundaries to
select intervals in analysis. The manifest's legacy trial_number is session metadata;
the event trial_number identifies repeated trials.

## Validate the export

```powershell
Set-Location 'C:\Users\reaga\VR_Telemetry'
python -B Analysis/validate_session.py 'C:/path/to/session' --output 'C:/path/to/new_report.json'
python -B -m unittest discover -s Analysis -p 'test_*.py' -v
```

The validator now checks event count/schema, timestamps, sequence, trial start/end
matching, target visibility pairing and target positions, and termination reasons.
It preserves hashes for all three source files and summarizes outcomes.

Equal event timestamps are valid; duplicate pose timestamps are not.
A structurally valid early-stop session may still have insufficient pose samples
for measurement; it remains saved and must not be deleted to conceal an aborted run.

## What has and has not been established

Automated checks cover state transitions, repeated clicks, minimum rest, manual
completion, exact-deadline timeout, cancellation, aborts, long stalls, CSV escaping,
and pose/event validation. Hardware tracking validity is still not recorded.

Unity batch scene creation/wiring passed. A Play Mode smoke test exercised actual
panel button callbacks and exported one manual completion plus one timeout:
370 samples, 17 events, no structural validator errors. The test session is marked
SYNTHETIC; it does not test controller ray hits, UI rendering, or actual tracked poses.

The head-relative panel is a prototype convenience and can affect head movement or
attention. Do not interpret scanning/dwell from this UI as a validated cognitive
measure. UI comfort, ray selection, floor alignment, task instructions, and timing
must be tested in the headset before participant collection.

A stationary repeat after tracking settles and the independent square calibration
still gate trusting physical movement measurements. Building this task has not
marked Phase 0 complete.

## Next combined headset check (when ready)

- Import/compile without errors and open this room.
- Confirm floor height before recording.
- Begin a session; verify Ready/countdown and a single target appearance.
- Confirm one trial manually, let another time out, and verify both reasons in export.
- On a separate run, stop during countdown and during an active trial.
- Check the panel remains selectable after approaching and while resting.
- Validate the exports and retain failures with notes.
- Complete the postponed stationary/square calibration in ValidationRoom2x2.
