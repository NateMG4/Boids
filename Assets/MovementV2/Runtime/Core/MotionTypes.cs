using UnityEngine;

namespace MovementV2.Core
{
    [System.Serializable]
    public struct Pose2
    {
        public Vector2 p_w;
        public float yaw;

        public Pose2(Vector2 positionWorld, float yawRadians)
        {
            p_w = positionWorld;
            yaw = yawRadians;
        }
    }

    [System.Serializable]
    public struct Twist2
    {
        public Vector2 v_w;
        public float w;

        public Twist2(Vector2 velocityWorld, float yawRate)
        {
            v_w = velocityWorld;
            w = yawRate;
        }
    }

    [System.Serializable]
    public struct BodyTwist2
    {
        public float v_fwd;
        public float v_lat;
        public float w;

        public BodyTwist2(float forwardSpeed, float lateralSpeed, float yawRate)
        {
            v_fwd = forwardSpeed;
            v_lat = lateralSpeed;
            w = yawRate;
        }
    }

    [System.Serializable]
    public struct MotionState
    {
        public Pose2 pose;
        public Twist2 twist;
        public BodyTwist2 bodyTwist;

        public MotionState(Pose2 poseIn, Twist2 twistIn, BodyTwist2 bodyTwistIn)
        {
            pose = poseIn;
            twist = twistIn;
            bodyTwist = bodyTwistIn;
        }
    }

    [System.Serializable]
    public struct DesiredMotion
    {
        public Vector2 desiredVelocityWorld;
        public float desiredYawRate;
        public Vector2 desiredThrustDirectionWorld;

        public DesiredMotion(Vector2 velocityWorld, float yawRate)
        {
            desiredVelocityWorld = velocityWorld;
            desiredYawRate = yawRate;
            desiredThrustDirectionWorld = Vector2.zero;
        }

        public DesiredMotion(Vector2 velocityWorld, float yawRate, Vector2 thrustDirectionWorld)
        {
            desiredVelocityWorld = velocityWorld;
            desiredYawRate = yawRate;
            desiredThrustDirectionWorld = thrustDirectionWorld;
        }

        public static DesiredMotion Stop => new DesiredMotion(Vector2.zero, 0f);
    }

    [System.Serializable]
    public struct ActuatorCommand
    {
        public float thrust;
        public float torque;

        public ActuatorCommand(float thrustIn, float torqueIn)
        {
            thrust = thrustIn;
            torque = torqueIn;
        }

        public static ActuatorCommand Zero => new ActuatorCommand(0f, 0f);
    }
}
