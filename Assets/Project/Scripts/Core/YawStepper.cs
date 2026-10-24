using UnityEngine;

namespace RoomPlanner.Core
{
    // Furniture yaw in fixed 15 degree steps, always kept in [0, 360).
    public static class YawStepper
    {
        public const float StepDegrees = 15f;

        public static float Normalize(float yawDegrees)
        {
            float wrapped = yawDegrees % 360f;
            if (wrapped < 0f)
            {
                wrapped += 360f;
            }

            // -0.0001 % 360 + 360 can round to exactly 360.
            return wrapped >= 360f ? 0f : wrapped;
        }

        public static float Snap(float yawDegrees) => Normalize(Mathf.Round(yawDegrees / StepDegrees) * StepDegrees);

        // steps > 0 turns clockwise seen from above, steps < 0 counter-clockwise.
        public static float Step(float yawDegrees, int steps) => Snap(yawDegrees + steps * StepDegrees);
    }
}
