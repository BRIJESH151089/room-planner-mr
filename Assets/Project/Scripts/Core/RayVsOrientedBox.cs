using System.Collections.Generic;
using UnityEngine;

namespace RoomPlanner.Core
{
    // Ray against a furniture box: the item's footprint (an OrientedRect on the floor) extruded from the floor
    // up to its height. Used for picking placed items, so the prefabs need no colliders. Does not allocate.
    public static class RayVsOrientedBox
    {
        private const float ParallelEpsilon = 1e-6f;

        // origin and direction are in the footprint's frame: x/z as in the rect, y measured up from the floor.
        // distance is measured along the normalised direction; 0 when the origin is inside the box.
        public static bool TryIntersect(Vector3 origin, Vector3 direction, in OrientedRect footprint, float height,
            out float distance, float maxDistance = float.PositiveInfinity)
        {
            distance = 0f;
            if (direction.sqrMagnitude < ParallelEpsilon)
            {
                return false;
            }

            direction.Normalize();

            // Into the box's own frame: centred on the footprint, axes along its width and depth.
            Vector2 localOrigin = PlanarRotation.InverseRotate(new Vector2(origin.x, origin.z) - footprint.Center, footprint.YawDegrees);
            Vector2 localDirection = PlanarRotation.InverseRotate(new Vector2(direction.x, direction.z), footprint.YawDegrees);

            float tMin = 0f;
            float tMax = maxDistance;
            float halfWidth = footprint.Width / 2f;
            float halfDepth = footprint.Depth / 2f;

            if (!Slab(localOrigin.x, localDirection.x, -halfWidth, halfWidth, ref tMin, ref tMax)
                || !Slab(origin.y, direction.y, 0f, height, ref tMin, ref tMax)
                || !Slab(localOrigin.y, localDirection.y, -halfDepth, halfDepth, ref tMin, ref tMax))
            {
                return false;
            }

            distance = tMin;
            return true;
        }

        // World-space ray against every placed item; returns the index of the nearest hit, or -1.
        public static int FindNearest(Vector3 worldOrigin, Vector3 worldDirection, FloorFrame frame,
            IReadOnlyList<PlacedItem> items, float maxDistance, out float distance)
        {
            distance = float.PositiveInfinity;
            if (worldDirection.sqrMagnitude < ParallelEpsilon)
            {
                return -1;
            }

            worldDirection.Normalize();
            Vector2 planarOrigin = frame.ToLocal(worldOrigin.x, worldOrigin.z);
            Vector2 planarDirection = PlanarRotation.InverseRotate(new Vector2(worldDirection.x, worldDirection.z), frame.YawDegrees);
            var origin = new Vector3(planarOrigin.x, worldOrigin.y - frame.Height, planarOrigin.y);
            var direction = new Vector3(planarDirection.x, worldDirection.y, planarDirection.y);

            int nearest = -1;
            for (int i = 0; i < items.Count; i++)
            {
                PlacedItem item = items[i];
                if (TryIntersect(origin, direction, item.Footprint, item.Definition.Height, out float hit, maxDistance)
                    && hit < distance)
                {
                    distance = hit;
                    nearest = i;
                }
            }

            return nearest;
        }

        private static bool Slab(float origin, float direction, float min, float max, ref float tMin, ref float tMax)
        {
            if (Mathf.Abs(direction) < ParallelEpsilon)
            {
                return origin >= min && origin <= max;
            }

            float t1 = (min - origin) / direction;
            float t2 = (max - origin) / direction;
            if (t1 > t2)
            {
                (t1, t2) = (t2, t1);
            }

            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            return tMin <= tMax;
        }
    }
}
