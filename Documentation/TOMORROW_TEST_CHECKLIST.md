# Headset validation checklist — first integrity build

Prepared September 4, 2026 (America/New_York), for the next testing session.
Project: C:\Users\reaga\VR_Telemetry
Branch: codex/recording-integrity
Implementation commit: 8eaa0b4
Unity version used for the compilation check: 6000.5.2f1

## What you are trying to establish

Can the new logger produce a readable, correctly timed recording on your headset,
with the right transforms assigned and a geometrically exact reference platform?

You do not need to finish every optional test. The minimum useful result is:

- A clean Unity compile.
- One 30-second stationary recording and its validation report.
- A saved validation scene containing the exact platform.
- One physical square recording if the stationary check is satisfactory and space permits.

This is self-testing, not participant collection. Software checks already passed:
15 Python validator tests, production scheduler assertions, and static C# compilation.
Unity Play Mode, headset behavior, and physical calibration have NOT been certified.

A successful report is not an automatic Phase 0 completion certificate.
Human spatial tolerances and actual tracking-validity logging remain unresolved.

## A. Before opening Play Mode

- [ ] Charge the headset/controllers and use your usual working Quest connection.
- [ ] Choose ONE mode for the first tests: preferably your already-working editor
      Link/Air Link setup. Record which one. Do not change transport mid-comparison.
- [ ] Ensure the physical area supports the route with clearance; do not disable the
      headset boundary to fit the square. If it does not fit, skip physical walking.
- [ ] Open PowerShell and run:

```powershell
Set-Location -LiteralPath 'C:\Users\reaga\VR_Telemetry'
git branch --show-current
git log -1 --oneline
git status --short
```

- [ ] Branch reads codex/recording-integrity. Later documentation commits may appear
      above 8eaa0b4; that is fine. The implementation must be present.
- [ ] Existing SampleScene.unity changes are expected. Do not discard/reset them.
- [ ] Confirm these files exist: Assets/Scripts/SampleSchedule.cs,
      Analysis/validate_session.py, Documentation/DESIGN_DECISIONS.md.
- [ ] No pull is necessary on the same PC if these changes are already present.
      If the branch is wrong or files are missing, stop before switching branches
      over uncommitted work. Record the status for follow-up.

Create a local results folder OUTSIDE the repository, so reports are not accidentally
mixed with code changes:

```powershell
$runTag = Get-Date -Format 'yyyyMMdd_HHmmss'
$resultsFolder = Join-Path 'C:\Users\reaga\VR_Telemetry_TestResults' $runTag
New-Item -ItemType Directory -Path $resultsFolder -Force | Out-Null
$resultsFolder
```

- [ ] Keep this PowerShell window open; later commands use $resultsFolder.
- [ ] Copy the results-folder path into your notes.
- [ ] Create a notes.txt there using a text editor and the template at the end.

## B. Open Unity and check compilation

- [ ] Open C:\Users\reaga\VR_Telemetry in Unity.
- [ ] Allow imports and compilation to finish before entering Play.
- [ ] Open the Console. Resolve no errors by guessing or deleting scripts.
- [ ] If red compile errors appear, copy the FIRST error and its stack/details to
      notes.txt. Do not attempt headset measurements until compilation is clean.
- [ ] Confirm the Tools menu contains:
      VR Telemetry > Create Exact 2m Validation Platform.
- [ ] Do not click it repeatedly: each click creates a new platform.

Optional automated regression check, before Unity testing:

```powershell
python -B -m unittest discover -s Analysis -p 'test_*.py' -v
```

Expected: 15 tests, OK. If Python is not available, you can still check Unity and
collect an export, but mark analysis pending rather than declaring success.

## C. Preserve the existing scene and create the reference

- [ ] Open your working SampleScene.
- [ ] OUTSIDE Play Mode, save its current intended edits, then use Save As to make
      a separate scene such as Assets/Scenes/ValidationRoom.unity.
- [ ] Confirm the active scene is the validation copy before adjusting geometry.
- [ ] Create the exact platform through the Tools menu once.
- [ ] Select ValidationSquare_2m in the Hierarchy.
- [ ] Confirm it is unparented and has these Transform values:

