# VR Behavioral Telemetry Platform — Project Scope and Design

**Version:** 0.2
**Date:** June 25, 2026
**Status:** Pre-build. Phase 0 and Phase 1 locked. Later phases provisional.
**Note:** v0.2 folds the environment design and research phasing into Section 8.
It supersedes the separate environment document.

---

## 1. One-sentence scope

A modular VR platform that records human movement, orientation, interaction, and
stimulus-response data in controlled virtual environments, then uses Python analysis
and (later) machine learning to study patterns in navigation, attention, uncertainty,
and cognitive response.

---

## 2. What this is, and what it is not

**It is** a reusable behavioral-telemetry research platform. The unit of value is clean,
trustworthy data collected from people moving through controlled VR scenes, plus the
tooling to analyze that data.

**It is not** a VR horror game, and it is not a diagnostic or medical tool. The eerie
and maze-like environments exist only because they reliably produce measurable behavior.
No clinical or diagnostic claims are made at any phase without formal supervision and
ethical review.

---

## 3. Project context and intent

This begins as a personal and portfolio project for exploration. The explicit longer-term
intent is to mature it into supervised research under a professor or lab, potentially
toward publication. Because of that trajectory, the data layer is built to research grade
from day one (anonymized IDs, versioned schema, reproducible sessions, an explicit
validation step) so that nothing has to be rewritten when a research context is added.

---

## 4. Central research question

Can a VR system collect meaningful human behavioral telemetry that can later be used to
study cognition, emotional response, spatial navigation, and behavioral patterns linked
to cognitive or affective state?

The first concrete sub-question, kept deliberately safe and achievable, is an
environment-response question: does an eerie or unnatural VR environment change navigation
behavior compared to a neutral one? This is an environment classifier, not a mental-health
classifier.

---

## 5. Key design decisions (platform level)

These shape everything downstream. Recorded so future collaborators understand why the
system looks the way it does. (Environment-level decisions live in Section 8.1.)

1. **Log raw, derive in Python.** Unity records only raw poses and discrete events. All
   behavioral features (velocity, acceleration, pauses, head-turn rate, path length, facing
   time) are computed in Python. This keeps Unity small, keeps feature logic in one place,
   and lets any feature be recomputed without re-running a participant.

2. **Continuous smooth locomotion.** Movement uses thumbstick continuous locomotion or
   physical room-scale walking, not teleportation. Teleport produces discrete jumps that
   destroy path, velocity, and acceleration telemetry, which are core signals. The tradeoff
   is motion-sickness risk, managed by short sessions and restrained distortion.

3. **Quaternions, not Euler angles.** Rotation is stored as quaternions to avoid wrap-around
   and gimbal issues. Forward vectors and yaw are derived in Python.

4. **Fixed-rate sampling, decoupled from frame rate.** Telemetry is sampled at a fixed target
   rate (72 Hz) via a time accumulator, and every sample stores its real elapsed time.
   Analysis never assumes evenly spaced rows.

5. **Documented coordinate system.** Unity is left-handed, Y up, meters. Floor plane is X by Z.
   Top-down path plots are X vs Z, never X vs Y. Documented in every session manifest.

6. **Validation before trust.** No analysis is trusted until the square test passes (walk a
   2x2 m square, confirm the logged path is a 2x2 m square). This gates Phase 1.

7. **Approximate line of sight, not eye tracking.** The Quest 2 has no eye tracking. Gaze is
   approximated with head-direction raycasts and described as approximate line of sight.

---

## 6. What the platform collects

**Raw telemetry (Unity, every sample):** timestamp and frame index; HMD position and rotation
(quaternion); left and right controller position and rotation. Everything else is derived from
this stream in Python.

**Events (Unity, Phase 2 onward):** timestamped discrete events such as sound played, light
changed, object appeared or moved, user entered a region, user reached a goal, task completed.
A separate stream, joined to telemetry on timestamp.

**Derived features (Python):** total path length, average and max speed, movement variability,
acceleration spikes, stop count and duration, rapid head-turn count, head rotation speed, head
jitter, facing time, scanning time, completion time, backtracking, revisited zones, junction
hesitation, reaction time after a stimulus, and approach versus avoidance after an event.

