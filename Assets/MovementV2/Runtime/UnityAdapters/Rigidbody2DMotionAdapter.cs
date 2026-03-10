using MovementV2.Control;
using MovementV2.Core;
using UnityEngine;

namespace MovementV2.UnityAdapters
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Rigidbody2DMotionAdapter : MonoBehaviour, IRigidbody2DAdapter
    {
        [SerializeField] private Rigidbody2D rb;

        public Rigidbody2D Body => rb;

        private void Awake()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }
        }

        public MotionState ReadState()
        {
            float yaw = MotionMath.YawFromTransformUp(transform);
            Vector2 vWorld = rb.velocity;
            float w = rb.angularVelocity * Mathf.Deg2Rad;

            Pose2 pose = new Pose2(rb.position, yaw);
            Twist2 twist = new Twist2(vWorld, w);
            BodyTwist2 bodyTwist = MotionMath.ToBodyTwist(vWorld, w, yaw);

            return new MotionState(pose, twist, bodyTwist);
        }

        public void Apply(in ActuatorCommand command)
        {
            Vector2 forward = transform.up;
            rb.AddForce(forward * command.thrust, ForceMode2D.Force);
            rb.AddTorque(command.torque, ForceMode2D.Force);
        }

        public void ClampSpeed(float maxSpeed)
        {
            if (maxSpeed <= 0f)
            {
                return;
            }

            if (rb.velocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                rb.velocity = rb.velocity.normalized * maxSpeed;
            }
        }
    }
}
