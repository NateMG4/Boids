using MovementV2.Control;
using MovementV2.Core;
using UnityEngine;

namespace MovementV2.Demo
{
    [RequireComponent(typeof(MotionControllerRunner))]
    public class ClickToMoveDesiredMotion : MonoBehaviour
    {
        private const float DirectionEpsilon = 1e-6f;

        private enum ArrivalState
        {
            Idle,
            Cruise,
            Brake,
            Stop
        }

        [SerializeField] private float desiredSpeed = 6f;
        [SerializeField] private float stopRadius = 0.5f;
        [SerializeField] private bool drawTarget = true;
        [SerializeField] private bool drawStateIndicator = true;
        [SerializeField] [Min(0f)] private float stateIndicatorSize = 0.6f;

        [Header("Arrival")]
        [SerializeField] [Min(0f)] private float stopSpeed = 0.25f;
        [SerializeField] [Min(0f)] private float brakeDistancePadding = 0.25f;
        [SerializeField] [Min(0f)] private float brakeCommandSpeed = 0.2f;
        [SerializeField] [Min(1f)] private float turnTimeLeadFactor = 1.15f;
        [SerializeField] [Min(0f)] private float brakeExitSpeedMargin = 0.35f;
        [SerializeField] [Min(1f)] private float brakeExitDistanceFactor = 1.2f;

        [Header("Steering")]
        [SerializeField] [Min(0f)] private float lateralCorrectionGain = 1f;

        private MotionControllerRunner runner;
        private Rigidbody2D rb;
        private Camera cam;
        private Vector2? targetWorld;
        private bool isBrakingForArrival;
        [SerializeField] private ArrivalState currentArrivalState = ArrivalState.Idle;

        public string CurrentArrivalStateName => currentArrivalState.ToString();

        private void Awake()
        {
            runner = GetComponent<MotionControllerRunner>();
            rb = GetComponent<Rigidbody2D>();
            cam = Camera.main;
        }

        private void Update()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null)
                {
                    return;
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mouse = Input.mousePosition;
                Vector3 world = cam.ScreenToWorldPoint(mouse);
                targetWorld = new Vector2(world.x, world.y);
                isBrakingForArrival = false;
            }

            if (!targetWorld.HasValue)
            {
                runner.Desired = DesiredMotion.Stop;
                isBrakingForArrival = false;
                currentArrivalState = ArrivalState.Idle;
                return;
            }

            Vector2 current = transform.position;
            Vector2 delta = targetWorld.Value - current;
            float dist = delta.magnitude;
            runner.Desired = ComputeDesiredMotion(delta, dist);

            if (drawTarget)
            {
                Debug.DrawLine(current, targetWorld.Value, GetStateColor());
                Debug.DrawRay(targetWorld.Value, Vector2.one * 0.05f, Color.magenta);
            }

