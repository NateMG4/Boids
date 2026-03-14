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

        [Header("Steering")]
        [SerializeField] [Min(0f)] private float lateralCorrectionGain = 1f;
        [SerializeField] [Min(0f)] private float longitudinalCorrectionGain = 1f;

        private MotionControllerRunner runner;
        private Rigidbody2D rb;
        private Camera cam;
        private Vector2? targetWorld;
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
            }

            if (!targetWorld.HasValue)
            {
                runner.Desired = DesiredMotion.Stop;
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
                currentArrivalState = ArrivalState.Stop;
                return DesiredMotion.Stop;
            }

            Vector2 rHat = distToTarget > DirectionEpsilon ? (deltaToTarget / distToTarget) : Vector2.zero;
            Vector2 desiredVelocity = rb == null
                ? (rHat * desiredSpeed)
                : ComputeDesiredVelocity(distToTarget, rHat);
            Vector2 desiredThrustDirection = rb == null
                ? desiredVelocity
                : ComputeDesiredThrustDirection(desiredVelocity, rb.velocity, rHat);

            return desiredVelocity.sqrMagnitude > DirectionEpsilon
                ? new DesiredMotion(desiredVelocity, 0f, desiredThrustDirection)
                : DesiredMotion.Stop;
        }

        private Vector2 ComputeDesiredVelocity(float distToTarget, Vector2 rHat)
        {
            Vector2 currentVelocity = rb.velocity;
            CascadedControlConfig config = runner.GetConfig();
            float approachSpeed = ComputeApproachSpeed(distToTarget, config);
            float radialSpeed = Vector2.Dot(currentVelocity, rHat);
            Vector2 lateralVelocity = currentVelocity - (radialSpeed * rHat);
            float brakeStartDistance = ComputeBrakeStartDistance(rHat, currentVelocity, config);
            float correctionBlend = ComputeCorrectionBlend(
                distToTarget,
                brakeStartDistance,
                radialSpeed,
                approachSpeed,
                config);

            Vector2 cruiseVelocity = rHat * approachSpeed;
            Vector2 radialVelocity = rHat * radialSpeed;
            Vector2 desiredVelocity = cruiseVelocity
                - (lateralVelocity * lateralCorrectionGain)
                - (radialVelocity * (longitudinalCorrectionGain * correctionBlend));

            currentArrivalState = correctionBlend > 0.01f ? ArrivalState.Brake : ArrivalState.Cruise;

            return ClampDesiredVelocity(desiredVelocity, config, currentVelocity.magnitude);
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

            float brakeSpeed = currentVelocity.magnitude;
            float brakeDistance = (brakeSpeed * brakeSpeed) / (2f * maxAccel);

            float closingSpeed = Mathf.Max(0f, Vector2.Dot(currentVelocity, rHat));
            float closingDistance = (closingSpeed * closingSpeed) / (2f * maxAccel);

            Vector2 brakeHeading = GetBrakeDirection(currentVelocity, rHat);
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

        private float ComputeCorrectionBlend(
            float distToTarget,
            float brakeStartDistance,
            float radialSpeed,
            float approachSpeed,
            CascadedControlConfig config)
        {
            float distanceBlend = 0f;
            float blendRange = brakeStartDistance - stopRadius;
            if (blendRange > DirectionEpsilon)
            {
                distanceBlend = Mathf.Clamp01((brakeStartDistance - distToTarget) / blendRange);
            }
            else if (distToTarget <= brakeStartDistance)
            {
                distanceBlend = 1f;
            }

            float closingOverspeed = Mathf.Max(0f, radialSpeed - approachSpeed);
            float overspeedDenominator = Mathf.Max(desiredSpeed, config.desiredSpeedEpsilon);
            float overspeedBlend = overspeedDenominator > DirectionEpsilon
                ? Mathf.Clamp01(closingOverspeed / overspeedDenominator)
                : 0f;

            return Mathf.Max(distanceBlend, overspeedBlend);
        }

        private Vector2 ClampDesiredVelocity(
            Vector2 desiredVelocity,
            CascadedControlConfig config,
            float currentSpeed)
        {
            float maxCommandSpeed = Mathf.Max(0f, desiredSpeed);
            if (desiredVelocity.sqrMagnitude <= DirectionEpsilon || maxCommandSpeed <= DirectionEpsilon)
            {
                return Vector2.zero;
            }

            float commandSpeed = desiredVelocity.magnitude;
            float clampedSpeed = Mathf.Min(commandSpeed, maxCommandSpeed);
            float minCommandSpeed = Mathf.Max(brakeCommandSpeed, config.desiredSpeedEpsilon + 0.01f);
            if (currentSpeed > stopSpeed && clampedSpeed < minCommandSpeed)
            {
                clampedSpeed = Mathf.Min(maxCommandSpeed, minCommandSpeed);
            }

            return desiredVelocity * (clampedSpeed / commandSpeed);
        }

        private Vector2 ComputeDesiredThrustDirection(
            Vector2 desiredVelocity,
            Vector2 currentVelocity,
            Vector2 fallbackDirection)
        {
            Vector2 velocityCorrection = desiredVelocity - currentVelocity;
            if (velocityCorrection.sqrMagnitude > DirectionEpsilon)
            {
                return velocityCorrection;
            }

            if (desiredVelocity.sqrMagnitude > DirectionEpsilon)
            {
                return desiredVelocity;
            }

            return fallbackDirection;
        }

        private Vector2 GetBrakeDirection(Vector2 currentVelocity, Vector2 fallbackDirection)
        {
            if (currentVelocity.sqrMagnitude > DirectionEpsilon)
            {
                return -currentVelocity.normalized;
            }

            return fallbackDirection;
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
