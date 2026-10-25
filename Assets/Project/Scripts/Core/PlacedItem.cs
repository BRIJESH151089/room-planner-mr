using System;
using UnityEngine;

namespace RoomPlanner.Core
{
    // A furniture item standing on the floor: center (X, Z) in metres in the room footprint's frame,
    // turned by YawDegrees. A struct so the ghost preview can build a candidate every frame without allocating.
    // Id is a stable identifier (32 hex chars from a Guid) given when the item is placed; it survives moves,
    // undo and redo, and is what save/load will key on. A ghost candidate that has not been placed has no Id.
    public readonly struct PlacedItem
    {
        public FurnitureDefinition Definition { get; }
        public float X { get; }
        public float Z { get; }
        public float YawDegrees { get; }
        public string Id { get; }

        public PlacedItem(FurnitureDefinition definition, float x, float z, float yawDegrees, string id = null)
        {
            if (id != null && string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id must be null (not placed yet) or non-empty.", nameof(id));
            }

            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            X = x;
            Z = z;
            YawDegrees = yawDegrees;
            Id = id;
        }

        public bool HasId => Id != null;

        public static string NewId() => Guid.NewGuid().ToString("N");

        public PlacedItem WithId(string id) => new PlacedItem(Definition, X, Z, YawDegrees, id);

        // Same item (same Id and definition) at a new position and yaw.
        public PlacedItem MovedTo(float x, float z, float yawDegrees) => new PlacedItem(Definition, x, z, yawDegrees, Id);

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
