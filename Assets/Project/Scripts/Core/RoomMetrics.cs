using System;
using System.Collections.Generic;
using System.Globalization;

namespace RoomPlanner.Core
{
    // Measurements for the measurements panel. Width and Depth are the room's bounding box in its own frame,
    // FloorArea is the exact outline area (shoelace), UsedArea the sum of item footprints (placement keeps items
    // from overlapping, so the sum is the covered area) and FreeArea the rest, never below zero.
    public readonly struct RoomMetrics
    {
        public float Width { get; }
        public float Depth { get; }
        public float FloorArea { get; }
        public float UsedArea { get; }
        public float FreeArea { get; }
        public int ItemCount { get; }

        public RoomMetrics(float width, float depth, float floorArea, float usedArea, int itemCount)
        {
            Width = width;
            Depth = depth;
            FloorArea = floorArea;
            UsedArea = usedArea;
            FreeArea = Math.Max(0f, floorArea - usedArea);
            ItemCount = itemCount;
        }

        public static RoomMetrics Compute(RoomFootprint room, IReadOnlyList<PlacedItem> items)
        {
            if (room == null)
            {
                throw new ArgumentNullException(nameof(room));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            float used = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                used += items[i].Definition.Width * items[i].Definition.Depth;
            }

            return new RoomMetrics(room.Width, room.Depth, room.Area, used, items.Count);
        }
    }

    public static class MetricsFormatter
    {
        public static string Room(in RoomMetrics metrics) => Format("Room: {0:0.0} x {1:0.0} m", metrics.Width, metrics.Depth);

        public static string Floor(in RoomMetrics metrics) => Format("Floor: {0:0.0} m2", metrics.FloorArea);

        public static string Used(in RoomMetrics metrics) => Format("Used: {0:0.0} m2", metrics.UsedArea);

        public static string Free(in RoomMetrics metrics) => Format("Free: {0:0.0} m2", metrics.FreeArea);

        public static string Items(in RoomMetrics metrics) => Format("Items: {0}", metrics.ItemCount);

        // All five lines, one per line, for a single text field.
        public static string All(in RoomMetrics metrics) =>
            Room(metrics) + "\n" + Floor(metrics) + "\n" + Used(metrics) + "\n" + Free(metrics) + "\n" + Items(metrics);

        private static string Format(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
