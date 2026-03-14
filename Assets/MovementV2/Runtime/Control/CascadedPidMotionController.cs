using MovementV2.Core;
using UnityEngine;

namespace MovementV2.Control
{
    [System.Serializable]
    public class CascadedPidMotionController : IMotionController
    {
        [SerializeField] private CascadedControlConfig config;
        [SerializeField] private PidController headingController;
        [SerializeField] private PidController yawRateController;
        [SerializeField] private PidController speedController;

        public CascadedPidMotionController(CascadedControlConfig configIn)
        {
            config = configIn;
            headingController = new PidController(config.headingGains);
            yawRateController = new PidController(config.yawRateGains);
            speedController = new PidController(config.speedGains);
        }

        public CascadedControlConfig Config
        {
            get => config;
            set
            {
                config = value;
                headingController.Gains = value.headingGains;
                yawRateController.Gains = value.yawRateGains;
                speedController.Gains = value.speedGains;
            }
        }

        public void Reset()
        {
            headingController.Reset();
            yawRateController.Reset();
            speedController.Reset();
        }

        public ActuatorCommand Compute(in MotionState state, in DesiredMotion desired, float dt, float mass)
        {
            if (dt <= 0f)
            {
                return ActuatorCommand.Zero;
            }

            Vector2 desiredVelocityWorld = desired.desiredVelocityWorld;
            Vector2 velocityErrorWorld = desiredVelocityWorld - state.twist.v_w;
            Vector2 thrustDirection = ResolveThrustDirection(desired, desiredVelocityWorld, velocityErrorWorld);

            float desiredYaw = state.pose.yaw;
            if (thrustDirection.sqrMagnitude > config.desiredSpeedEpsilon * config.desiredSpeedEpsilon)
            {
                desiredYaw = Mathf.Atan2(thrustDirection.y, thrustDirection.x) - Mathf.PI * 0.5f;
            }

            float headingError = MotionMath.WrapPi(desiredYaw - state.pose.yaw);
            float yawRateTarget = headingController.Step(headingError, dt);
            yawRateTarget += desired.desiredYawRate;
            yawRateTarget = Mathf.Clamp(yawRateTarget, -config.limits.maxYawRate, config.limits.maxYawRate);

            float yawRateError = yawRateTarget - state.bodyTwist.w;
            float torqueCmd = yawRateController.Step(yawRateError, dt);
            torqueCmd = Mathf.Clamp(torqueCmd, -config.limits.maxTorque, config.limits.maxTorque);

            Vector2 forward = MotionMath.ForwardFromYaw(state.pose.yaw);
            float speedError = thrustDirection.sqrMagnitude > config.desiredSpeedEpsilon * config.desiredSpeedEpsilon
                ? Vector2.Dot(velocityErrorWorld, thrustDirection)
                : 0f;
            float accelCmd = speedController.Step(speedError, dt);
            float thrustCmd = Mathf.Clamp(mass * accelCmd, 0f, config.limits.maxThrust);

            float gate = thrustDirection.sqrMagnitude > config.desiredSpeedEpsilon * config.desiredSpeedEpsilon
                ? ComputeThrustGate(
                forward,
                thrustDirection,
                headingError,
                config.headingThrustGateRad)
                : 0f;
            thrustCmd *= gate;

            return new ActuatorCommand(thrustCmd, torqueCmd);
        }

        private static Vector2 ResolveThrustDirection(
            in DesiredMotion desired,
            Vector2 desiredVelocityWorld,
            Vector2 velocityErrorWorld)
        {
            if (desired.desiredThrustDirectionWorld.sqrMagnitude > 0f)
            {
                return desired.desiredThrustDirectionWorld.normalized;
            }

            if (velocityErrorWorld.sqrMagnitude > 0f)
            {
                return velocityErrorWorld.normalized;
            }

            if (desiredVelocityWorld.sqrMagnitude > 0f)
            {
                return desiredVelocityWorld.normalized;
            }

            return Vector2.zero;
        }

        private static float ComputeThrustGate(
            Vector2 forward,
            Vector2 thrustDirection,
            float headingError,
            float gateWidthRad)
        {
            float headingGate = HeadingGate(headingError, gateWidthRad);
            float alignmentGate = Mathf.Max(0f, Vector2.Dot(forward, thrustDirection));
            return Mathf.Max(headingGate, alignmentGate);
        }

        private static float HeadingGate(float headingError, float gateWidthRad)
        {
            if (gateWidthRad <= 0f)
            {
                return 1f;
            }

            float absErr = Mathf.Abs(headingError);
            if (absErr >= gateWidthRad)
            {
                return 0f;
            }

            float normalized = absErr / gateWidthRad;
            return 1f - normalized;
        }
    }
}
