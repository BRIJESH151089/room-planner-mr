using System;
using UnityEngine;

namespace RoomPlanner.Core
{
    public static class GridSnapper
    {
        // Ghost positions snap to a 5 cm grid in the room's own frame, so items line up with the walls.
        public const float PositionStep = 0.05f;

        public static float Snap(float value, float step)
        {
            if (step <= 0f)
            {
                throw new ArgumentException("Step must be positive.", nameof(step));
            }

            return Mathf.Round(value / step) * step;
        }

        public static Vector2 Snap(Vector2 value, float step) => new Vector2(Snap(value.x, step), Snap(value.y, step));
    }
}
