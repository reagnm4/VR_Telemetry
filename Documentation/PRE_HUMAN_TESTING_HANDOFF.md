# Pre-human-testing engineering handoff

Updated 2026-10-03, America/New_York.

This is the boundary between what software checks can establish and what Reagan must
evaluate in the headset. It does not mark Phase 0 complete and does not authorize
participant collection.

## Completed without a headset

- Pose recording uses real monotonic observation times, never duplicate catch-up rows.
- Missed sampling deadlines are counted rather than fabricated.
- Session metadata, pose schema 0.2.0, optional event schema 0.1.0, and coordinate
  conventions are explicit.
- Repeated Object-Approach trials have deterministic countdown, timeout, rest, manual
  completion, early-stop, and final-save state transitions.
- Trial and target events share the telemetry clock and equal-time ordering is explicit.
- Structural validation checks poses, quaternions, timestamps, frame progression,
  event counts/order, session boundaries, trial lifecycle, target visibility, hashes,
  and source paths.
- Descriptive Object-Approach analysis refuses invalid sessions and keeps measured
  geometry separate from head-direction proxies and interpretation.
- Session shutdown stops capture even if a task callback fails. Export remains retryable,
  uses temporary files, and publishes the manifest last.
- Synthetic tests cover the state machine, CSV/JSON escaping, invalid input, event
  lifecycle, feature calculations, short-trial retention, and compatibility with
  pose-only and non-target event sessions. The current Python suite has 39 tests.

## Still requires Reagan in the headset

1. Confirm the Quest boundary/floor remains correct after reconnecting. The earlier
   correction produced a plausible standing headset height but is one observation.
2. Repeat the manually started stationary recording after tracking settles. Validate
   timing, pose continuity, controller availability, and startup behavior.
3. Complete the physical 2 x 2 m square walk and assess the plotted dimensions and shape.
   A structural validator pass is not a square-test pass.
4. Open ObjectApproachRoom and assess panel readability, reachability, ray selection,
   comfort, obstruction, and whether it changes natural head behavior.
5. Run at least one manual completion, one timeout, one stop during countdown, and one
   stop during an active trial. Retain every export, including failures.
6. Confirm the target appears once, stays fixed, disappears at completion, and does not
   reappear during rest. Check that Ready never starts automatically.
7. Validate each export, then run the descriptive analyzer. Compare reported trial
   windows and outcomes with what actually happened before interpreting distances.

## Decisions deliberately left open

- Final trial count, countdown, timeout, and rest duration.
- Whether physical walking, thumbstick motion, or separately analyzed modes belong in
  the research protocol.
- Whether the panel remains head-relative or moves to a neutral world-space station.
- Enforced start-position tolerance and whether enforcement would alter behavior.
- Distance-to-center versus distance-to-object-surface for the primary endpoint.
- Operational definitions for dwell, slowdown point, exclusions, and valid completion.
- Any inference about attention, avoidance, anxiety, cognition, or clinical state.

These choices should use the headset evidence and be discussed before implementation.
The professor/API direction remains recorded as deferred context and has not shaped
this pass.

## Commands after a run

From `C:\Users\reaga\VR_Telemetry`:

```powershell
python -B Analysis/validate_session.py 'C:/path/to/session' --output 'C:/path/to/integrity_report.json'
python -B Analysis/analyze_object_approach.py 'C:/path/to/session' --output 'C:/path/to/approach_report.json'
```

Never edit or replace the session folder to make a validator pass. Fix the software or
procedure, record a new session, and retain the failed run with notes.
