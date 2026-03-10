# Current Project State

Snapshot date: 2026-03-06

This document describes the current technical state of the Unity project, with emphasis on the script layout and the newer script model work in progress.

## Existing docs found

- `Assets/ProjectIdeas/Unity Project Document.md`
  - High-level concept / design document for the larger "Summer Civilization" idea.
  - Not a technical implementation doc for the current Unity prototype.
- `Assets/MovementV2/Docs/README.md`
  - Small technical note for the new `MovementV2` controller module.
  - Describes the module as additive and explicitly says it does not replace the legacy boid scripts yet.

There was no current "project state" or "script architecture" document in the repo, so this file fills that gap.

## Unity / project baseline

- Unity version: `2022.3.30f1`
- Build settings currently include only `Assets/Scenes/SampleScene.unity`
- Scenes present in the repo:
  - `Assets/Scenes/SampleScene.unity`
  - `Assets/Scenes/Main.unity`
  - `Assets/Scenes/Scene.unity`
  - `Assets/Scenes/V2 Test.unity`

## High-level script status

There are currently three different script paths in the repo:

1. Legacy prototype scripts in `Assets/Scripts`
2. A newer modular flock system in `Assets/BoidScripts`
3. A separate motion-control module in `Assets/MovementV2`

These systems are not fully unified yet.

## 1. Legacy prototype scripts (`Assets/Scripts`)

This folder still contains the scripts that appear to power the older prototype scenes.

### Main scripts

- `Boid.cs`
  - Main monolithic boid controller.
  - Handles target selection, turning, thrusting, stopping, screen wrapping, debug drawing, and multiple modes:
    - `Setpoint`
    - `Velocity`
    - `Flock`
  - Uses `Rigidbody2D` directly and computes thrust / torque internally.
  - Still looks like the most complete "single ship in zero-G" controller in the repo.

- `Flock.cs`
  - Legacy flock coordinator for `Boid`.
  - Finds all `Boid` instances, forces them into `BoidMode.Flock`, and assigns each one a target vector based on:
    - separation
    - alignment
    - cohesion
  - This is still tightly coupled to `Boid.cs`.

- `Boid_OLD.cs`
  - Older experimental boid controller.
  - Contains PID-like turning logic and debug-heavy prototype code.
  - Still referenced by `SampleScene`.

### Support / legacy scripts

- `BoidController.cs`
  - Older manager aimed at `Boid_OLD`.
  - Most of its update logic is commented out.
  - Reads like an experiment rather than part of the current path.

- `CameraController.cs`
  - Simple camera follow script.

- `LineController.cs`
  - Helper for rendering target/debug lines.

- `BoidDrive.cs`
  - Abstract base class for a drive model.
  - Not clearly integrated into a working scene path.

- `ThrustDrive.cs`
  - Partial implementation of `BoidDrive`.
  - Contains `NotImplementedException` methods, so it is not production-ready.

- `boids.cs`
  - Placeholder script retained to satisfy project references.
  - No behavior.

### Assessment of the legacy folder

- `Boid.cs` is still the most complete playable control script.
- The folder mixes active code, older experiments, and partial refactor work.
- It does not represent a clean final architecture yet.

## 2. New modular flock system (`Assets/BoidScripts`)

This looks like the newer script model for flocking behavior.

### Core pieces

- `Core/FlockManager.cs`
  - Spawns `FlockAgent` instances from a prefab.
  - Gathers nearby transforms each frame.
  - Calls a `FlockBehavior` ScriptableObject to calculate movement.
  - Scales and clamps the returned move vector before sending it to the agent.

- `Core/FlockAgent.cs`
  - Thin wrapper around per-agent stats and movement.
  - Requires:
    - `PhysicsBoidDrive`
    - `CircleCollider2D`

- `Core/BoidStats.cs`
  - New data model for movement and behavior tuning.
  - Includes:
    - max speed
    - max thrust
    - max torque
    - mass
    - vision radius
    - behavior weights

### Behavior layer

- `Behaviors/FlockBehavior.cs`
  - Abstract base class.

- `Behaviors/CompositeBehavior.cs`
  - Combines multiple `FlockBehavior` assets with weights.
  - Also multiplies by matching weights from `BoidStats`.

