using System;
using UnityEngine;

namespace RoomPlanner.Core
{
    // A furniture item standing on the floor: center (X, Z) in metres in the room footprint's frame,
    // turned by YawDegrees. A struct so the ghost preview can build a candidate every frame without allocating.
    public readonly struct PlacedItem
    {
        public FurnitureDefinition Definition { get; }
        public float X { get; }
        public float Z { get; }
        public float YawDegrees { get; }

        public PlacedItem(FurnitureDefinition definition, float x, float z, float yawDegrees)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            X = x;
            Z = z;
            YawDegrees = yawDegrees;
        }

        public OrientedRect Footprint => new OrientedRect(new Vector2(X, Z), Definition.Width, Definition.Depth, YawDegrees);

        public void GetCorners(Span<Vector2> corners) => Footprint.GetCorners(corners);

        public Vector2[] GetCorners()
        {
            var corners = new Vector2[4];
            GetCorners(corners);
            return corners;
        }
    }
}
