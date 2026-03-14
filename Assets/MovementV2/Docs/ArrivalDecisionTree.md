# MovementV2 Arrival Decision Tree

This document walks through the current control flow in plain language, from clicking in the scene to arriving at the target point.

## High-Level Idea

The current MovementV2 path-following stack separates two ideas:

- `desiredVelocityWorld`: the world-space velocity the boid should end up having
- `desiredThrustDirectionWorld`: the world-space direction the boid should point so forward thrust helps correct its motion

That separation matters because the boid does not always want to face the same direction it wants to travel. Near arrival, it may still want a small net targetward velocity while needing to point somewhere else to apply a corrective burn.

## Step 1: Mouse Click Creates A Target

In `ClickToMoveDesiredMotion`, left mouse click is converted from screen space to world space and stored as `targetWorld`.

From then on, each frame:

1. Read current world position.
2. Compute `delta = targetWorld - currentPosition`.
3. Compute `dist = |delta|`.
4. Build a `DesiredMotion` for the controller.

If there is no target, the script sends `DesiredMotion.Stop`.

## Step 2: Decide Whether We Are Already Close Enough To Stop

Before building any motion command, `ComputeDesiredMotion` checks:

- Is the boid inside `stopRadius`?
- Is its current speed below `stopSpeed`?

If both are true, the system returns `DesiredMotion.Stop`.

This is the "we are close enough and slow enough, stop driving" condition.

## Step 3: Compute The Target Direction

If the boid is not yet stopped:

1. Compute `rHat`, the unit vector from the boid to the target.
2. Use `rHat` as the primary notion of "toward the clicked point."

This direction is the base for the travel goal.

## Step 4: Compute The Desired World Velocity

`ComputeDesiredVelocity` builds a continuous desired velocity state.

### 4.1 Compute Ideal Approach Speed

The script computes `approachSpeed` from the remaining distance and the configured max thrust.

In plain language:

- Far from the target: high allowed speed.
- Near the target: lower allowed speed.

This is meant to answer:

"If the boid were behaving ideally, how fast should it be moving toward the target right now?"

### 4.2 Split Current Velocity Into Radial And Lateral Parts

Current velocity is decomposed into:

- `radialSpeed`: the component toward the target
- `lateralVelocity`: the sideways drift relative to the target line

This tells the system:

- how fast the boid is closing
- how much it is sliding sideways

### 4.3 Estimate When Arrival Correction Should Matter

The script computes `brakeStartDistance` from:

- a base padding term
- current speed braking distance
- closing distance
- turn distance

This is not a discrete brake trigger anymore. It is used to decide how strongly arrival correction should start to influence the command.

### 4.4 Compute Arrival Correction Blend

The script computes `correctionBlend`.

This blend increases when:

- the boid gets inside the brake zone
- or the current closing speed is too high for the current `approachSpeed`

Interpretation:

- `correctionBlend` near `0`: mostly cruise
- `correctionBlend` near `1`: arrival correction should strongly influence the command

### 4.5 Build The Desired Velocity State

The final desired world velocity is built from three ideas:

1. `cruiseVelocity = rHat * approachSpeed`
2. subtract lateral drift correction
3. subtract some of the current radial motion based on `correctionBlend`

In plain language:

- aim toward the target
- cancel sideways slip
- near arrival, progressively bleed off targetward momentum

This is still a desired state, not just an error vector.

## Step 5: Compute The Desired Thrust Direction

After the desired world velocity is built, the script computes a separate thrust direction.

The current sender logic uses:

- `velocityCorrection = desiredVelocity - currentVelocity`

Then it chooses:

1. If `velocityCorrection` is meaningful, use that as the thrust direction.
2. Else if `desiredVelocity` is meaningful, use that.
3. Else fall back to `rHat`.

In plain language:

- `desiredVelocityWorld` says where we want the velocity state to end up.
- `desiredThrustDirectionWorld` says where the nose should point right now so forward thrust reduces the velocity error.

This is the critical separation in the current design.

## Step 6: Package The Command

`ClickToMoveDesiredMotion` sends a `DesiredMotion` containing:

- desired world velocity
- desired yaw rate
- desired thrust direction

So the motion command now carries both:

- the travel goal
- the burn direction

## Step 7: MotionControllerRunner Passes The Command To The Controller

In `FixedUpdate`, `MotionControllerRunner`:

1. Reads the current `MotionState` from the rigidbody adapter.
2. Calls `CascadedPidMotionController.Compute(state, Desired, dt, mass)`.
3. Applies the resulting thrust and torque to the body.

Debug rays show:

- yellow: current velocity
- cyan: desired velocity
- red: desired thrust direction

## Step 8: Controller Resolves The Burn Direction

Inside `CascadedPidMotionController`, the controller computes:

- `desiredVelocityWorld`
- `velocityErrorWorld = desiredVelocityWorld - currentVelocityWorld`

Then it resolves the thrust direction with this precedence:

1. If `desired.desiredThrustDirectionWorld` is provided, use that.
2. Else if `velocityErrorWorld` exists, use that.
3. Else if `desiredVelocityWorld` exists, use that.
4. Else use zero.

Meaning:

- explicit thrust direction is highest quality
- velocity error is the default correction axis
- desired velocity is the last useful fallback

## Step 9: Controller Rotates Toward The Thrust Direction

The controller converts the resolved thrust direction into a desired yaw.

Then:

1. heading PID computes a desired yaw rate
2. yaw-rate PID computes torque

This means the boid now rotates toward the burn direction, not always toward the travel direction.

## Step 10: Controller Computes How Much Thrust Is Helpful

The controller computes:

- `velocityErrorWorld = desiredVelocityWorld - currentVelocityWorld`
- `speedError = dot(velocityErrorWorld, thrustDirection)`

That scalar answers:

"How much velocity correction is still needed along the direction we intend to burn?"

Interpretation:

- positive value: more thrust in this direction helps
- zero or negative value: thrust in this direction does not help

Then the speed PID converts that scalar error into acceleration, and then into thrust.

This is different from the older model, which only looked at forward speed along the boid's current nose.

## Step 11: Controller Gates Thrust By Alignment

Even if thrust is useful, the controller still gates it based on alignment.

It combines:

- a heading-based gate
- an alignment gate from `dot(forward, thrustDirection)`

This means:

- if the boid is pointed usefully toward the burn direction, thrust can start
- if it is pointed the wrong way, thrust is suppressed

So the boid:

1. rotates toward the burn direction
2. starts thrusting when doing so will actually help the correction

## Step 12: Physics Updates The Boid

Finally the rigidbody adapter:

- applies force along `transform.up`
- applies torque around the rigidbody

Unity physics then updates:

- position
- linear velocity
- angular velocity

Next fixed frame, the whole loop runs again.

## Summary

The current decision tree is:

1. Click creates a world target.
2. If close enough and slow enough, stop.
3. Build a desired world velocity toward the target with arrival shaping.
4. Build a separate desired thrust direction from the velocity correction.
5. Send both to the controller.
6. Controller rotates toward the thrust direction.
7. Controller computes thrust from velocity error projected onto that thrust direction.
8. Controller applies thrust only when alignment makes that burn useful.
9. Repeat until the boid reaches the stop condition.

## Practical Interpretation

The clean mental model is:

- `desiredVelocityWorld` = "What velocity should I have?"
- `desiredThrustDirectionWorld` = "Where should I point to apply the burn that moves me toward that velocity?"

That is the reason the current system can support arrival behaviors where travel direction and burn direction are not the same.
