using UnityEngine;

namespace RoomPlanner.Core
{
    // Rotation in the horizontal (x, z) plane using Unity's yaw convention: a positive yaw turns
    // clockwise seen from above, so yaw 90 turns local +z (forward) into +x (right). Vector2.y holds z.
    public static class PlanarRotation
    {
        public static Vector2 Rotate(Vector2 local, float yawDegrees)
        {
            float radians = yawDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(local.x * cos + local.y * sin, -local.x * sin + local.y * cos);
        }

        public static Vector2 InverseRotate(Vector2 world, float yawDegrees) => Rotate(world, -yawDegrees);
    }
}
