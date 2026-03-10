using UnityEngine;

namespace MovementV2.Core
{
    public static class MotionMath
    {
        public static float WrapPi(float radians)
        {
            return Mathf.Repeat(radians + Mathf.PI, Mathf.PI * 2f) - Mathf.PI;
        }

        public static float YawFromTransformUp(Transform t)
        {
            Vector2 up = t.up;
            return Mathf.Atan2(up.y, up.x) - Mathf.PI * 0.5f;
        }

        public static Vector2 ForwardFromYaw(float yaw)
        {
            float theta = yaw + Mathf.PI * 0.5f;
            return new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
        }

        public static Vector2 LeftFromYaw(float yaw)
        {
            Vector2 fwd = ForwardFromYaw(yaw);
            return new Vector2(-fwd.y, fwd.x);
        }

        public static BodyTwist2 ToBodyTwist(Vector2 velocityWorld, float yawRate, float yaw)
        {
            Vector2 fwd = ForwardFromYaw(yaw);
            Vector2 left = LeftFromYaw(yaw);
            float vFwd = Vector2.Dot(velocityWorld, fwd);
            float vLat = Vector2.Dot(velocityWorld, left);
            return new BodyTwist2(vFwd, vLat, yawRate);
        }
    }
}