**Out of scope for v1:** eye tracking, hand tracking, heart rate, EEG, galvanic skin response,
facial expression, full-body tracking, audio capture. These are future hardware expansions.

---

## 7. Hardware: Quest 2 capabilities and limits

**Reliable:** head and controller movement, basic locomotion, approximate line of sight via
head direction, position and rotation tracking, interaction logging, audio stimuli, timing and
reaction logging.

**Not available:** true eye tracking, facial expression capture, medical-grade movement
analysis, physiological signals, EEG, heart rate, skin conductance, full-body tracking without
external trackers.

The first version is designed around what the Quest 2 does well and makes no claims that depend
on sensors it lacks.

---

## 8. Environments and research phasing

Environments are not levels in a game. Each one is a measurement instrument designed to make a
specific kind of behavior measurable and comparable. They are sequenced so each builds on the
last with minimal wasted work.

### 8.1 Environment design principles

1. **Control the path so the variance equals the thing being studied.** The more the route is
   fixed, the more every change in the telemetry is reaction, hesitation, scanning, or stimulus
   response rather than route choice. Early environments are heavily path-controlled on purpose.
2. **Subtlety over content.** Unease comes from ambiguity and anticipation, not overt scares. A
   sourceless sound, a hallway slightly too long, an ajar door. Subtle conditions are cheaper to
   build and yield richer signal: a jump-scare gives one startle spike then habituation, while
   sustained low-grade unease gives continuous scanning, slowed approach, and freezing.
3. **Every treatment environment has a matched neutral control.** A treatment only means
   something against a structurally identical neutral condition. Controls are designed alongside
   treatments, never bolted on later.
4. **Prefer repeated trials over single runs.** Where possible, run many short trials per
   session. This multiplies data per participant and lets one person experience multiple
   conditions, controlling for individual differences.
5. **White box first.** A plain untextured environment with controlled stimuli is often the
   better instrument, because it removes confounds from art, narrative, and recognizable content.
6. **One geometric seed, branching reuse.** The corridor is the core geometry. Add junctions and
   it becomes the navigation instrument; add an unsettling layer and it becomes the affective
   instrument. Build the corridor well and most later environments are modifications, not rebuilds.

### 8.2 Stimulus taxonomy

Stimuli are the controllable inputs that provoke measurable behavior. Each is logged as a
timestamped event so any behavior can be aligned to the moment it was triggered.

- **Auditory:** spatial sounds from a known location, sounds from behind, sourceless ambient
  noise, sudden onsets. Good for orienting, reaction time, approach or avoidance. Cheap and strong
  on Quest 2.
- **Lighting:** flicker, dimming, color shifts, a light that changes when unobserved. Good for
  unease, scanning, vigilance, at almost no build cost.
- **Architectural:** a hallway slightly too long, a layout that subtly changes, an ajar door, a
  looping path. Good for expectation violation, reorientation, disorientation.
- **Object-based:** an object that appears, despawns, moves when unobserved, or is unsettling.
  Good for attention capture, approach-avoidance, curiosity versus threat.

### 8.3 The progression at a glance

| Environment | Phase | Primary goal | Key measures | Build cost | Ethics sensitivity |
|---|---|---|---|---|---|
| Empty Validation Room | 0 | Prove telemetry is correct | square-test spans, sample rate | Low | Low |
| Object-Approach Room | 1 | Cleanest baseline measures | approach distance, speed, dwell, facing time | Low | Low |
| Instrumented Corridor | 1 | Controlled stimulus-response | reaction time, head turn, slowdown | Low | Low to Med |
| Eerie Corridor | 2 | Affective and threat response | scanning, freezing, slowed approach, avoidance | Med | Med to High |
| Soft Maze | 2 | Navigation decisions, interpretable | junction hesitation, wrong turns, backtracking | Med | Low |
| Search-and-Find Room | 2 | Visual attention and search | head-sweep coverage, search efficiency, time-to-find | Med | Low |
| Full Maze | 3 | Wayfinding and spatial memory | route efficiency, revisits, completion time | Med to High | Low |
| Audio-Influence Environment | 3 | Sound localization and orienting | head turn, orientation latency | Med | Low to Med |
| Non-Euclidean Geometry | 3 | Disorientation and reorientation | reorientation time, hesitation, backtracking | High | Med |

