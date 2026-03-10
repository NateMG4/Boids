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
            float desiredSpeed = desiredVelocityWorld.magnitude;

            float desiredYaw = state.pose.yaw;
            if (desiredSpeed > config.desiredSpeedEpsilon)
            {
                desiredYaw = Mathf.Atan2(desiredVelocityWorld.y, desiredVelocityWorld.x) - Mathf.PI * 0.5f;
            }

            float headingError = MotionMath.WrapPi(desiredYaw - state.pose.yaw);
            float yawRateTarget = headingController.Step(headingError, dt);
            yawRateTarget += desired.desiredYawRate;
            yawRateTarget = Mathf.Clamp(yawRateTarget, -config.limits.maxYawRate, config.limits.maxYawRate);

            float yawRateError = yawRateTarget - state.bodyTwist.w;
            float torqueCmd = yawRateController.Step(yawRateError, dt);
            torqueCmd = Mathf.Clamp(torqueCmd, -config.limits.maxTorque, config.limits.maxTorque);

            Vector2 forward = MotionMath.ForwardFromYaw(state.pose.yaw);
            float forwardTarget = Vector2.Dot(desiredVelocityWorld, forward);
            forwardTarget = Mathf.Clamp(forwardTarget, 0f, config.limits.maxSpeed);

            float speedError = forwardTarget - state.bodyTwist.v_fwd;
            float accelCmd = speedController.Step(speedError, dt);
            float thrustCmd = Mathf.Clamp(mass * accelCmd, 0f, config.limits.maxThrust);

            float gate = HeadingGate(headingError, config.headingThrustGateRad);
            thrustCmd *= gate;

            return new ActuatorCommand(thrustCmd, torqueCmd);
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
