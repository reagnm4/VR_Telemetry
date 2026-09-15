# Design decision timeline — VR Behavioral Telemetry

Established: 2026-09-04 (America/New_York; implementation work crossed into 2026-09-05 UTC).
Maintainers: Reagan and Codex.
Purpose: preserve what we chose, when, why, what evidence informed it, and what would make us reconsider.

## How this record works

This is a decision history, not a claim that every idea is already implemented.
Keep old entries. If a decision changes, add a new entry that names the superseded ID;
do not silently rewrite the original rationale. Correct factual mistakes with a dated
correction that preserves the original context. Link relevant code, tests, reports,
and commits once those exist. Never invent a historical decision date or a commit ID.

For each substantial change, record: local date and timezone, participants/source,
status, problem, decision, alternatives, rationale, consequences, evidence, remaining
uncertainty, and revisit trigger. Record unsuccessful approaches when their failure
teaches something relevant. Routine typo fixes do not require a design entry.

Status vocabulary:
- Agreed direction: accepted in discussion; implementation can still be pending.
- Implemented, software checked: code exists and specified automated checks passed.
- Headset validation pending: physical/runtime behavior has not been demonstrated.
- Proposed: recommendation that is not yet a settled project choice.
- Superseded: replaced by a later entry, linked by ID.

Implementation status below describes this first integrity change set. It does not
certify the scene, headset, participant protocol, or overall Phase 0 milestone.

## Timeline index

| ID | Recorded date | Decision |
|---|---|---|
| D001 | 2026-09-04 | Preserve the platform-first scope |
| D002 | 2026-09-04 | Repair measurement integrity before new environments |
| D003 | 2026-09-04 | Make the validation platform exactly 2 x 2 meters |
| D004 | 2026-09-04 | Separate software geometry tests from human movement tests |
| D005 | 2026-09-04 | Treat the digital twin as a partial measured representation |
| D006 | 2026-09-04 | Observe once per eligible update; never backfill |
| D007 | 2026-09-04 | Use one elapsed-time clock and explicit frame semantics |
| D008 | 2026-09-04 | Add optional XR Origin and version the schema |
| D009 | 2026-09-04 | Build a non-destructive integrity validator |
| D010 | 2026-09-04 | Harden session identity, serialization, and export lifecycle |
| D011 | 2026-09-04 | Preserve scene edits and make geometry creation opt-in |
| D012 | 2026-09-04 | Introduce event logging before Phase 1 behavioral trials |
| D013 | 2026-09-04 | Keep observable behavior separate from inferred constructs |
| D014 | 2026-09-04 | Maintain an explicit test and evidence ledger |

## Historical baseline (reconstructed, not newly dated decisions)

PROJECT_SCOPE.md identifies version 0.2, dated June 25, 2026. It establishes raw pose
logging, quaternions, Unity world coordinates, a square-test gate, and descriptive
analysis before machine learning. Those are inherited decisions. This timeline
does not claim to know the precise day each underlying idea was first discussed.

Repository history reviewed:
- fc09b2e: baseline described as a dry-fire pipeline passing at 72 Hz, pre-headset.
- 606796a: floor tracking origin, zeroed rig, DX11 editor, stick locomotion testing.
Commit subjects establish what was recorded in history, not independent verification.

At the start of this work, SampleScene.unity had an existing unstaged modification
(396 insertions and 283 deletions in the earlier assessment). Reagan explained the
platform sizing edits were related to wanting a precise square. We preserved them.

## D001 — Platform-first scope

Recorded: 2026-09-04, America/New_York.
Source: inherited scope and current discussion.
Status: agreed direction, ongoing.

Decision: build a reusable behavioral measurement platform, beginning with reliable
raw observations and controlled environments. Keep the Object-Approach Room ahead
of the instrumented corridor; defer eerie comparisons and ML until their prerequisites exist.

Why: unreliable timestamps or missing measurement context cannot be repaired by
adding environments or more sophisticated models. A reusable data layer makes later
experiments cheaper and their outputs comparable.

Alternatives: build the eerie corridor immediately; start with a full maze; train a
classifier on the existing test recordings. These were not chosen as the next task.

Consequences: early progress is judged by measurement evidence, not visual richness.
The summer calendar in the original scope is historical; use milestone completion
rather than treating those dates as a current delivery promise.

Revisit when: a validated recording pipeline and a repeatable first task exist.

