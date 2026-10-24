namespace RoomPlanner.Core
{
    // Turns a thumbstick axis (-1..1) into discrete steps: one step per push, not a repeat while held.
    // A push counts when the axis passes PressThreshold; the next push needs the stick back
    // inside ReleaseThreshold first (hysteresis, so noise near the threshold cannot double-step).
    public sealed class AxisStepDetector
    {
        public const float PressThreshold = 0.7f;
        public const float ReleaseThreshold = 0.3f;

        private bool armed = true;

        // Returns +1 for a push to the positive side, -1 for the negative side, 0 otherwise.
        public int Update(float axis)
        {
            if (armed)
            {
                if (axis >= PressThreshold)
                {
                    armed = false;
                    return 1;
                }

                if (axis <= -PressThreshold)
                {
                    armed = false;
                    return -1;
                }
            }
            else if (axis > -ReleaseThreshold && axis < ReleaseThreshold)
            {
                armed = true;
            }

            return 0;
        }
    }
}
