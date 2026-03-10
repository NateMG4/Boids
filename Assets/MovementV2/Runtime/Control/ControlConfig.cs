using UnityEngine;

namespace MovementV2.Control
{
    [System.Serializable]
    public struct ActuatorLimits
    {
        public float maxThrust;
        public float maxTorque;
        public float maxSpeed;
        public float maxYawRate;

        public static ActuatorLimits Default()
        {
            return new ActuatorLimits {
                maxThrust = 10f,
                maxTorque = 50f,
                maxSpeed = 8f,
                maxYawRate = 6f
            };
        }
    }

    [System.Serializable]
    public struct CascadedControlConfig
    {
        public PidGains headingGains;
        public PidGains yawRateGains;
        public PidGains speedGains;
        public ActuatorLimits limits;
        public float headingThrustGateRad;
        public float desiredSpeedEpsilon;

        public static CascadedControlConfig Default()
        {
            return new CascadedControlConfig {
                headingGains = PidGains.DefaultHeading(),
                yawRateGains = PidGains.DefaultYawRate(),
                speedGains = PidGains.DefaultSpeed(),
                limits = ActuatorLimits.Default(),
                headingThrustGateRad = Mathf.Deg2Rad * 50f,
                desiredSpeedEpsilon = 0.1f
            };
        }
    }
}
