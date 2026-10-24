using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoomPlanner.Core
{
    // The room's floor outline in the horizontal plane, built from the wall corners in order
    // (clockwise or counter-clockwise). Each corner is (x, z) in metres; corner i and corner i+1
    // are the two ends of one wall, and the last corner connects back to the first.
    //
    // Non-rectangular rooms (L-shapes, angled walls) keep their real polygon:
    // - Area and Contains use the exact polygon, so the cut-out part of an L-shaped room
    //   is outside the room and does not count towards the area.
    // - Width and Depth are the axis-aligned bounding box of the corners, so for an L-shape they
    //   describe the overall extent, not a usable rectangle. Callers should express the corners in
    //   the room's own frame (not world axes) so the box lines up with the walls.
    // The outline is assumed to be a simple polygon (edges do not cross), which is what Space Setup produces.
    public sealed class RoomFootprint
    {
        private const float Epsilon = 1e-5f;

        private readonly Vector2[] corners;

        public static RoomFootprint Empty { get; } = new RoomFootprint(Array.Empty<Vector2>(), 0f);

        public float Width { get; }
        public float Depth { get; }
        public float Height { get; }
        public float Area { get; }
        public int WallCount => corners.Length;
        public bool IsEmpty => corners.Length == 0;
        public IReadOnlyList<Vector2> Corners => corners;

        public RoomFootprint(IReadOnlyList<Vector2> wallCorners, float height)
        {
            if (wallCorners == null)
            {
                throw new ArgumentNullException(nameof(wallCorners));
            }

            if (height < 0f)
            {
                throw new ArgumentException("Height must not be negative.", nameof(height));
            }

            int count = wallCorners.Count;

            // Accept an outline that repeats the first corner at the end.
            if (count > 1 && (wallCorners[count - 1] - wallCorners[0]).sqrMagnitude < Epsilon * Epsilon)
            {
                count--;
            }

            if (count is 1 or 2)
            {
                throw new ArgumentException("A room outline needs at least 3 corners, or none for an empty room.", nameof(wallCorners));
            }

            corners = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                corners[i] = wallCorners[i];
            }

            Height = height;

            if (count == 0)
            {
                return;
            }

            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            float twiceArea = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % count];
                minX = Mathf.Min(minX, a.x);
                maxX = Mathf.Max(maxX, a.x);
                minZ = Mathf.Min(minZ, a.y);
                maxZ = Mathf.Max(maxZ, a.y);
                twiceArea += a.x * b.y - b.x * a.y; // shoelace formula
            }

            Width = maxX - minX;
            Depth = maxZ - minZ;
            Area = Mathf.Abs(twiceArea) / 2f;
        }

        // True when (x, z) is inside the outline and at least `margin` metres away from every wall.
        // With margin 0 a point exactly on a wall counts as inside.
        public bool Contains(float x, float z, float margin = 0f)
        {
            if (margin < 0f)
            {
                throw new ArgumentException("Margin must not be negative.", nameof(margin));
            }

            if (IsEmpty)
            {
                return false;
            }

            var point = new Vector2(x, z);
            float distanceToWall = DistanceToNearestWall(point);

            if (distanceToWall < margin - Epsilon)
            {
                return false;
            }

            if (distanceToWall <= Epsilon)
            {
                return true;
            }

            return IsInsidePolygon(point);
        }

        private float DistanceToNearestWall(Vector2 point)
        {
            float nearest = float.MaxValue;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % corners.Length];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
                nearest = Mathf.Min(nearest, Vector2.Distance(point, a + ab * t));
            }

            return nearest;
        }

        // Even-odd ray cast along +x.
        private bool IsInsidePolygon(Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = corners.Length - 1; i < corners.Length; j = i++)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[j];
                if ((a.y > point.y) != (b.y > point.y)
                    && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
