using MovementV2.Control;
using MovementV2.Core;
using UnityEngine;

namespace MovementV2.Demo
{
    [RequireComponent(typeof(MotionControllerRunner))]
    public class ClickToMoveDesiredMotion : MonoBehaviour
    {
        private const float DirectionEpsilon = 1e-6f;
        private static readonly Color CorrectStateColor = new Color(1f, 0.5f, 0f);

        private enum ArrivalState
        {
            Idle,
            Cruise,
            Correct,
            Flip,
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
        [SerializeField] [Range(0f, 1f)] private float flipBiasProgress = 0.5f;
        [SerializeField] [Min(0f)] private float flipBiasWidth = 0.2f;
        [SerializeField] [Min(0f)] private float flipBiasStrength = 1f;

        [Header("Steering")]
        [SerializeField] [Min(0f)] private float lateralCorrectionGain = 1f;
        [SerializeField] [Min(0f)] private float longitudinalCorrectionGain = 1f;
        [SerializeField] [Min(0f)] private float thrustDirectionSmoothing = 10f;
        [SerializeField] [Min(1f)] private float maxLateralCorrectionMultiplier = 2f;
        [SerializeField] [Min(0f)] private float lateralCorrectionDistancePadding = 0.25f;

        [Header("Stability")]
        [SerializeField] [Min(0f)] private float innerStabilityRadius = 0.9f;
        [SerializeField] [Min(0f)] private float innerVelocityFadeStrength = 1f;
        [SerializeField] private bool innerDisableMinCommand = true;

        private MotionControllerRunner runner;
        private Rigidbody2D rb;
        private Camera cam;
        private Vector2? targetWorld;
        private float initialTargetDistance;
        private bool hasInitialTargetDistance;
        private Vector2 lastStableTargetDirection;
        private Vector2 smoothedThrustDirection;
        [SerializeField] private ArrivalState currentArrivalState = ArrivalState.Idle;

        public string CurrentArrivalStateName => currentArrivalState.ToString();

        private void Awake()
        {
            runner = GetComponent<MotionControllerRunner>();
            rb = GetComponent<Rigidbody2D>();
            cam = Camera.main;
            lastStableTargetDirection = transform.up;
        }

        private void OnValidate()
        {
            if (innerStabilityRadius < stopRadius)
            {
                innerStabilityRadius = stopRadius;
            }
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
                Vector2 clickOrigin = transform.position;
                Vector2 deltaToClick = targetWorld.Value - clickOrigin;
                initialTargetDistance = deltaToClick.magnitude;
                hasInitialTargetDistance = initialTargetDistance > DirectionEpsilon;
                if (hasInitialTargetDistance)
                {
                    lastStableTargetDirection = deltaToClick / initialTargetDistance;
                }

                smoothedThrustDirection = Vector2.zero;
            }

            if (!targetWorld.HasValue)
            {
                runner.Desired = DesiredMotion.Stop;
                hasInitialTargetDistance = false;
                smoothedThrustDirection = Vector2.zero;
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
                smoothedThrustDirection = Vector2.zero;
                currentArrivalState = ArrivalState.Stop;
                return DesiredMotion.Stop;
            }

            Vector2 rawTargetDirection = distToTarget > DirectionEpsilon ? (deltaToTarget / distToTarget) : Vector2.zero;
            float innerBlend = ComputeInnerBlend(distToTarget);
            Vector2 guidanceDirection = ResolveGuidanceDirection(rawTargetDirection);
            float progress = ComputeProgress(distToTarget);
            float correctionBlend = 0f;
            Vector2 desiredVelocity = rb == null
                ? (guidanceDirection * desiredSpeed)
                : ComputeDesiredVelocity(distToTarget, guidanceDirection, innerBlend, out correctionBlend);
            Vector2 desiredThrustDirection = rb == null
                ? desiredVelocity
                : ComputeDesiredThrustDirection(
                    desiredVelocity,
                    rb.velocity,
                    guidanceDirection,
                    progress,
                    correctionBlend,
                    innerBlend,
                    Time.deltaTime);

            UpdateArrivalState(
                desiredThrustDirection,
                rb != null ? rb.velocity : Vector2.zero,
                desiredVelocity,
                correctionBlend);

            return desiredVelocity.sqrMagnitude > DirectionEpsilon || desiredThrustDirection.sqrMagnitude > DirectionEpsilon
                ? new DesiredMotion(desiredVelocity, 0f, desiredThrustDirection)
                : DesiredMotion.Stop;
        }

