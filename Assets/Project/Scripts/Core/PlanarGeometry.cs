using System;
using UnityEngine;

namespace RoomPlanner.Core
{
    public static class PlanarGeometry
    {
        public static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0f)
            {
                return Vector2.Distance(point, start);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + segment * t);
        }

        // Shortest distance between a wall segment and a rectangle that it does not cross. For two
        // non-overlapping convex shapes the closest points are a vertex of one and an edge of the other,
        // so checking the rectangle's corners against the wall and the wall's ends against the
        // rectangle's edges is exact. Does not allocate.
        public static float SegmentToRectDistance(Vector2 start, Vector2 end, in OrientedRect rect)
        {
            Span<Vector2> corners = stackalloc Vector2[4];
            rect.GetCorners(corners);

            float nearest = float.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                Vector2 edgeStart = corners[i];
                Vector2 edgeEnd = corners[(i + 1) % 4];
                nearest = Mathf.Min(nearest, DistanceToSegment(edgeStart, start, end));
                nearest = Mathf.Min(nearest, DistanceToSegment(start, edgeStart, edgeEnd));
                nearest = Mathf.Min(nearest, DistanceToSegment(end, edgeStart, edgeEnd));
            }

            return nearest;
        }
    }
}