## D002 — Integrity before expansion

Recorded: 2026-09-04.
Source: Codex code assessment; Reagan accepted the proposed priority.
Status: implemented improvements; headset validation pending.

Evidence: the previous logger called RecordSample(now) repeatedly from a catch-up
while loop. Every iteration used the same timestamp and current transforms.
The latest inspected legacy session, 20260711_201139_P001, contained 4,168 rows
and 1,206 non-increasing timestamp intervals. Its maximum interval was about
2.11914 seconds; its overall row rate was about 69.51 Hz.

Decision: stop manufacturing catch-up rows and add validation that rejects
non-increasing timestamps before derivative-based analysis.

Why: zero-duration intervals cannot represent distinct timed observations for speed
and acceleration. An aggregate row rate can disguise the problem.

Important boundary: these findings do not imply that every legacy position is useless.
Keep the original files. Any later repair/deduplication must create a separately
identified derivative with provenance, not silently modify raw data.

Revisit when: fresh recordings demonstrate monotonic times and documented frame-rate behavior.

## D003 — Exact 2 x 2 meter platform

Recorded: 2026-09-04.
Source: Reagan explicitly requested precise Unity geometry.
Status: builder implemented and compiled; scene placement and headset checks pending.

Decision: provide an exact 2-meter X by 2-meter Z platform. The new builder uses a
unit cube scaled to (2, 0.05, 2), centered at (0, -0.025, 0), so the top surface is
Y=0. The root is unparented with unit scale and no rotation.

Corner centers are (-1,-1), (-1,+1), (+1,-1), (+1,+1) in X/Z meters.
Corner marker thickness does not define the measurement dimensions. Marker colliders
are removed; the platform retains its collider.

Alternative discussed: a 2 x 2 outline on a larger supporting floor. That remains a
possible comfort/layout choice, but does not replace the agreed precise platform.

Why: known geometry removes uncertainty from hand-adjusted plane sizes and provides
a reproducible reference. A Unity Plane primitive has different base dimensions
from a Cube; the builder deliberately uses a cube to make the dimensions explicit.

Consequences: do not scale the root or place it under a scaled parent. Translation
is acceptable; rotation changes how world-axis spans should be interpreted.
The physical room must independently be suitable for the intended walking route.

Revisit when: calibration shows a mismatch between known virtual and physical distances.

## D004 — Two distinct validation layers

Recorded: 2026-09-04.
Source: Codex recommendation accepted as the direction for implementation.
Status: protocol defined; full synthetic pose/export and headset tests pending.

Decision: separate a deterministic software trajectory from a human square walk.
An exact software square tests geometry, units, transforms, and serialization.
A headset walk tests the integrated device, tracking, rig, and recording behavior.

Why: head sway, leaning, and cornering mean a human headset trace is not a geometric
outline. An exact rendered platform does not establish exact human movement.

Alternatives: require human extrema to equal exactly 2.000 m; pass a session merely
because its X/Z spans are near two meters. Both would misstate what was measured.

Current gate: software integrity pass is necessary but not sufficient. Visual route
review, the instructed task, locomotion mode, and calibration context are also needed.
Human spatial acceptance tolerances remain proposed work, not validated thresholds.

Keep stationary, physical-walking, and thumbstick runs separate. An existing
synthetic generator has noise and a simulated dropout; it is a parser/plot exercise,
not a complete Unity hardware validation.

Revisit when: repeatability observations support explicit human-test tolerances.

## D005 — Partial digital twin

Recorded: 2026-09-04.
Source: Reagan's wish for a digital twin at each epoch; discussion refined terminology.
Status: agreed direction.

Decision: call recording instants samples. Reserve epoch for later ML contexts.
Represent what is measured at each sample and identify what remains unknown.

Measured today: headset and controller world poses, observation timestamp, Unity frame.
New optional context: XR Origin world pose.
Not measured today: feet, torso, full-body posture, eye gaze, or physiological state.

Why: reconstructing three tracked objects is useful, but is not a validated full-body
digital twin. Later inferred body poses must have separate provenance and confidence.

Revisit when: a concrete research question justifies additional sensors or inference.

## D006 — Honest sampling

Recorded: 2026-09-04.
Status: implemented, software checked; headset validation pending.

Decision: use a target-rate deadline grid and at most one observation per LateUpdate.
Skip and count missed deadlines. Keep actual observation timestamps. Never synthesize
historical poses by duplicating the current pose.