### 8.4 Phase 0: Foundation

**Empty Validation Room.**
- Goal: prove the telemetry pipeline is correct before any research behavior is collected.
- Build: a flat floor with a few reference cubes and good tracking volume. No task.
- Measures: square-test spans, effective sample rate, tracking stability.
- Pros: trivial to build; gates every later environment behind known-good data; catches unit,
  coordinate, and sampling bugs immediately.
- Cons: produces no research data by design.
- Takeaway: nothing downstream is trustworthy until this passes. The cheapest and most important step.

### 8.5 Phase 1: Controlled instruments

**Object-Approach Room.**
- Goal: the cleanest possible dependent measures and a movement baseline toward a single target.
- Build: one white room, one object opposite the user, walk over and look at it, run as repeated
  trials (appear, approach, despawn, repeat).
- Measures: closest approach distance, approach speed and slowdown point, dwell, facing time.
- Control: within-subject variation of the object (neutral vs unsettling, static vs moving,
  smooth vs sudden appearance).
- Pros: trivial to build; crisp measures; high data density from repeated trials; within-subject
  design controls for individual differences cheaply.
- Cons: low ecological validity; narrow behavior, no navigation; can feel clinical or repetitive.
- Takeaway: proves the instrument before complexity is added. Clean measures here mean the harder
  environments rest on solid ground.

**Instrumented Corridor.**
- Goal: controlled stimulus-response under a fixed path, with stimuli at identical spatial points
  for every participant so reactions are comparable across people.
- Build: a straight or L-shaped white hallway with one goal, reach the far door. Stimuli fire at
  fixed positions (sound from behind at 5 m, light flicker, object in an alcove).
- Measures: reaction time per stimulus, head turn toward or away, speed change after each event,
  gaze-proxy dwell on the alcove.
- Control: the neutral corridor is the baseline for the eerie corridor in Phase 2.
- Pros: path is controlled, so variance is reaction not route; fixed stimulus positions give clean
  cross-subject comparability; very cheap; the seed geometry for nearly everything later.
- Cons: low ecological validity; limited decision behavior; a determined participant can rush it.
- Takeaway: the core stimulus-response rig and the seed geometry. Build it well and most of Phase 2
  is a modification of it.

### 8.6 Phase 2: Branching the corridor

**Eerie Corridor (affective layer).**
- Goal: measure affective and threat response under a controlled path, so any difference is
  attributable to the unsettling layer rather than route choice.
- Build: the neutral corridor plus an unsettling layer (sourceless audio, dim flickering light, an
  ajar door, an alcove object, optional subtle length distortion). Subtlety is the design target.
- Measures: scanning rate, freezing or pausing, slowed approach, avoidance of the alcove or door,
  movement irregularity.
- Control: the structurally identical neutral corridor, run counterbalanced.
- Pros: builds directly on the corridor; sustained subtle unease yields rich continuous signal;
  strong fit for the project's interest in threat perception and scanning.
- Cons: real ethics sensitivity (distress kept mild and stoppable); motion-sickness risk if
  distortion is overdone; keeping unease subtle is genuinely hard; baseline anxiety is a confound.
- Takeaway: the primary affective-response instrument, with the neutral corridor as its control.
  The neutral-versus-eerie comparison is the first real research question.

**Soft Maze (junction decisions).**
- Goal: introduce navigation and decision behavior while keeping it interpretable, using two or
  three decision points rather than a full labyrinth.
- Build: the corridor extended with a handful of junctions and one exit.
- Measures: junction hesitation, wrong turns, backtracking, revisited zones, time to exit.
- Control: matched soft-maze layouts, counterbalanced order, to control for learning.
- Pros: still interpretable; bridges toward full navigation; reuses corridor geometry.
- Cons: path variance returns, lowering cross-subject comparability; learning effects appear and
  must be counterbalanced; layouts must be carefully matched.
- Takeaway: the navigation and decision instrument, kept small enough to interpret. The controlled
  stepping stone to the full maze.

**Search-and-Find Room.**
- Goal: isolate visual attention and search behavior.
- Build: a room with several objects, task is to find a target among distractors.
- Measures: head-sweep coverage, search efficiency, time-to-find, distractors fixated (head proxy).
- Control: vary target salience and distractor count; optionally combine with the eerie layer.
- Pros: isolates attention cleanly; head-sweep telemetry is rich; simple to run.
- Cons: head direction is only a proxy for gaze (no eye tracking on Quest 2); layout must be
  balanced; more setup than the corridor.