        private Vector2 ComputeDesiredVelocity(
            float distToTarget,
            Vector2 guidanceDirection,
            float innerBlend,
            out float correctionBlend)
        {
            Vector2 currentVelocity = rb.velocity;
            CascadedControlConfig config = runner.GetConfig();
            float approachSpeed = ComputeApproachSpeed(distToTarget, config);
            float radialSpeed = Vector2.Dot(currentVelocity, guidanceDirection);
            Vector2 lateralVelocity = currentVelocity - (radialSpeed * guidanceDirection);
            float brakeStartDistance = ComputeBrakeStartDistance(guidanceDirection, currentVelocity, config);
            float lateralCorrectionScale = ComputeLateralCorrectionScale(
                distToTarget,
                lateralVelocity,
                config);
            correctionBlend = ComputeCorrectionBlend(
                distToTarget,
                brakeStartDistance,
                radialSpeed,
                approachSpeed,
                config);

            Vector2 cruiseVelocity = guidanceDirection * approachSpeed;
            Vector2 radialVelocity = guidanceDirection * radialSpeed;
            Vector2 desiredVelocity = cruiseVelocity
                - (lateralVelocity * lateralCorrectionScale)
                - (radialVelocity * (longitudinalCorrectionGain * correctionBlend));
            float velocityFade = Mathf.Clamp01(1f - (innerBlend * innerVelocityFadeStrength));
            desiredVelocity *= velocityFade;

            return ClampDesiredVelocity(desiredVelocity, config, currentVelocity.magnitude, distToTarget, innerBlend);
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

        private float ComputeLateralCorrectionScale(
            float distToTarget,
            Vector2 lateralVelocity,
            CascadedControlConfig config)
        {
            float maxAccel = ComputeMaxAcceleration(config);
            if (maxAccel <= DirectionEpsilon)
            {
                return lateralCorrectionGain;
            }

            float lateralSpeed = lateralVelocity.magnitude;
            if (lateralSpeed <= DirectionEpsilon)
            {
                return lateralCorrectionGain;
            }

            float lateralStopDistance = (lateralSpeed * lateralSpeed) / (2f * maxAccel);
            float effectiveDistance = Mathf.Max(distToTarget - stopRadius, DirectionEpsilon);
            float lateralUrgency = Mathf.Clamp01(
                (lateralStopDistance + lateralCorrectionDistancePadding) / effectiveDistance);
            return lateralCorrectionGain * Mathf.Lerp(1f, maxLateralCorrectionMultiplier, lateralUrgency);
        }

        private float ComputeMaxAcceleration(CascadedControlConfig config)
        {
            float maxThrust = config.limits.maxThrust;
            if (maxThrust <= 0f)
            {
                return 0f;
            }

            float mass = rb != null ? Mathf.Max(0.0001f, rb.mass) : 1f;
            return maxThrust / mass;
        }

        private float ComputeProgress(float distToTarget)
        {
            if (!hasInitialTargetDistance || initialTargetDistance <= DirectionEpsilon)
            {
                return 1f;
            }

            return Mathf.Clamp01(1f - (distToTarget / initialTargetDistance));
        }

        private float ComputeInnerBlend(float distToTarget)
        {
            if (distToTarget >= innerStabilityRadius)
            {
                return 0f;
            }

            if (distToTarget <= stopRadius)
            {
                return 1f;
            }

            float range = innerStabilityRadius - stopRadius;
            if (range <= DirectionEpsilon)
            {
                return 1f;
            }

            return Mathf.Clamp01((innerStabilityRadius - distToTarget) / range);
        }

        private Vector2 ResolveGuidanceDirection(Vector2 rawTargetDirection)
        {
            if (rawTargetDirection.sqrMagnitude > DirectionEpsilon)
            {
                lastStableTargetDirection = rawTargetDirection;
                return rawTargetDirection;
            }

            return lastStableTargetDirection.sqrMagnitude > DirectionEpsilon
                ? lastStableTargetDirection.normalized
                : transform.up;
        }

        private Vector2 ClampDesiredVelocity(
            Vector2 desiredVelocity,
            CascadedControlConfig config,
            float currentSpeed,
            float distToTarget,
            float innerBlend)
        {
            float maxCommandSpeed = Mathf.Max(0f, desiredSpeed);
            if (desiredVelocity.sqrMagnitude <= DirectionEpsilon || maxCommandSpeed <= DirectionEpsilon)
            {
                return Vector2.zero;
            }

            float commandSpeed = desiredVelocity.magnitude;
            float clampedSpeed = Mathf.Min(commandSpeed, maxCommandSpeed);
            float minCommandSpeed = Mathf.Max(brakeCommandSpeed, config.desiredSpeedEpsilon + 0.01f);
            if (innerDisableMinCommand && distToTarget <= innerStabilityRadius)
            {
                minCommandSpeed *= Mathf.Clamp01(1f - innerBlend);
            }

            if (currentSpeed > stopSpeed && clampedSpeed < minCommandSpeed)
            {
                clampedSpeed = Mathf.Min(maxCommandSpeed, minCommandSpeed);
            }

            return desiredVelocity * (clampedSpeed / commandSpeed);
        }

        private Vector2 ComputeDesiredThrustDirection(
            Vector2 desiredVelocity,
            Vector2 currentVelocity,
            Vector2 fallbackDirection,
            float progress,
            float correctionBlend,
            float innerBlend,
            float dt)
        {
            Vector2 baseCorrection = desiredVelocity - currentVelocity;
            Vector2 retrogradeDirection = currentVelocity.sqrMagnitude > DirectionEpsilon
                ? -currentVelocity.normalized
                : fallbackDirection;
            float midpointBlend = ComputeMidpointBlend(progress);
            float retrogradeBlend = Mathf.Clamp01(Mathf.Max(correctionBlend, midpointBlend * flipBiasStrength));
            Vector2 retrogradeBias = retrogradeDirection * currentVelocity.magnitude * retrogradeBlend;
            Vector2 desiredThrustVector = baseCorrection + retrogradeBias;

            Vector2 rawDirection;
            if (desiredThrustVector.sqrMagnitude > DirectionEpsilon)
            {
                rawDirection = desiredThrustVector.normalized;
            }
            else if (desiredVelocity.sqrMagnitude > DirectionEpsilon)
            {
                rawDirection = desiredVelocity.normalized;
            }
            else
            {
                rawDirection = fallbackDirection;
            }

            if (rawDirection.sqrMagnitude <= DirectionEpsilon)
            {
                return smoothedThrustDirection.sqrMagnitude > DirectionEpsilon
                    ? smoothedThrustDirection.normalized
                    : Vector2.zero;
            }

            if (smoothedThrustDirection.sqrMagnitude <= DirectionEpsilon || thrustDirectionSmoothing <= DirectionEpsilon || dt <= 0f)
            {
                smoothedThrustDirection = rawDirection;
                return rawDirection;
            }

            float effectiveSmoothing = Mathf.Lerp(thrustDirectionSmoothing, thrustDirectionSmoothing * 3f, innerBlend);
            float alpha = 1f - Mathf.Exp(-effectiveSmoothing * dt);
            Vector2 blendedDirection = Vector2.Lerp(smoothedThrustDirection, rawDirection, alpha);
            smoothedThrustDirection = blendedDirection.sqrMagnitude > DirectionEpsilon
                ? blendedDirection.normalized
                : rawDirection;
            return smoothedThrustDirection;
        }

        private float ComputeMidpointBlend(float progress)
        {
            float halfWidth = flipBiasWidth * 0.5f;
            float start = Mathf.Clamp01(flipBiasProgress - halfWidth);
            float end = Mathf.Clamp01(flipBiasProgress + halfWidth);
            if (end <= start + DirectionEpsilon)
            {
                return progress >= end ? 1f : 0f;
            }

            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, end, progress));
        }

        private void UpdateArrivalState(
            Vector2 desiredThrustDirection,
            Vector2 currentVelocity,
            Vector2 desiredVelocity,
            float correctionBlend)
        {
            if (desiredVelocity.sqrMagnitude <= DirectionEpsilon && desiredThrustDirection.sqrMagnitude <= DirectionEpsilon)
            {
                currentArrivalState = ArrivalState.Stop;
                return;
            }

            if (currentVelocity.sqrMagnitude <= DirectionEpsilon || desiredThrustDirection.sqrMagnitude <= DirectionEpsilon)
            {
                currentArrivalState = ArrivalState.Cruise;
                return;
            }

            float alignment = Vector2.Dot(desiredThrustDirection.normalized, currentVelocity.normalized);
            if (alignment < -0.35f)
            {
                currentArrivalState = ArrivalState.Flip;
                return;
            }

            currentArrivalState = correctionBlend > 0.01f ? ArrivalState.Correct : ArrivalState.Cruise;
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
                case ArrivalState.Correct:
                    return CorrectStateColor;
                case ArrivalState.Flip:
                    return Color.red;
                case ArrivalState.Stop:
                    return Color.yellow;
                default:
                    return Color.gray;
            }
        }
    }
}
