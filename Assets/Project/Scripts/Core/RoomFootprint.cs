using System;

namespace RoomPlanner.Core
{
    public readonly struct RoomFootprint
    {
        public float Width { get; }
        public float Depth { get; }
        public float Margin { get; }

        public RoomFootprint(float width, float depth, float margin)
        {
            if (width <= 0f)
            {
                throw new ArgumentException("Width must be positive.", nameof(width));
            }

            if (depth <= 0f)
            {
                throw new ArgumentException("Depth must be positive.", nameof(depth));
            }

            if (margin < 0f)
            {
                throw new ArgumentException("Margin must not be negative.", nameof(margin));
            }

            Width = width;
            Depth = depth;
            Margin = margin;
        }
    }
}
