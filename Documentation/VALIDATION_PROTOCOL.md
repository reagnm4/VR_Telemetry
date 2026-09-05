# Phase 0 — recording integrity and exact square validation

Updated 2026-09-04 (America/New_York). Read DESIGN_DECISIONS.md for rationale.
This procedure is self-testing of the instrument, not a behavioral experiment.

## 1. Unity preparation

1. Open the project in Unity 6000.5.2f1 and allow scripts to import. Check the Console
   for compile errors before Play. Existing SampleScene edits have been preserved.
2. Save a separate validation scene copy so the current layout remains available.
3. On TelemetrySystem > Telemetry Logger, check the HMD and controller references.
   Assign **Xr Origin** to the XR Origin root (not Camera Offset or Main Camera).
   Keep the XR Origin scale at (1,1,1).
4. Use **Tools > VR Telemetry > Create Exact 2m Validation Platform** once in the
   validation scene. The new root starts at world origin. Use one supporting floor
   surface; reposition/disable overlapping old floor geometry in this scene copy.
5. Confirm the generated platform's scale is (2,0.05,2), position is (0,-0.025,0),
   and root scale is (1,1,1). Its top is Y=0; X/Z span is exactly two meters.
   The four marker centers are at X/Z = +/-1. Do not measure marker outside edges.
6. Save the scene. Target rate 72, auto-start enabled, auto-stop 30 seconds for the
   first stationary run. Use participant P000 and condition stationary_self_test.

The platform's geometry is exact by construction; headset tracking scale and the
human path still require measurement. Inspect the platform in Unity before walking.

## 2. First headset recording: stationary

- Put on the headset and keep controllers tracked. Start Play, remain approximately
  stationary for the 30-second recording, and allow auto-stop to export.
- Do not try to suppress ordinary head sway. Do not recenter during this baseline.
- Note whether this is Quest Link, Air Link, or standalone Android.
- Record obvious stalls, headset removal, and whether controllers were visible.
- Find the new GUID-named session in the Console output. Editor sessions normally
  live under AppData/LocalLow/DefaultCompany/VR_Telemetry/sessions.
- Confirm schema_version is 0.2.0 and origin_reference_assigned is true.

## 3. Validate before interpreting

From the repository root, use a new output filename for each report:

```powershell
python Analysis/validate_session.py "C:/path/to/session" --output stationary_report.json
```

Exit 1 indicates integrity errors. Exit 2 indicates unreadable/invalid input.
Exit 0 means no integrity errors, but inspect warnings. All current recordings warn
that hardware tracking validity is unverified. An origin_nonfinite_pose warning means
the origin wasn't available and needs checking before movement interpretation.

Expected: zero non-increasing intervals; monotonically increasing Unity frame numbers;
finite HMD poses; valid quaternion norms; manifest count matching CSV row count.
Record effective rate, missed_sample_deadlines, max gap, and origin availability.
Do not increase the rate or fill gaps just to make the report appear successful.

The validator never certifies the human square test. Old sessions may fail it;
preserve them. Missing controller data can make controller-based features unsuitable
even when HMD-based integrity checks pass.

## 4. Second recording: physical square

After reviewing the stationary report:

1. Use condition physical_square_self_test, auto-stop 60 seconds.
2. Align a known physical 2 x 2 m route with the virtual reference where practical.
   A rendered marker alone is not independent evidence of physical scale.
3. Begin near one corner, pause briefly, walk all four sides, pause at each corner,
   and return to the start. Keep thumbstick movement and turning unused for this run.
4. End with a few gentle left/right head turns while stationary.
5. Validate the export, then inspect the X/Z path using the existing plotter:

```powershell
python Analysis/analyze_session.py "C:/path/to/session"
```

Record route completion, X/Z spans, plot shape, observed leaning/corner cutting,
and whether the origin stayed stable. Do not require the head trace to equal exactly
2.000 m. Human spatial tolerances have not yet been agreed or empirically established.
If the physical space does not support the route, do the stationary test first;
use a separately labeled thumbstick test without calling it physical calibration.

## 5. Later targeted checks

- Separate thumbstick trial: verify world motion differs from origin-relative motion.
- Tracking interruption: a frozen finite pose can still occur; current schema cannot
  reliably label tracking loss. This test informs the next implementation, not a pass.
- Headset suspend/resume: characterize lifecycle/export behavior before long recordings.
- Deterministic Unity trajectory: exercise exact poses through the logger/exporter.
  This end-to-end software test is still pending, distinct from scheduler assertions.

## 6. Automated regression checks

```powershell
python -m unittest discover -s Analysis -p "test_*.py" -v
```

In a fresh PowerShell session, compile and execute the isolated production scheduler:

```powershell
Add-Type -Path @("Assets/Scripts/SampleSchedule.cs", "Analysis/SampleScheduleTests.cs")
[SampleScheduleTests]::Main()
```

## Report back

Provide the new session folder name, recording mode, Console errors (if any), and
what you physically did. Codex can inspect local exports and append findings to
DESIGN_DECISIONS.md. No need to commit recordings to GitHub for this review.
