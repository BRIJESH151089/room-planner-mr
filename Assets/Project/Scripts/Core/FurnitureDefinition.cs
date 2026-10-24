using System;

namespace RoomPlanner.Core
{
    // One kind of furniture. Width runs along the item's local x axis, Depth along its local z axis,
    // Height is vertical; all in metres. Immutable.
    public sealed class FurnitureDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public float Width { get; }
        public float Depth { get; }
        public float Height { get; }

        public FurnitureDefinition(string id, string displayName, float width, float depth, float height)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id must not be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name must not be empty.", nameof(displayName));
            }

            if (width <= 0f || depth <= 0f || height <= 0f)
            {
                throw new ArgumentException("Width, depth and height must be positive.");
            }

            Id = id;
            DisplayName = displayName;
            Width = width;
            Depth = depth;
            Height = height;
        }
    }
}
