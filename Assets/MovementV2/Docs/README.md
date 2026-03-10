# MovementV2

MovementV2 is an additive robotics-style movement control module for Unity 2D.

## What it contains
- SE(2)-style state types (`Pose2`, `Twist2`, `BodyTwist2`, `MotionState`)
- Cascaded PID controller (heading -> yaw rate -> torque, speed -> thrust)
- Forward-thrust/yaw-torque actuator model
- Rigidbody2D adapter
- Minimal click-to-move demo driver

## Add to a boid GameObject
1. Add `Rigidbody2D`.
2. Add `Rigidbody2DMotionAdapter`.
3. Add `MotionControllerRunner`.
4. Add `ClickToMoveDesiredMotion` for demo input.

## Notes
- This module does not modify existing legacy scripts.
- Control runs in `FixedUpdate`.
- Forward direction assumes `transform.up`.