| Object | Position | Rotation | Scale |
|---|---|---|---|
| ValidationSquare_2m root | 0, 0, 0 | 0, 0, 0 | 1, 1, 1 |
| Platform_2m_X_2m_Z_SurfaceY0 child, local | 0, -0.025, 0 | 0, 0, 0 | 2, 0.05, 2 |

- [ ] Confirm platform top surface Y=0, spanning X=-1 to +1 and Z=-1 to +1.
- [ ] Confirm four corner-marker centers are at X/Z = +/-1.
      Their outside edges are not the two-meter measurement.
- [ ] Keep the root unscaled and axis-aligned for the simple X/Z span check.
- [ ] In this scene copy, disable or relocate old floor geometry that overlaps the
      new surface. Do not delete the original scene's objects.
- [ ] Inspect the surrounding support/virtual edge before walking. If the rig can
      fall off or collide unexpectedly, stop and fix the validation layout first.
- [ ] Save the validation scene OUTSIDE Play Mode.

Important: a new two-meter platform does not automatically reposition the XR rig,
align the physical room, or wire the logger. Those are separate checks below.

## D. Wire the logger and session manager

Select TelemetrySystem (or the object with the two recording components).

- [ ] Telemetry Logger > Hmd: the Main Camera under the XR Origin.
- [ ] Left Controller: the left tracked-controller Transform.
- [ ] Right Controller: the right tracked-controller Transform.
- [ ] Xr Origin: the XR Origin ROOT, not Camera Offset or the headset camera.
- [ ] XR Origin root scale: 1,1,1. Check for scaled ancestors too.
- [ ] Sample Rate Hz: 72.
- [ ] Session Manager > Telemetry: this Telemetry Logger component.
- [ ] Participant Id: P000 (self-test identifier; no name needed).
- [ ] Environment Id: validation_room_2m.
- [ ] Condition: stationary_self_test.
- [ ] Trial Number: 1.
- [ ] Auto Start On Play: enabled.
- [ ] Auto Stop After Seconds: 30.
- [ ] Save the scene before Play. Inspector changes made during Play usually do not
      persist; set the next run's values after leaving Play.

Keep your working XR tracking-origin configuration for the initial check.
If the floor/head height is clearly wrong, stop and note the current configuration;
do not compensate by scaling the rig or changing the platform dimensions.

## E. Run 1 — 30-second stationary baseline

- [ ] Be ready in the headset before starting Play; automatic recording begins with
      Play, not when you decide you are ready.
- [ ] Start Play with the same method you normally use. If setup consumes much of
      the recording, label it setup_attempt and repeat.
- [ ] Stand approximately still with controllers normally visible/tracked.
- [ ] Do not use thumbsticks, recenter, remove the headset, or intentionally hide
      controllers during this baseline.
- [ ] Allow ordinary head sway; do not try to be perfectly motionless.
- [ ] Let auto-stop finish. Look for the Console message:
      Session stopped. ... samples written to ...
- [ ] Stop Play after the export message.
- [ ] Copy the EXACT session directory from the Console into notes.txt.
- [ ] Note stalls, unexpected movement, floor-height problems, and connection mode.

Expected editor export root:
C:\Users\reaga\AppData\LocalLow\DefaultCompany\VR_Telemetry\sessions

The Console path is authoritative if company/product settings changed.
Standalone APK recordings are on the headset; use your established export method.
Do not assume the PC folder contains a new Android recording.

## F. Validate Run 1

- [ ] Open the new session folder. It should contain telemetry.csv and manifest.json.
- [ ] Confirm manifest schema_version = 0.2.0.
- [ ] Confirm origin_reference_assigned = true.
- [ ] Confirm participant/environment/condition/trial match what you entered.
- [ ] Confirm sampling_policy = one_observation_per_LateUpdate_no_backfill.
- [ ] Note sample_count and missed_sample_deadlines.

In the same PowerShell window, paste the Console path when prompted:

```powershell
$sessionFolder = Read-Host 'Paste the full session folder path without quotation marks'
$reportPath = Join-Path $resultsFolder 'stationary_trial1_report.json'
python Analysis/validate_session.py $sessionFolder --output $reportPath
$validationExit = $LASTEXITCODE
"Validator exit code: $validationExit"
```

