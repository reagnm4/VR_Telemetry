# VR Behavioral Telemetry — Milestone 1 Setup

**Goal of milestone 1:** put on the Quest 2, walk around an empty room with continuous
locomotion, log head + controller pose to CSV, pull it off the headset, and plot the
path in Python. The pass condition is the **square test** (walk a 2x2 m square, confirm
the logged path is a 2x2 m square). Nothing else gets built until this passes.

No maze, no events, no stimuli yet. This step exists to prove the data is trustworthy.

---

## 0. What you need
- Unity 6 LTS (6000.x) or Unity 2022.3 LTS (either is fine).
- Meta Quest 2 + USB-C cable.
- For fast iteration: Meta Quest Link (or Air Link) + the Meta Quest desktop app, so you
  can hit Play in the editor and test in-headset without building an APK each time.
- Python 3.10+ with `pandas numpy matplotlib`.

---

## 1. Create the project
1. Unity Hub > New Project > **3D (URP)** or **3D Core** template. Either works.
2. Open it.

## 2. Install XR packages
Window > Package Manager (set the dropdown to **Unity Registry**), install:
- **XR Plugin Management**
- **OpenXR Plugin**
- **XR Interaction Toolkit** (3.x)

Then in Package Manager, select **XR Interaction Toolkit** > **Samples** tab >
Import **Starter Assets**. This gives you a pre-wired rig with continuous locomotion,
so you do not have to build the input plumbing by hand.

## 3. Enable Quest (Android) build target
1. Edit > Project Settings > **XR Plug-in Management**.
2. Switch to the **Android** tab, check **OpenXR**.
3. Under OpenXR, add the **Meta Quest Support** feature and the
   **Oculus Touch Controller Profile** interaction profile.
4. File > Build Settings > select **Android** > **Switch Platform**.
5. Back in Project Settings, open the **OpenXR > Project Validation** tab and click
   **Fix All**. This auto-resolves most Android/Quest config issues.

## 4. Build the scene rig
1. In the Project window, find the Starter Assets folder. Drag the
   **XR Interaction Setup** prefab (or **Complete XR Origin Set Up**) into your scene.
   It already includes the XR Origin, Main Camera (the HMD), Left/Right Controllers,
   and continuous move + turn providers.
2. Add a floor: GameObject > 3D Object > **Plane**, scaled up (e.g. 5x5).
   You need something to stand on and a few reference objects so motion is visible.
3. Delete any default "Main Camera" that is NOT under the XR Origin (avoid two cameras).

## 5. Add the telemetry scripts
1. Put `TelemetryLogger.cs` and `SessionManager.cs` in your project's `Assets/Scripts/`.
2. Create an empty GameObject in the scene, name it **TelemetrySystem**.
3. Add Component > **Telemetry Logger** and **Session Manager** to it.
4. On **Telemetry Logger**, drag in:
   - `hmd` = the **Main Camera** under XR Origin (Camera Offset).
   - `leftController` = the **Left Controller** object.
   - `rightController` = the **Right Controller** object.
5. On **Session Manager**:
   - Drag the **Telemetry Logger** component into the `telemetry` slot.
   - Set `participantId` (e.g. `P001`), `environmentId` = `empty_room_test`.
   - Set `autoStopAfterSeconds` = `60` so the first run saves itself hands-free.

## 6. Run the square test
1. Connect the Quest via Link/Air Link, or build the APK to the headset.
2. Press **Play**. Put on the headset.
3. Walk a clear ~2 x 2 m square (physically, or with the thumbstick if your space is small).
4. After 60 s it auto-saves. Watch the Console for
   `Session stopped. N samples written to ...`.

## 7. Pull the data off the Quest
Data lands in `Application.persistentDataPath/sessions/<session_id>/`.
On Quest that path is:
```
/sdcard/Android/data/<your.package.name>/files/sessions/<session_id>/
```
Find `<your.package.name>` in Project Settings > Player > Other Settings > Package Name.

Pull with adb:
```
adb pull /sdcard/Android/data/<your.package.name>/files/sessions ./sessions
```
(Or use the Meta Quest Developer Hub file manager, or SideQuest's file browser.)
If you tested over Link in the editor, the files are just on your PC's persistent path
(on Windows: `C:\Users\<you>\AppData\LocalLow\<Company>\<Product>\sessions\`).

## 8. Validate in Python
```
pip install pandas numpy matplotlib
python analyze_session.py ./sessions/<session_id>
```
Check the printout:
- **X span** and **Z span** each ~2.0 m after a 2x2 m walk.
- **effective rate** close to the target (72 Hz).
- The top-down plot looks like the square you walked.

If those line up, milestone 1 is done and the data layer is trustworthy.

---

## Data schema (v0.1.0)

**manifest.json** — one per session:
`schema_version, session_id, participant_id, environment_id, condition,
trial_number, start_utc, end_utc, duration_sec, sample_rate_hz, sample_count,
coordinate_system, rotation_format, telemetry_file`

**telemetry.csv** — one row per sample:
```
t_sec, frame,
hmd_px, hmd_py, hmd_pz, hmd_rx, hmd_ry, hmd_rz, hmd_rw,
lc_px,  lc_py,  lc_pz,  lc_rx,  lc_ry,  lc_rz,  lc_rw,
rc_px,  rc_py,  rc_pz,  rc_rx,  rc_ry,  rc_rz,  rc_rw
```
- `t_sec` = seconds since session start (not evenly spaced; use it, do not assume dt).
- Positions in meters, Unity world space (left-handed, Y up).
- Rotations are quaternions (x,y,z,w). Derive Euler/forward in Python, never log Euler.
- Missing transforms write `NaN`.

**Why raw, not features:** velocity, acceleration, pauses, head-turn rate, path length,
and path efficiency are all computed in Python from this raw stream. That keeps Unity
simple and lets you recompute or fix any feature without re-running a single session.

---

## What's next (after the square test passes)
1. Build the first maze / hallway environment (geometry + a clear exit).
2. Add an **EventLogger** (timestamped discrete events: enter region, reach goal, sound played).
3. Add a **StimulusManager** (lights, spatial audio cues) writing into the event stream.
4. Extend the Python side with a feature extractor (path length, pauses, head-turn rate,
   facing time) and comparison plots across sessions.
5. Run a small counterbalanced pilot (two matched mazes, neutral vs eerie) and treat it
   as a feasibility study, not a powered experiment.
