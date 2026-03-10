using MovementV2.Core;
using UnityEngine;

namespace MovementV2.Demo
{
    [RequireComponent(typeof(MotionControllerRunner))]
    public class ClickToMoveDesiredMotion : MonoBehaviour
    {
        [SerializeField] private float desiredSpeed = 6f;
        [SerializeField] private float stopRadius = 0.5f;
        [SerializeField] private bool drawTarget = true;

        private MotionControllerRunner runner;
        private Camera cam;
        private Vector2? targetWorld;

        private void Awake()
        {
            runner = GetComponent<MotionControllerRunner>();
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
                return;
            }

            Vector2 current = transform.position;
            Vector2 delta = targetWorld.Value - current;
            float dist = delta.magnitude;

            if (dist <= stopRadius)
            {
                runner.Desired = DesiredMotion.Stop;
            }
            else
            {
                Vector2 desiredVelocity = delta.normalized * desiredSpeed;
                runner.Desired = new DesiredMotion(desiredVelocity, 0f);
            }

            if (drawTarget)
            {
                Debug.DrawLine(current, targetWorld.Value, Color.green);
                Debug.DrawRay(targetWorld.Value, Vector2.one * 0.05f, Color.magenta);
            }
        }
    }
}
