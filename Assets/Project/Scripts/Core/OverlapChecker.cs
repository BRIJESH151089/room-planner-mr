using System;
using UnityEngine;

namespace RoomPlanner.Core
{
    // Separating axis theorem for convex shapes on the floor plane. Two convex polygons do not overlap
    // if there is an axis (the normal of one of their edges) on which their projections do not overlap.
    // Shapes that only touch (projections meet exactly, within Epsilon) do NOT count as overlapping,
    // so furniture can stand flush against other furniture or a wall.
    // Uses stack memory only: safe to call every frame without allocating.
    public static class OverlapChecker
    {
        public const float Epsilon = 1e-5f;

        public static bool Overlaps(in OrientedRect a, in OrientedRect b)
        {
            Span<Vector2> cornersA = stackalloc Vector2[4];
            Span<Vector2> cornersB = stackalloc Vector2[4];
            a.GetCorners(cornersA);
            b.GetCorners(cornersB);
            return ConvexPolygonsOverlap(cornersA, cornersB);
        }

        // A wall is a segment: a two-point "polygon" whose only axis is the wall's normal.
        public static bool SegmentOverlaps(Vector2 start, Vector2 end, in OrientedRect rect)
        {
            Span<Vector2> segment = stackalloc Vector2[2];
            segment[0] = start;
            segment[1] = end;
            Span<Vector2> corners = stackalloc Vector2[4];
            rect.GetCorners(corners);
            return ConvexPolygonsOverlap(segment, corners);
        }

        // Vertices of each polygon must be in order around it (either winding).
        public static bool ConvexPolygonsOverlap(ReadOnlySpan<Vector2> a, ReadOnlySpan<Vector2> b)
        {
            return !HasSeparatingEdgeNormal(a, a, b) && !HasSeparatingEdgeNormal(b, a, b);
        }

        private static bool HasSeparatingEdgeNormal(ReadOnlySpan<Vector2> edgeSource, ReadOnlySpan<Vector2> a, ReadOnlySpan<Vector2> b)
        {
            for (int i = 0; i < edgeSource.Length; i++)
            {
                Vector2 edge = edgeSource[(i + 1) % edgeSource.Length] - edgeSource[i];
                if (edge.sqrMagnitude < Epsilon * Epsilon)
                {
                    continue;
                }

                // Unit-length axis so Epsilon is a distance in metres.
                Vector2 axis = new Vector2(-edge.y, edge.x).normalized;
                Project(a, axis, out float minA, out float maxA);
                Project(b, axis, out float minB, out float maxB);

                if (maxA <= minB + Epsilon || maxB <= minA + Epsilon)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Project(ReadOnlySpan<Vector2> points, Vector2 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            for (int i = 0; i < points.Length; i++)
            {
                float projection = Vector2.Dot(points[i], axis);
                min = Mathf.Min(min, projection);
                max = Mathf.Max(max, projection);
            }
        }
    }
}
