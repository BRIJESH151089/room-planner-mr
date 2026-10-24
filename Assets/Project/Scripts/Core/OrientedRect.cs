using System;
using UnityEngine;

namespace RoomPlanner.Core
{
    // A rectangle on the floor plane: Width along its local x axis, Depth along its local z axis,
    // turned by YawDegrees around its center (see PlanarRotation for the yaw convention).
    public readonly struct OrientedRect
    {
        public Vector2 Center { get; }
        public float Width { get; }
        public float Depth { get; }
        public float YawDegrees { get; }

        public OrientedRect(Vector2 center, float width, float depth, float yawDegrees)
        {
            Center = center;
            Width = width;
            Depth = depth;
            YawDegrees = yawDegrees;
        }

        public OrientedRect Inflate(float amount) => new OrientedRect(Center, Width + 2f * amount, Depth + 2f * amount, YawDegrees);

        // Writes the four corners in order around the rectangle into `corners` (length 4 or more).
        public void GetCorners(Span<Vector2> corners)
        {
            if (corners.Length < 4)
            {
                throw new ArgumentException("Need room for 4 corners.", nameof(corners));
            }

            float halfWidth = Width / 2f;
            float halfDepth = Depth / 2f;
            corners[0] = Center + PlanarRotation.Rotate(new Vector2(-halfWidth, -halfDepth), YawDegrees);
            corners[1] = Center + PlanarRotation.Rotate(new Vector2(halfWidth, -halfDepth), YawDegrees);
            corners[2] = Center + PlanarRotation.Rotate(new Vector2(halfWidth, halfDepth), YawDegrees);
            corners[3] = Center + PlanarRotation.Rotate(new Vector2(-halfWidth, halfDepth), YawDegrees);
        }
    }
}