- Takeaway: the attention instrument, pairing naturally with the eerie layer to test whether unease
  changes how people search.

### 8.7 Phase 3: Specialized environments

Deferred until the platform and analysis pipeline are mature. Each trades build cost or
interpretability for depth in one direction.

**Full Maze.**
- Goal: deep wayfinding and spatial-memory behavior.
- Build: a full maze with one exit requiring real route planning and memory.
- Measures: route efficiency, revisited cells, completion time, distance-to-goal over time.
- Pros: rich navigation data; a classic validated paradigm; relevant to later neurodegenerative
  navigation research.
- Cons: navigation skill dominates the variance; uncontrolled paths hurt comparability; higher
  build cost and stronger learning effects.
- Takeaway: a Phase 3 specialization for when wayfinding is the question, not a starting instrument.

**Audio-Influence Environment.**
- Goal: isolate sound localization and orienting response.
- Build: a controlled space where spatial sounds play from varying known locations.
- Measures: head turn toward or away, orientation latency, approach or avoidance of the source.
- Pros: isolates auditory-spatial behavior; Quest 2 spatial audio is capable; clean cause-effect.
- Cons: needs careful audio spatialization; VR room acoustics are simplified; narrow behavior.
- Takeaway: a focused auditory-response instrument; earlier audio cues graduate into their own study.

**Non-Euclidean Geometry.**
- Goal: strong spatial disorientation through deliberate violation of spatial expectation.
- Build: impossibly looping hallways, rooms connected in non-physical ways, via portal rendering or
  teleport tricks.
- Measures: reorientation time, hesitation spikes, backtracking, decision delay, broken spatial
  memory.
- Pros: strong novel disorientation behavior; distinctive to demonstrate; relevant to spatial memory.
- Cons: technically expensive; high motion-sickness risk; hard to interpret cleanly; easy to
  over-engineer.
- Takeaway: a deferred showcase environment, worth building only once platform and analysis are solid.

### 8.8 Environment to construct mapping

The bridge from raw telemetry to research meaning, and the part a supervisor will want to see.

| Environment | Behavioral features elicited | Cognitive / neuroscience construct |
|---|---|---|
| Object-Approach Room | approach distance, speed profile, dwell, facing time | approach-avoidance, valence and arousal proxy, attention |
| Instrumented Corridor | reaction time, head turn, post-event slowdown | stimulus orienting, attention capture |
| Eerie Corridor | scanning rate, freezing, slowed approach, avoidance | threat perception, vigilance, anxiety-like behavior |
| Soft Maze | junction hesitation, wrong turns, backtracking | spatial decision-making, uncertainty, cognitive load |
| Search-and-Find Room | head-sweep coverage, search efficiency, time-to-find | visual search, selective attention |
| Full Maze | route efficiency, revisits, completion time | wayfinding, spatial memory, cognitive mapping |
| Audio-Influence Environment | head turn toward or away, orientation latency | auditory spatial attention, sound localization |
| Non-Euclidean Geometry | reorientation time, hesitation spikes, backtracking | spatial reorientation, expectation violation, disorientation |

All features are derived in Python from the same raw pose and event streams. No new telemetry type
is needed for any environment in this roadmap: the data layer is fixed and the environments vary on
top of it.

### 8.9 Recommended build order and what to build first

Build order: Empty Validation Room (pass the square test), then Object-Approach Room, then
Instrumented Corridor, then Eerie Corridor (first real comparison), then Soft Maze and
Search-and-Find, then the Phase 3 specializations in rising order of cost and risk.

What to build first: the **Instrumented Corridor as the core**, with the **Object-Approach Room**
as the warm-up that validates measurement on the way there. The corridor is the seed both other
directions grow from: add junctions for navigation, add the unsettling layer for affect. One
buildable core, two research directions, no wasted work. Because this project leans toward threat
perception, scanning, and affective response more than pure wayfinding, the path-controlled corridor
serves the real interest better than a maze, and the maze still gets its place in Phase 3.

