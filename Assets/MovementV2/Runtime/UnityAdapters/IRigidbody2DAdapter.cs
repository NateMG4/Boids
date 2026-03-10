using MovementV2.Core;

namespace MovementV2.UnityAdapters
{
    public interface IRigidbody2DAdapter
    {
        MotionState ReadState();
        void Apply(in ActuatorCommand command);
    }
}
