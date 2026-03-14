using MovementV2.Control;
using MovementV2.Core;
using MovementV2.UnityAdapters;
using UnityEngine;

namespace MovementV2.Demo
{
    [RequireComponent(typeof(Rigidbody2DMotionAdapter))]
    public class MotionControllerRunner : MonoBehaviour
    {
        [Header("Control")]
        [SerializeField] private CascadedControlConfig config = default;
        [SerializeField] private bool useDefaultConfig = true;
        [SerializeField] private bool clampSpeed = true;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRays = true;
        [SerializeField] private float debugRayScale = 0.25f;

        private Rigidbody2DMotionAdapter adapter;
        private CascadedPidMotionController controller;

        public DesiredMotion Desired { get; set; } = DesiredMotion.Stop;

        private void Awake()
        {
            adapter = GetComponent<Rigidbody2DMotionAdapter>();
            if (useDefaultConfig)
            {
                config = CascadedControlConfig.Default();
            }

            controller = new CascadedPidMotionController(config);
            ApplyCurrentConfig();
        }

        private void OnValidate()
        {
            if (useDefaultConfig && !Application.isPlaying)
            {
                config = CascadedControlConfig.Default();
            }

            ApplyCurrentConfig();
        }

        private void FixedUpdate()
        {
            MotionState state = adapter.ReadState();
            float dt = Time.fixedDeltaTime;
            float mass = adapter.Body.mass;

            ActuatorCommand cmd = controller.Compute(state, Desired, dt, mass);
            adapter.Apply(cmd);

            if (clampSpeed)
            {
                adapter.ClampSpeed(config.limits.maxSpeed);
            }

            if (drawDebugRays)
            {
                Vector2 pos = state.pose.p_w;
                Debug.DrawRay(pos, state.twist.v_w * debugRayScale, Color.yellow);
                Debug.DrawRay(pos, Desired.desiredVelocityWorld * debugRayScale, Color.cyan);
            }
        }

        public CascadedControlConfig GetConfig()
        {
            return config;
        }

        public void SetConfig(CascadedControlConfig newConfig)
        {
            config = newConfig;
            ApplyCurrentConfig();
        }

        public void ResetController()
        {
            controller.Reset();
        }

        private void ApplyCurrentConfig()
        {
            if (controller == null)
            {
                return;
            }

            controller.Config = config;
        }
    }
}