### 8.10 Open design questions per phase

Resolved per phase, just before that phase is built, so decisions use the most current information.

- Corridor length and stimulus spacing (sets comparability of reaction-time measures).
- Locomotion speed cap (prevent rushing, bound motion sickness).
- Eerie calibration: the minimum unsettling layer that produces a measurable difference without
  crossing into distress. Needs a small informal test, not a guess.
- Trial count and session length before fatigue degrades data.
- Counterbalancing scheme for neutral vs eerie and for matched maze layouts, fixed before any pilot.
- Consent and in-headset stop mechanism, wired before any non-self participant is recorded.

---

## 9. System architecture

**Unity side.** Session Manager (anonymous IDs, manifest, start and stop, export); Telemetry Logger
(fixed-rate raw pose sampling); Event Logger (Phase 2, timestamped events); Stimulus Manager (Phase
2, audio, lighting, object changes, writing into the event stream); Environment Manager (owns the
current scene, lets new environments be added without rewriting the system).

**Python side.** Data Loader (manifest plus telemetry and later events); Feature Extractor (all
behavioral features from raw telemetry); Visualization Tools (path plots, heatmaps, pause maps,
head-rotation graphs, event timelines, cross-session comparisons); ML Pipeline (later, classification,
clustering, anomaly flagging once enough sessions exist).

---

## 10. Data schema (v0.1.0)

**manifest.json** (one per session): schema_version, session_id, participant_id, environment_id,
condition, trial_number, start_utc, end_utc, duration_sec, sample_rate_hz, sample_count,
coordinate_system, rotation_format, telemetry_file.

**telemetry.csv** (one row per sample):
```
t_sec, frame,
hmd_px, hmd_py, hmd_pz, hmd_rx, hmd_ry, hmd_rz, hmd_rw,
lc_px,  lc_py,  lc_pz,  lc_rx,  lc_ry,  lc_rz,  lc_rw,
rc_px,  rc_py,  rc_pz,  rc_rx,  rc_ry,  rc_rz,  rc_rw
```
Positions in meters, Unity world space. Rotations are quaternions (x,y,z,w). Missing transforms write
NaN. `t_sec` is seconds since session start and is not evenly spaced.

**events.csv** (Phase 2, one row per event): t_sec, event_type, plus event-specific fields.

The `schema_version` field exists so the format can evolve without breaking older sessions.

---

## 11. Minimum viable platform — Phase 1 success criteria

Phase 1 is a real research platform, even if basic, when a person can launch a VR environment, start
a session, have their head and controller telemetry recorded, finish the session, produce structured
data files, load them in Python, and generate basic visualizations of their path and behavior. The
system must also allow new environments to be added without rewriting the core.

The hard gate is the **square test**: after walking a 2x2 m square, the logged X and Z spans each read
about 2.0 m and the top-down plot looks like a square.

---

## 12. Methodology notes

These protect the project from drawing conclusions the data cannot support.

- **The first study is a feasibility pilot, not a powered experiment.** With a handful of
  participants there is no statistical power. The goal is to prove the pipeline end to end and surface
  design problems, not to confirm a hypothesis.
- **Control the learning-effect confound.** Running the same maze neutral-then-eerie means the eerie
  run benefits from familiarity, which masquerades as an environment effect. Fix with two matched
  layouts and counterbalanced order, or a between-subjects design.
- **Descriptive before predictive.** Early analysis is visualization and descriptive statistics.
  Classifiers come only once the dataset is large enough that they cannot simply memorize the sessions.

---

## 13. Machine learning direction

ML is not built first. It comes after the platform reliably collects clean data and after enough
sessions exist to make modeling meaningful. The first ML target is simple and safe: distinguish
neutral-environment sessions from eerie-environment sessions using only behavioral telemetry. This is
an environment-response classifier, easier and safer than any mental-health classifier.

Early candidate methods: logistic regression, random forest, SVM, k-means, DBSCAN, isolation forest.
Deep learning is avoided until the dataset is large enough to justify it. Later tasks: clustering
navigation strategies (exploratory, cautious, direct, avoidant, confused), detecting high-reactivity
sessions, predicting completion time from early movement, and flagging unusual patterns.

---

## 14. Research evolution (long-term stages)

These are research maturity stages, distinct from the environment build phases in Section 8.

