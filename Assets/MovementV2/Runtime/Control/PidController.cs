using UnityEngine;

namespace MovementV2.Control
{
    [System.Serializable]
    public struct PidGains
    {
        public float kp;
        public float ki;
        public float kd;
        public float iClamp;
        public Vector2 outputClamp;

        public static PidGains DefaultHeading()
        {
            return new PidGains {
                kp = 4f,
                ki = 0f,
                kd = 0.2f,
                iClamp = 1f,
                outputClamp = new Vector2(-8f, 8f)
            };
        }

        public static PidGains DefaultYawRate()
        {
            return new PidGains {
                kp = 5f,
                ki = 0f,
                kd = 0.05f,
                iClamp = 2f,
                outputClamp = new Vector2(-9999f, 9999f)
            };
        }

        public static PidGains DefaultSpeed()
        {
            return new PidGains {
                kp = 3f,
                ki = 0f,
                kd = 0.1f,
                iClamp = 2f,
                outputClamp = new Vector2(-30f, 30f)
            };
        }
    }

    [System.Serializable]
    public class PidController
    {
        [SerializeField] private PidGains gains;
        [SerializeField] private float integral;
        [SerializeField] private float previousError;
        [SerializeField] private bool hasPrevious;

        public PidController(PidGains gainsIn)
        {
            gains = gainsIn;
            integral = 0f;
            previousError = 0f;
            hasPrevious = false;
        }

        public PidGains Gains
        {
            get => gains;
            set => gains = value;
        }

        public void Reset()
        {
            integral = 0f;
            previousError = 0f;
            hasPrevious = false;
        }

        public float Step(float error, float dt)
        {
            if (dt <= 0f)
            {
                return 0f;
            }

            integral += error * dt;
            integral = Mathf.Clamp(integral, -gains.iClamp, gains.iClamp);

            float derivative = 0f;
            if (hasPrevious)
            {
                derivative = (error - previousError) / dt;
            }

            float output = gains.kp * error + gains.ki * integral + gains.kd * derivative;
            output = Mathf.Clamp(output, gains.outputClamp.x, gains.outputClamp.y);

            previousError = error;
            hasPrevious = true;
            return output;
        }
    }
}