The validator refuses to overwrite an existing report. Use a new filename
(e.g. stationary_trial2_report.json) for a repeat.

- [ ] Report integrity_pass = true.
- [ ] errors is an empty list.
- [ ] non_increasing_intervals = 0.
- [ ] hmd_nonfinite_pose_rows = 0.
- [ ] hmd_invalid_quaternions = 0.
- [ ] origin_nonfinite_pose_rows = 0 for the newly wired origin.
- [ ] Record effective_rate_hz, max_dt_sec, and every warning.

### How to interpret the result

| Result | Meaning | Action |
|---|---|---|
| Exit 0, empty errors | No checked integrity error | Inspect warnings before continuing |
| Exit 1 | Integrity problem | Save evidence; do not use this run for behavioral conclusions |
| Exit 2 | Input/path/manifest problem | Verify folder and files; rerun using a new report name if needed |
| hardware_tracking_validity_unverified | Expected limitation of current schema | Record it; it alone need not stop self-testing |
| origin_nonfinite_pose | Origin missing/unavailable | Exit Play, assign root, save, repeat stationary run |
| lc/rc_nonfinite_pose | Incomplete controller data | Check references; repeat before controller interpretation |
| gap_over_three_periods | Application timing gap | Note setup stalls; repeat a quiet stationary run |
| effective_rate_below_90_percent_target | Below 64.8 Hz at a 72 Hz target | Note connection/settings; repeat without changing target to hide the result |
| non_increasing_timestamps | New timing invariant failed | Stop progression and save the session/report |
| sample_after_manifest_end / count mismatch | Export/timing inconsistency | Stop progression and save evidence |

Repeated timing warnings are useful findings. Do not tune multiple settings at once,
delete rows, smooth the CSV, or treat a high row count as proof of good sampling.
Controller problems need not prevent inspecting HMD data, but this first baseline
should aim for all three devices plus origin being available.

If one repeat still has unexplained integrity errors or major timing problems, you
have completed a useful diagnostic session. Stop there and bring back the evidence.

## G. Optional quick plot of Run 1

Check plotting dependencies:

```powershell
python -c "import numpy, pandas, matplotlib; print('Plot dependencies available')"
```

If missing and you want to install them in this Python environment:

```powershell
python -m pip install numpy pandas matplotlib
```

Then:

```powershell
python Analysis/analyze_session.py $sessionFolder
```

- [ ] Inspect head height and top-down motion. A stationary run should not resemble
      sustained travel, but normal sway/noise is expected.
- [ ] Save the figure using its save button to the results folder.
- [ ] Treat the plotter's old "dropout" labels as missing-value checks, not evidence
      that actual hardware tracking loss was detected.
- [ ] A plot opening successfully is not a validation pass.

## H. Run 2 — physical square (only after a satisfactory baseline)

- [ ] Confirm the physical route fits safely. If using thumbstick motion instead,
      use the separate thumbstick_self_test label and procedure.
- [ ] Measure physical two-meter sides independently if space permits, with clear
      corner references. A virtual two-meter square alone does not test physical scale.
- [ ] Align the route to the virtual square's X/Z directions before recording.
      Do any necessary recentering BEFORE the run and document it.
- [ ] Exit Play. Set condition physical_square_self_test, trial 1, auto-stop 60.
- [ ] Save the scene, then start near one corner.
- [ ] Start Play when ready. Pause briefly at the start.
- [ ] Walk four sides once, pausing briefly at corners; return to the starting corner.
- [ ] Do not use thumbstick movement OR thumbstick turning.
- [ ] Finish with a few gentle head turns while standing still.
- [ ] Remain still if you finish early; allow auto-stop to export.
- [ ] Record which corner/order you used, any deviation, and whether movement felt
      normal. Do not force completion within the timer if the route is uncomfortable.

Repeat Section F using the NEW session directory and physical_square_trial1_report.json.
Then plot and save physical_square_trial1.png.

- [ ] Integrity errors absent.
- [ ] X/Z spans and top-down shape recorded.
- [ ] Plot visibly corresponds to the route actually walked.
- [ ] Note corner cutting, leaning, diagonal alignment, recentering, or accidental
      stick input. These affect interpretation.