1. Platform building: Unity, locomotion, raw logging, file export, basic visualization.
2. Behavioral experimentation: how users react to different environments and cues.
3. Cognitive and neuroscience framing: connect behaviors to spatial cognition, attention, cognitive
   load, threat perception, navigation strategy.
4. ML analysis: classify, cluster, and flag behavior at scale.
5. Mental-health or neurological direction: explored only with formal supervision, ethical review,
   and clinical grounding.

---

## 15. Roadmap (summer 2026)

Realistically about eight weeks remain this summer, so the roadmap is scoped to fit.

- **Weeks 1 to 2, VR foundation:** Unity and Quest 2 setup, XR rig with continuous locomotion, empty
  room, telemetry logger, pass the square test.
- **Weeks 3 to 5, telemetry platform:** session and event logging, stimulus manager, the
  Object-Approach Room and Instrumented Corridor, structured export, a repeatable end-to-end session.
- **Weeks 6 to 8, analysis and pilot:** Python feature extraction and visualization, a 3 to 5 person
  feasibility pilot, cross-session comparison, and a written summary of the system and early findings.

The Eerie Corridor comparison and any ML are stretch goals beyond this window.

---

## 16. First prototype experiment

**Name:** Eerie Corridor Response Test.
**Environment:** the Instrumented Corridor in two matched conditions, neutral and eerie, each with
fixed stimuli at fixed marks and one clear exit.
**Task:** reach the far door.
**Design:** within-subjects with counterbalanced order, or between-subjects, to control for learning.
**Data:** path, head direction, controller movement, pauses, reaction to stimuli, completion time,
hesitation points.
**Question:** how does an eerie environment change behavior versus a neutral one?
**Candidate metrics:** longer completion time, more pauses, more head turns, more scanning, delayed
response after audio cues, increased movement irregularity, avoidance of the alcove.

---

## 17. Ethics and data handling

- Obtain informed consent from every participant.
- Use anonymous participant IDs. Do not collect names unless necessary.
- Do not collect mental-health history in the first phase.
- Do not intentionally induce extreme fear or distress.
- Let participants stop a session at any time.
- Store data securely and make no clinical claims.
- If university resources or participants are involved, check whether IRB approval is required before
  recruiting.
- Apply extra care if the project later involves mental health, disease, minors, or other vulnerable
  populations.

---

## 18. Research boundaries

The project does not claim to diagnose anxiety, PTSD, dementia, Alzheimer's, Parkinson's,
schizophrenia, depression, or any mental or neurological condition.

It may study behavioral response, navigation behavior, attention patterns, reaction to stimuli,
cognitive-load proxies, spatial disorientation, avoidance, environmental scanning, and movement
hesitation.

Preferred framing: this platform studies behavioral telemetry in immersive VR environments and
explores how those behaviors may relate to cognitive and affective responses.

---

## 19. Future expansion

**Hardware:** eye tracking (Quest Pro, Vive Pro Eye, Varjo), physiological sensors (heart rate, GSR,
Empatica-style wearables), EEG (Muse, OpenBCI, Emotiv), full-body tracking (Vive trackers, SlimeVR,
motion capture).

**Research questions for later:** predictable versus unpredictable spaces, whether eerie environments
increase scanning, whether audio cues produce measurable orientation responses, estimating cognitive
load from behavior, clustering users into strategy types, and adaptive environments that respond to
detected uncertainty or stress-like behavior.

**Long-term neuroscience connections:** spatial cognition (navigation, wayfinding, disorientation),
attention (scanning, response to stimuli), threat perception (freezing, avoidance, orientation toward
sounds), cognitive load (hesitation, inefficient paths), and possible relevance to spatial-navigation
difficulty in neurodegenerative research. These remain future directions pending clinical supervision
and formal approval.

---

## 20. Project philosophy

The strongest version of this project is a modular VR research platform for collecting and analyzing
human behavioral telemetry in controlled immersive environments. The eerie environment matters
because it creates measurable behavior. The corridor matters because it controls the path so the
behavior is interpretable. The maze matters because it creates a spatial-cognition task. The ML
matters because it finds patterns in the behavior. The neuroscience matters because it gives those
patterns meaning. The platform is the foundation that makes all of it possible.