Alternatives: retain the catch-up loop; assign artificial evenly spaced timestamps;
interpolate poses inside the raw logger; assume FixedUpdate supplies fresh XR poses.
None establishes sensor observations that were not actually read.

Why LateUpdate: it runs after Update, allowing ordinary Update-driven movement to
complete first. It does not guarantee ordering relative to every other LateUpdate
script or XR before-render updates. This is not a claim of exact sensor capture timing.

Consequences: a 60-update-per-second application cannot produce 72 independent
per-update observations per second. The achieved rate may be lower than requested.
The missed-deadline count is an application scheduling measure, not device packet loss.

Evidence: standalone tests execute the exact production SampleSchedule source:
duplicate/early/reversed/invalid times; short stall; resumption; 60 Hz updates with
a 72 Hz target; 144 Hz updates with a 72 Hz target; invalid sampling rate.

References:
- https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.LateUpdate.html
- https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-realtimeSinceStartupAsDouble.html

Revisit when: headset observations show timing or update-order requirements that demand
a device-specific acquisition approach.

## D007 — Clock and frame semantics

Recorded: 2026-09-04.
Status: implemented, compiled; runtime validation pending.

Decision: use the logger's realtimeSinceStartupAsDouble elapsed time for duration
and auto-stop. Keep UTC only for human-readable session start/end labels.
Serialize elapsed seconds with round-trip precision. Record Time.frameCount.

Why: the prior session timer used wall-clock UTC while observations used a realtime
clock. Clock corrections should not decide when a recording ends.
The previous frame column was a row counter, not a Unity frame identifier.

Compatibility consequence: schema 0.1.0 frame remains a sample counter.
Schema 0.2.0 frame means actual Unity frame count and may have gaps.
A monotonic application timer still does not make values hardware acquisition timestamps.

Revisit when: event streams or external devices require clock synchronization.

## D008 — Schema 0.2.0 and origin pose

Recorded: 2026-09-04.
Status: implemented; inspector assignment pending.

Decision: append origin_px/py/pz/rx/ry/rz/rw, and record sampling policy, clock/frame
semantics, missed deadlines, Unity version, origin assignment, and tracking limitations
in the manifest. Preserve existing device columns.

Why: world-space headset motion alone mixes physical device motion with locomotion
of the rig. The origin pose provides context for subsequent coordinate conversion.

Limit: deriving tracking-space pose from origin requires known unit scale and the
appropriate hierarchy. We do not yet record origin scale or a separate Camera Offset
transform. Origin data alone does not label every recenter or locomotion cause.

An unassigned origin writes NaNs and generates a validator warning. This keeps the
existing scene loadable while making missing context visible. Assign it before the
new baseline tests.

Tracking-validity flags and explicit recenter events are not implemented here.
Finite values must not be described as proof that a device was tracked.

Revisit when: tracking validity and recenter handling are added in a documented schema change.

## D009 — Integrity validation with explicit limits

Recorded: 2026-09-04.
Status: implemented, 15 automated tests passed, legacy regression checked.

Decision: add a standard-library validator that reads both supported schemas and
emits JSON, source-file SHA-256 hashes, errors, warnings, metrics, and thresholds.
Do not overwrite raw sessions or existing report files.

Checks include required/duplicate columns, sample count, numeric timestamps,
strictly increasing times/frames, finite poses, quaternion norm, duration bounds,
and target rate. Missing HMD poses are errors; missing controller/origin poses are
warnings. Their suitability depends on which downstream feature is being studied.

Provisional engineering warnings: gaps over three target periods; achieved rate
below 90% of target. Quaternion norm tolerance is 0.01. Allow 0.002 seconds for
legacy manifest rounding. These are transparent engineering defaults, not validated
research acceptance criteria.

Always emit hardware_tracking_validity_unverified for these schemas.
Always emit square_test_status = not_assessed. Exit code 0 means no integrity errors,
not that all warnings are resolved or Phase 0 is complete.

Why: a successful plot is not sufficient evidence. Saved reports make the gate inspectable.
The earlier plotting tool remains a visualization utility; run the validator first.

Revisit when: tracking flags and task-specific acceptance rules are available.

## D010 — Session lifecycle

Recorded: 2026-09-04.
Status: implemented, compiled; Unity lifecycle behavior pending.

