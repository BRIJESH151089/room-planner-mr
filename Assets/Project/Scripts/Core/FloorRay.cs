using UnityEngine;

namespace RoomPlanner.Core
{
    // Intersects a ray with the horizontal floor plane y = floorHeight.
    public static class FloorRay
    {
        private const float MinDownward = 1e-4f;

        // No hit when the ray points up or runs parallel to the floor, when the floor is behind the origin,
        // or when the hit is farther than maxDistance (measured along the ray).
        public static bool TryHit(Vector3 origin, Vector3 direction, float floorHeight, out Vector3 hit, float maxDistance = float.PositiveInfinity)
        {
            hit = default;

            float lengthSquared = direction.sqrMagnitude;
            if (lengthSquared < MinDownward * MinDownward)
            {
                return false;
            }

            Vector3 unitDirection = direction / Mathf.Sqrt(lengthSquared);
            if (unitDirection.y > -MinDownward)
            {
                return false;
            }

            float distance = (floorHeight - origin.y) / unitDirection.y;
            if (distance < 0f || distance > maxDistance)
            {
                return false;
            }

            hit = origin + unitDirection * distance;
            hit.y = floorHeight;
            return true;
        }
    }
}