- `Behaviors/SteeredAlignmentBehavior.cs`
- `Behaviors/SteeredCohesionBehavior.cs`
- `Behaviors/SteeredSeparationBehavior.cs`
  - Standard flocking outputs as steering vectors.

### Movement layer

- `Drive/BoidDrive.cs` (`PhysicsBoidDrive` class)
  - Converts a desired velocity into torque + forward thrust on a `Rigidbody2D`.
  - Rotates toward desired motion, then thrusts when roughly aligned.
  - Caps speed to `agent.stats.maxSpeed`.

### Assessment of the new flock system

This is the clearest attempt at the "new script model":

- flock logic is separated from motion
- stats are grouped into a dedicated data structure
- behaviors are ScriptableObjects
- agents are prefab-driven

Current limitations / rough edges visible in code:

- `FlockManager` neighbor detection uses `Physics2D.OverlapCircleAll(...)` with the manager's `neighborRadius`, not `BoidStats.visionRadius`.
- Nearby object filtering is broad; any collider in radius can enter context, not just flock agents.
- `PhysicsBoidDrive` is simpler than `Boid.cs` and does not include the more advanced braking / setpoint handling logic.
- I did not find scene wiring that clearly proves this path has replaced the older one yet.

## 3. MovementV2 (`Assets/MovementV2`)

This is a separate, cleaner movement-control module.

### What it contains

- state types in `Runtime/Core`
- PID control and config in `Runtime/Control`
- Unity `Rigidbody2D` adapter in `Runtime/UnityAdapters`
- a demo runner in `Runtime/Demo`

### Main classes

- `CascadedPidMotionController`
  - Heading -> yaw rate -> torque
  - Forward speed -> thrust

- `Rigidbody2DMotionAdapter`
  - Reads pose / twist from `Rigidbody2D`
  - Applies thrust and torque

- `MotionControllerRunner`
  - Executes the controller in `FixedUpdate`

- `ClickToMoveDesiredMotion`
  - Demo input script for click-to-move testing

### Assessment of MovementV2

- This is the cleanest low-level movement code in the repo.
- It is documented as additive, not a replacement for legacy scripts.
- It currently looks like a reusable control module, not a finished boid system.
- I did not find any scene references to `MotionControllerRunner` or `ClickToMoveDesiredMotion`.
- `Assets/Scenes/V2 Test.unity` currently appears to contain only a camera and no movement test objects yet.

## Scene state

Based on scene references:

- `SampleScene`
  - Uses `Boid_OLD` and `BoidController`
  - This is also the only scene currently in build settings

- `Main`
  - Uses `Boid.cs`
  - Looks like a direct single-agent zero-G setpoint / target test scene

- `Scene`
  - Uses `Flock.cs` and `Boid.cs`
  - Looks like the older flocking path built on top of the monolithic `Boid` script

- `V2 Test`
  - Present but not in build settings
  - Untracked in git at the moment
  - Appears to be an empty scaffold scene so far

## Current conclusion

The repo is in a transition state, not a settled architecture.

If the goal is "what is the current playable prototype?", the answer is still mostly the older `Assets/Scripts` path:

- `Boid.cs` for ship motion
- `Flock.cs` for basic flocking
- `Boid_OLD.cs` still lingering in `SampleScene`

If the goal is "what is the newer architecture direction?", the answer is:

- `Assets/BoidScripts` for modular flock behavior
- `Assets/MovementV2` for cleaner motion control

Those newer systems exist, but they do not appear fully integrated into the active scenes/build path yet.

## Recommended next cleanup / validation pass

If you want to continue the refactor safely, the highest-value next steps are:

1. Decide which runtime path is the intended future:
   - `Boid.cs`
   - `BoidScripts + PhysicsBoidDrive`
   - `BoidScripts + MovementV2`
2. Make one scene the canonical test scene for that path.
3. Remove or clearly mark legacy-only scripts that should no longer be used.
4. Test neighbor filtering and scene wiring for the `BoidScripts` path.
5. If `MovementV2` is the intended drive layer, connect `FlockBehavior` output into `DesiredMotion` and verify it in `V2 Test`.

## Quick status summary

- Concept docs exist.
- A small MovementV2 README exists.
- No current implementation/state doc existed before this file.
- The old boid prototype is still the most obviously wired path.
- The newer modular flock system is present but not clearly the active scene path.
- MovementV2 is promising, but currently looks like a standalone control module / demo foundation.