Decision: add a GUID to session folder names, snapshot metadata at start, serialize
with JsonUtility, require a logger, and retain failed exports for retry.
Stop/export when SessionManager is disabled as well as on normal auto-stop/quit.

Why: second-resolution names plus participant ID can collide; hand-built JSON can
break on quotes; changing inspector labels during a run should not relabel it.
A failed export should not silently permit a new session to clear its buffer.

Manifest is written after telemetry. This is not a transactional or crash-proof
storage system. Hard process termination, power loss, or a partial disk write can
still lose data. Periodic checkpointing remains future work.

Revisit when: sessions get longer, headset suspend behavior is characterized, or
collection moves beyond development/self-testing.

## D011 — Preserve the scene under active editing

Recorded: 2026-09-04.
Source: existing user edits plus the explicit square preference.
Status: implemented as an opt-in editor command.

Decision: do not rewrite SampleScene.unity. Add Tools > VR Telemetry >
Create Exact 2m Validation Platform. It creates new geometry with Undo support;
it does not delete objects or save the scene.

Why: the unstaged scene contains the user's ongoing environment work.
A generated reference gives a reproducible starting point without guessing which
existing objects should be replaced.

Consequence: place it in a saved copy/validation scene with an XR rig. Avoid
overlapping floor surfaces. Wire the optional origin reference manually.
Scene placement, visual inspection, and headset use remain pending.

## D012 — Events belong before Phase 1 trials

Recorded: 2026-09-04.
Status: agreed next-stage direction; not implemented in this change set.

Decision: introduce trial/stimulus timestamps before Object-Approach experiments.
The original scope postpones events to Phase 2 but already asks for Phase 1
stimulus-response measures. Record this inconsistency explicitly.

Why: target appearance, trial boundaries, and target location are necessary context
for approach timing and event alignment. Pose-only data cannot reconstruct them reliably.

Revisit: define the common clock and event payload before building the first task.
Do not silently claim the scope text has already been updated.

## D013 — Research interpretation remains conditional

Recorded: 2026-09-04.
Status: ongoing methodological direction.

Decision: distinguish observed behavior from proposed cognitive explanations.
A fixed route reduces route-choice variance; it does not eliminate learning,
locomotion, tracking, or individual-difference explanations.
Head scanning does not by itself establish anxiety, attention, or vigilance.

Why: the platform's usefulness depends on claims matching the available evidence.
The first comparisons remain feasibility work. No clinical interpretation is added.

Future protocol decisions: choose one initial manipulation, operationalize outcomes,
control order/learning, document locomotion settings, and separate exploratory
analyses from prespecified ones. These are future decisions, not a completed protocol.

## D014 — Evidence ledger and next checkpoint

Recorded: 2026-09-04.
Status: software checks completed; headset checks pending.

Completed:
1. Read scope, setup guide, logger, session manager, analyzer, synthetic generator.
2. Inspected existing scene status without modifying it.
3. Ran 15 Python integrity-validator tests successfully.
4. Compiled new/changed C# against installed Unity 6000.5.2f1 managed assemblies
   using a temporary .NET compile harness.
5. Executed production scheduler assertions successfully.
6. Validated legacy session 20260711_201139_P001: correctly rejected its 1,206
   non-increasing intervals. No raw data was changed.

Compilation is a static compatibility check, not a Unity Play Mode test.
The compile harness is a workspace verification artifact, not a new project dependency.
No claim is made that JsonUtility export, the geometry menu, or XR poses were
executed inside Unity during these checks.

Pending:
- Import scripts in Unity and inspect Console.
- Assign xrOrigin and confirm unit rig scale.
- Create/inspect the exact platform in a validation scene.
- Stationary and physical-square recordings with schema 0.2.0.
- Separate thumbstick recording if that mode is used.
- Observe tracking interruption and headset suspend behavior.
- Full deterministic Unity pose-to-CSV trajectory test.
- Explicit tracking validity and recenter events.
- Agree measured tolerances; record evidence before declaring Phase 0 passed.

Reagan offered to boot Unity and the headset for testing. The next checkpoint is
the short procedure in VALIDATION_PROTOCOL.md. Attach reports and observations
to a new dated entry here, including surprises or failures.

## D015 — Standalone headset testing handoff

Recorded: 2026-09-04 (America/New_York).
Source: Reagan requested a detailed checklist to test independently the following day.
Status: documentation prepared; no new headset evidence yet.