- [ ] Do NOT require the headset spans to equal exactly 2.000 m.
- [ ] Do NOT declare success based on spans alone: a diagonal or irregular route can
      cover the same bounding box as a square.

Human spatial acceptance tolerances are not yet established. Mark this run
"captured for review" even when it looks good.

If practical, repeat under the same conditions as trial 2. Change only the trial
number and report filename. Repeatability is more useful than adding a new environment.

## I. Optional Run 3 — thumbstick locomotion

Do this only after the preceding work, if you want to test this mode.

- [ ] Separate session; condition thumbstick_self_test; 30 seconds.
- [ ] Stay physically approximately stationary.
- [ ] Move gently using the stick, then stop. Record which controls you used.
- [ ] Validate and save the report.
- [ ] Expect world-space travel. This does NOT validate physical two-meter scale.
- [ ] Save for later world/origin-relative analysis; the current plotter does not
      yet perform that decomposition automatically.

Skip deliberate tracking loss and suspend/resume testing tomorrow unless you want
a separately labeled diagnostic run. These are known follow-up areas, not required
to complete the first checklist.

## J. Troubleshooting without guessing

| Symptom | Check / next action |
|---|---|
| New menu absent | Wait for compilation; inspect first Console error; verify branch/files |
| No Xr Origin field | Verify new TelemetryLogger source and completed import |
| Session starts before headset is ready | Mark setup attempt and repeat once ready |
| No export files | Use Console path; verify auto-start, logger reference, and export errors |
| JSON schema is 0.1.0 | Likely old session/build; verify timestamp/folder and rebuild if using APK |
| Headset on wrong floor height | Record origin mode and offsets; do not scale rig to compensate |
| Two surfaces flicker or collisions feel wrong | Inspect overlapping floors in validation copy |
| Origin warning despite assigned field | Confirm correct scene/component and saved assignment |
| Report filename already exists | Choose a new report filename; preserve earlier report |
| Python not found | Capture/export remains useful; defer analysis and note it |
| Plot is not a square | Verify actual route, physical vs stick, axis alignment and timing first |
| Zero/very few samples | Check early Play stop, component disabling, startup/export errors |
| Frozen poses appear finite | Known tracking-validity limitation; note behavior, do not call it valid tracking |

If blocked, stop after preserving the first error, exact session folder, and what
happened. Avoid resetting Git, deleting recordings, upgrading packages, or redesigning
the XR rig during this validation task.

## K. Finish and preserve evidence

- [ ] Leave Play Mode before final scene/Inspector edits.
- [ ] Save the validation scene and any intentional setup changes.
- [ ] Retain all raw session folders, including failed attempts.
- [ ] Keep reports, plots, and notes together under $resultsFolder.
- [ ] Run git status --short to see new/modified scene files. This is expected.
- [ ] Do not blindly stage everything; session data does not need to go to GitHub.
- [ ] Note whether Unity remains open and which scene is active.
- [ ] Bring back results-folder path plus session folder names. Codex can read them locally.

Success for tomorrow: useful evidence and reproducible setup, even if a test exposes
a problem. Final hardware validation status will be reviewed from those results.

## Notes template — copy into notes.txt

Test date/time/timezone:
Branch and commit:
Unity version:
Connection: Link / Air Link / standalone:
Active validation scene:
Headset refresh/performance settings, if known:
Any changes made before testing:
XR Origin scale / tracking-origin mode:
Physical route measured independently? How:
Recenter performed before recording?:
Results folder:

Run:
Condition / trial:
Raw session folder:
Intended action:
Actual action and deviations:
Manifest schema:
Origin reference assigned:
Sample count / missed deadlines:
Validator exit:
Integrity pass:
Errors:
Warnings:
Effective rate / max gap:
X span / Z span (square only):
Plot saved as:
Console errors/warnings:
Comfort / floor / collision observations:
Next action taken:

Overall:
[ ] Stationary data captured
[ ] Stationary report reviewed
[ ] Exact platform inspected
[ ] Physical square captured, or reason skipped:
[ ] Repeat captured, or reason skipped:
[ ] Optional thumbstick captured
[ ] Evidence saved
Open questions for review:
