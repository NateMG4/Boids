using MovementV2.Core;

namespace MovementV2.Control
{
    public interface IMotionController
    {
        void Reset();
        ActuatorCommand Compute(in MotionState state, in DesiredMotion desired, float dt, float mass);
    }
}