Decision: provide TOMORROW_TEST_CHECKLIST.md with preparation, scene preservation,
exact Inspector values, stationary recording first, conditional physical-square and
optional thumbstick runs, validation commands, troubleshooting, and an evidence template.

Why: testing should not depend on a live chat or on remembering earlier discussion.
The checklist separates known schema limitations from new failures and gives clear
reasons to stop rather than changing unrelated settings to make a test pass.

Implementation: documentation only. Uses the existing validator and geometry command.
No new numerical human-movement acceptance threshold is introduced. Failed runs remain
useful evidence and raw recordings are preserved. No Git reset or blanket staging is
part of the procedure.

Review: commands and field names checked against the implemented scripts and protocol.
The actual checklist has not yet been executed with the headset. Append the results
as a new entry, rather than changing this handoff into a retrospective claim of success.

## D016 — Boundary calibration resolved apparent excess headset height

Recorded: 2026-09-06 (America/New_York).
Source: Reagan's guided Air Link self-test and supplied Unity screenshots.
Status: positioning issue resolved by user report; recording validation pending.

Context: Reagan created ValidationRoom2x2, inspected the generated platform and
assigned the logger's XR Origin reference. Before recording, the headset view felt
too high. Reagan reports standing height of 5 ft 11 in (approximately 1.8034 m).

Evidence before calibration: during Play Mode the XR Origin Y was approximately
zero (-4.47e-08), Camera Offset Y was zero, and Current Tracking Origin Mode was
Floor. Main Camera local Y was 2.233746 m. The installed XROrigin implementation
sets Camera Offset Y to zero in Floor mode, consistent with the runtime screenshot.
The excessive height was therefore not explained by the earlier edit-mode camera
offset of 1.36144 m.

Action and observation: Reagan recalibrated the Quest boundary/floor reference,
reconnected Air Link, and repeated the positioning check. The subsequent screenshot
shows Main Camera local Y = 1.64285 m; Reagan reports the boundary change solved
the problem and the view now feels realistic. The approximately 0.591 m difference
is between two snapshots, not a controlled measurement of calibration error.

Decision: check the headset floor reference before compensating with Unity camera
offsets, rig scale, or platform height. No compensating Unity code change was made.
Use a no-recording positioning check before the first baseline after boundary changes.

Why: changing virtual geometry to cancel an external tracking-reference error could
hide a calibration problem and compromise meter-scale interpretation.

Limits: snapshots and the user's report strongly support the boundary explanation;
they do not establish exact physical eye-height accuracy or certify tracking scale.
The latest screenshot alone does not reverify every ancestor transform. No new raw
session has yet been assessed. Stationary recording integrity and the square test
remain pending. No claim is made that Phase 0 has passed.

Next checkpoint: 30-second stationary_self_test, P000, Air Link; validate the new
schema 0.2.0 export before physical-square recording. Revisit calibration if floor
height becomes implausible or the boundary/tracking reference changes.

## D017 — First headset export passes structural checks but includes startup transients

Recorded: 2026-09-06 (America/New_York).
Source: Reagan's Console screenshot and read-only analysis of the exported files.
Session: 20260906_210440_db78b8cce4ef4c33afe1aab0bc23ab12.
Metadata: P000, stationary_self_test, trial 1, Air Link; schema 0.2.0.

Evidence: 1,994 samples over a manifest duration of 30.002404 seconds; 166 missed
deadlines. Validator reports no integrity errors, zero non-increasing timestamp
intervals, no nonfinite poses, and no quaternion-norm errors. Overall observation
rate is 66.5489 Hz. Warnings are gap_over_three_periods and the expected
hardware_tracking_validity_unverified. All four pose references produced finite data.

Largest gap is 1.895019 seconds, from t=0.0402433 to 1.9352623, across one Unity
frame increment. Four intervals exceed three target periods. Excluding only this
largest gap yields about 71.0087 Hz, a diagnostic calculation rather than a repaired
dataset or a redefinition of the full-session rate.

The first row has head Y=1.36144; shortly afterward head Y=0.10001 and origin
Y=0.10001. After t>=3 seconds, head Y ranges 1.47041 to 1.66714 and the origin
position is constant. Finite startup poses thus pass structural checks without
establishing valid settled tracking. Exact causes of each transient remain unproven.

Decision: retain this as startup/engineering evidence, not a clean stationary
baseline or square-test pass. Establish settled tracking before recording the next
baseline. A delayed or explicit start is the proposed next recording-control
improvement; it has not been implemented in this entry. Do not silently trim raw data.