            if (drawStateIndicator)
            {
                DrawStateIndicator(current);
            }
        }

        private DesiredMotion ComputeDesiredMotion(Vector2 deltaToTarget, float distToTarget)
        {
            if (distToTarget <= stopRadius && (rb == null || rb.velocity.magnitude <= stopSpeed))
            {
                isBrakingForArrival = false;
                currentArrivalState = ArrivalState.Stop;
                return DesiredMotion.Stop;
            }

            Vector2 rHat = distToTarget > DirectionEpsilon ? (deltaToTarget / distToTarget) : Vector2.zero;
            Vector2 desiredVelocity = rb == null
                ? (rHat * desiredSpeed)
                : ComputeDesiredVelocity(distToTarget, rHat);

            return desiredVelocity.sqrMagnitude > DirectionEpsilon
                ? new DesiredMotion(desiredVelocity, 0f)
                : DesiredMotion.Stop;
        }

        private Vector2 ComputeDesiredVelocity(float distToTarget, Vector2 rHat)
        {
            Vector2 currentVelocity = rb.velocity;
            CascadedControlConfig config = runner.GetConfig();
            float minCommandSpeed = Mathf.Max(brakeCommandSpeed, config.desiredSpeedEpsilon + 0.01f);
            float approachSpeed = ComputeApproachSpeed(distToTarget, config);
            Vector2 targetVelocity = rHat * approachSpeed;

            if (distToTarget <= stopRadius)
            {
                isBrakingForArrival = true;
                currentArrivalState = ArrivalState.Brake;
                return BuildVelocityCommand(-currentVelocity, currentVelocity.magnitude, minCommandSpeed, Vector2.zero);
            }

            float radialSpeed = Vector2.Dot(currentVelocity, rHat);
            Vector2 lateralVelocity = currentVelocity - (radialSpeed * rHat);
            float brakeStartDistance = ComputeBrakeStartDistance(rHat, currentVelocity, targetVelocity, config);
            bool shouldBrake = UpdateBrakeState(
                distToTarget,
                brakeStartDistance,
                currentVelocity,
                targetVelocity,
                approachSpeed,
                lateralVelocity,
                config);

            Vector2 headingVector = shouldBrake
                ? (targetVelocity - currentVelocity)
                : (targetVelocity - (lateralVelocity * lateralCorrectionGain));

            float commandSpeed = shouldBrake
                ? Mathf.Max(headingVector.magnitude, minCommandSpeed)
                : approachSpeed;

            currentArrivalState = shouldBrake ? ArrivalState.Brake : ArrivalState.Cruise;

            float speedFloor = shouldBrake ? minCommandSpeed : 0f;
            return BuildVelocityCommand(headingVector, commandSpeed, speedFloor, rHat);
        }

        private float ComputeApproachSpeed(float distToTarget, CascadedControlConfig config)
        {
            float maxThrust = config.limits.maxThrust;
            if (maxThrust <= 0f)
            {
                return desiredSpeed;
            }

            float mass = rb != null ? Mathf.Max(0.0001f, rb.mass) : 1f;
            float maxAccel = maxThrust / mass;
            if (maxAccel <= 0f)
            {
                return desiredSpeed;
            }

            float effectiveDist = Mathf.Max(0f, distToTarget - stopRadius);
            return Mathf.Min(desiredSpeed, Mathf.Sqrt(2f * maxAccel * effectiveDist));
        }

        private float ComputeBrakeStartDistance(
            Vector2 rHat,
            Vector2 currentVelocity,
            Vector2 targetVelocity,
            CascadedControlConfig config)
        {
            float baseDistance = stopRadius + brakeDistancePadding;

            float maxThrust = config.limits.maxThrust;
            if (rb == null || maxThrust <= 0f)
            {
                return baseDistance;
            }

            float mass = Mathf.Max(0.0001f, rb.mass);
            float maxAccel = maxThrust / mass;
            if (maxAccel <= 0f)
            {
                return baseDistance;
            }

            Vector2 velocityError = currentVelocity - targetVelocity;
            float brakeSpeed = velocityError.magnitude;
            float brakeDistance = (brakeSpeed * brakeSpeed) / (2f * maxAccel);

            float closingSpeed = Mathf.Max(0f, Vector2.Dot(currentVelocity, rHat));
            float closingDistance = (closingSpeed * closingSpeed) / (2f * maxAccel);

            Vector2 brakeHeading = (targetVelocity - currentVelocity);
            float turnDistance = 0f;
            if (brakeHeading.sqrMagnitude > DirectionEpsilon)
            {
                float maxYawRate = config.limits.maxYawRate;
                if (maxYawRate > 0f)
                {
                    Vector2 forward = transform.up;
                    float angle = Mathf.Acos(Mathf.Clamp(Vector2.Dot(forward, brakeHeading.normalized), -1f, 1f));
                    float turnTime = angle / maxYawRate;
                    turnDistance = currentVelocity.magnitude * turnTime * turnTimeLeadFactor;
                }
            }

            return baseDistance + Mathf.Max(brakeDistance, closingDistance) + turnDistance;
        }

        private bool UpdateBrakeState(
            float distToTarget,
            float brakeStartDistance,
            Vector2 currentVelocity,
            Vector2 targetVelocity,
            float approachSpeed,
            Vector2 lateralVelocity,
            CascadedControlConfig config)
        {
            if (!isBrakingForArrival)
            {
                isBrakingForArrival = distToTarget <= brakeStartDistance;
                return isBrakingForArrival;
            }

            float radialError = Mathf.Abs(currentVelocity.magnitude - approachSpeed);
            float lateralSpeed = lateralVelocity.magnitude;
            float velocityError = (targetVelocity - currentVelocity).magnitude;
            float exitDistance = brakeStartDistance * Mathf.Max(1f, brakeExitDistanceFactor);
            float exitSpeedThreshold = Mathf.Max(config.desiredSpeedEpsilon, brakeExitSpeedMargin);

            bool canExitBrake = distToTarget > exitDistance
                && radialError <= exitSpeedThreshold
                && lateralSpeed <= exitSpeedThreshold
                && velocityError <= exitSpeedThreshold;

            if (canExitBrake)
            {
                isBrakingForArrival = false;
            }

            return isBrakingForArrival;
        }

        private Vector2 BuildVelocityCommand(
            Vector2 headingVector,
            float commandSpeed,
            float minCommandSpeed,
            Vector2 fallbackDirection)
        {
            Vector2 direction = headingVector;
            if (direction.sqrMagnitude <= DirectionEpsilon)
            {
                direction = fallbackDirection;
            }

            if (direction.sqrMagnitude <= DirectionEpsilon || commandSpeed <= DirectionEpsilon)
            {
                return Vector2.zero;
            }

            float clampedSpeed = Mathf.Clamp(commandSpeed, 0f, desiredSpeed);
            if (clampedSpeed > DirectionEpsilon && clampedSpeed < minCommandSpeed)
            {
                clampedSpeed = minCommandSpeed;
            }

            return direction.normalized * clampedSpeed;
        }

        private void DrawStateIndicator(Vector2 currentPosition)
        {
            Color stateColor = GetStateColor();
            Vector2 up = transform.up;
            Vector2 right = new Vector2(-up.y, up.x);

            Debug.DrawRay(currentPosition, up * stateIndicatorSize, stateColor);
            Debug.DrawRay(currentPosition, right * (stateIndicatorSize * 0.5f), stateColor);
            Debug.DrawRay(currentPosition, -right * (stateIndicatorSize * 0.5f), stateColor);
        }

        private Color GetStateColor()
        {
            switch (currentArrivalState)
            {
                case ArrivalState.Cruise:
                    return Color.cyan;
                case ArrivalState.Brake:
                    return Color.red;
                case ArrivalState.Stop:
                    return Color.yellow;
                default:
                    return Color.gray;
            }
        }
    }
}