Report saved in the task workspace as vr-integrity-work/stationary_20260906_210440_report.json,
including SHA-256 hashes of the source manifest and telemetry. Raw exports unchanged.
Next checkpoint: agree/implement a post-initialization start, then repeat the baseline.

## D018 — Explicit manual recording start after XR initialization

Recorded: 2026-09-06 (America/New_York).
Source: first baseline's startup transients (D017), followed by Reagan asking to proceed.
Status: implemented; static compilation checked; headset repeat pending.

Decision: expose Start Recording and Stop Recording and Save in the Session Manager
component context menu. Default auto-start to false for newly added components.
Existing serialized scene settings retain their values and must be unchecked explicitly.
Start refuses Edit Mode or a disabled component. The existing 30-second auto-stop
remains measured from the actual StartSession call, not from entering Play Mode.

Why: the operator can allow Air Link and tracking to initialize, check floor height,
then start the baseline without restarting Play Mode. A fixed delay alone cannot
prove tracking readiness. This is an operator readiness check, not an automatic
tracking-validity guarantee. A headset controller binding or countdown can follow
if desktop interaction materially affects the protocol.

Validation: changed C# compiled against the installed Unity assemblies and existing
production scheduler assertions executed successfully. Unity menu interaction and
recording behavior require the next headset run. Raw session files and scene edits
were preserved; schema remains 0.2.0.

Procedure: outside Play, disable Auto Start On Play, set stationary_self_test trial 2
and auto-stop 30 seconds, and save. Enter Play, wait until view and tracking settle,
then use TelemetrySystem > Session Manager header menu > Start Recording. Remain
stationary with tracked controllers until export. Record any setup movement; do not
silently remove it. The checklist's immediate-on-Play baseline instruction is
superseded for this guided validation by this manual-start procedure.

Revisit when: manual starts are inconvenient, tracking flags are implemented, or
trials require precise stimulus/task onset independent of operator interaction.

## D019 — Professor interest retained as a deferred opportunity

Recorded: 2026-09-14 (America/New_York).
Source: Reagan's report of a meeting with his OS professor.
Status: contextual note only; explicitly deferred by Reagan.

The professor is interested in this project and needs telemetry logging in his own
platform. A possible much-later direction is a functioning reusable tool invoked
through an API. Reagan explicitly requested that this perspective NOT drive current
iteration. No API, SDK, service abstraction, distribution packaging, compatibility
promise, or external integration is being designed or built for that purpose now.
Revisit only when Reagan explicitly brings the integration into scope.

## D020 — Continue task development while physical validation is pending

Recorded: 2026-09-14.
Source: Reagan asked whether headset testing could wait, then approved building the
next task flow after tradeoffs were discussed.
Status: agreed development sequence; measurement validation remains incomplete.

Decision: build session/trial controls, event logging, and the Object-Approach Room
without requiring another headset session first. Treat validation as a gate before
trusting behavioral data, not as a ban on useful engineering work. No participant
collection, cognitive interpretation, or ML analysis is authorized by this change.

Why: the first export demonstrated basic recording and eliminated duplicate times,
while also exposing startup transients. The next headset session can jointly assess
manual start, square calibration, and the first task, rather than test isolated edits.
The earlier blanket stop on development was broader than necessary. D017's data
limitations and D018's manual-start requirement still apply.

## D021 — Object-Approach controls and trial termination

Recorded: 2026-09-14.
Source: options presented to Reagan; Reagan said to proceed with the recommended
in-headset panel and manual confirmation plus a maximum-duration timeout.
Status: implemented; software checks performed; headset usability pending.

Options discussed: Inspector-only controls are cheaper but awkward in-headset;
an in-headset panel requires more work but supports self-testing and repeated trials.
Timed-only completion standardizes duration but can interrupt an unfinished approach.
Distance thresholds are automatic but do not prove the task is complete. Manual
confirmation records a declaration and introduces a button interaction.

Decision: in-headset Begin/Ready/Complete/Stop controls with Inspector fallbacks.
Start the session only after the operator judges tracking settled. Each trial has
a countdown, stationary neutral target, explicit confirmation or timeout, and rest.
At an exact deadline timeout takes precedence. Stop is available throughout and
cancels a countdown or ends an active trial with a distinct reason.

Implementation defaults: three trials, three-second countdown, 30-second timeout,
minimum five-second rest followed by explicit Ready. These are editable engineering
defaults, NOT an approved final research protocol. A hitch during countdown delays
actual appearance; it does not fabricate an earlier target or consume the active
trial's entire duration. Pose capture continues across rests and countdowns.

Prototype panel placement is head-relative below the central field of view so it
remains accessible after moving. This is a provisional UI implementation, not a
claim of research suitability. It can affect head movement/attention and must be
evaluated before using scanning or facing-time outcomes. The existing XR UI rays
operate its buttons. Controller gestures are not introduced in this iteration.

The target is a 0.3 m neutral sphere at world (0,1.1,1.5), with a blue start marker
at world X/Z zero. The separate room has a 6 x 8 m floor and three walls. This does
not change the two-meter calibration platform. Starting position is instructed,
not automatically verified; no rig teleport or recenter is used between trials.
Locomotion mode is an explicit descriptive field. Physical space is not inferred
from the virtual floor. Keep geometry/settings unchanged within a recording.

Revisit: UI placement/comfort, start-position enforcement, trial duration, target
distance and completion instructions after the combined headset test. Discuss
material alternatives with Reagan before building a different approach.

## D022 — Shared-clock event stream and separate event schema

Recorded: 2026-09-14.
Status: implemented; validator and state-transition checks completed.

Decision: add events.csv with t_sec, sequence, event_type, trial_number, payload_json.
Use the logger's elapsed clock. Sequence disambiguates equal-time events; event times
may be equal while pose times must increase. Trial/stimulus events carry target
world pose/scale and settings. Trial end reasons distinguish confirmation, timeout,
operator stop, component/task disable, and shutdown as applicable.

The pose schema stays 0.2.0. The optional event extension declares its own 0.1.0
schema, filename, and count in the manifest. Old pose-only recordings remain valid
inputs. In repeated-trial sessions, event trial_number is authoritative; the legacy
manifest trial_number remains session metadata. The user need not split each trial
into a separate file to align events and poses.

Activation/deactivation events occur after the SetActive command. They identify
application commands, not measured photons/display onset or verified awareness.
No eye tracking, reaction-time feature, or inferred cognitive state is added.

Stop notifies the task before ending telemetry, allowing target cleanup and terminal
trial events inside the session window. Telemetry and events are written before the
manifest. Failed writes retain buffers for retry; this remains non-transactional
and not crash-proof. Avoid describing the export as durable until it completes.

Validation extends existing pose checks with event schema/count/order, session
boundaries, trial matching, target visibility pairing, finite target coordinates,
termination reasons, and source hashes. Equal timestamps and escaped JSON are
tested. This is structural validation, not proof that a participant followed the task.

Tests: production C# state machine/event buffer assertions cover double clicks,
countdown/rest, manual and timeout completion, exact deadlines, abort/cancellation,
clock reversal, long stalls, and escaping. All 31 Python tests passed. Unity batch
compilation and scene-wiring checks passed; headset interaction remains pending.

Play Mode evidence, September 14 local / September 15 UTC: the first smoke driver
attempted a click before the panel's next-frame interactability update, so it was
changed to wait for the actual button state. That failed synthetic export was
retained. The repeat passed through the real panel button callbacks, produced one
manual_confirmation and one timeout, hid the target, and exported successfully.
Session 20260915_013317_0a193f61fd44497ea9538f4e9f8b29b1 is explicitly labeled
SYNTHETIC / synthetic_editor_smoke. It contains 370 pose samples and 17 events.
The full Python validator found no structural errors and both expected outcomes.
Timing gaps, unavailable hardware validity, and unverified display onset remain
warnings. This test used no headset and no actual controller ray selection; it is
not a clean behavioral baseline or validation of UI appearance/comfort.

Validator version is now 0.2.0 to identify the added event checks separately from
the unchanged pose schema. Batch logs and synthetic validation reports are in the
task workspace's vr-object-approach folder; raw synthetic sessions stay outside Git.

## Template for the next entry

### D023 — [Decision title]
Recorded local date/time and timezone:
Source/participants:
Status:
Problem and evidence:
Decision:
Alternatives considered:
Why selected:
Implementation and affected files/schema:
Validation performed and result:
Limits / pending tests:
Revisit trigger:
Supersedes / superseded by:
Commit or report reference (only when actually available):
