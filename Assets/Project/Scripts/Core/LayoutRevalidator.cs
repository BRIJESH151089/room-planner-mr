using System;
using System.Collections.Generic;

namespace RoomPlanner.Core
{
    public sealed class LayoutFit
    {
        public IReadOnlyList<PlacedItem> Accepted { get; }
        public IReadOnlyList<PlacedItem> Skipped { get; }

        public LayoutFit(IReadOnlyList<PlacedItem> accepted, IReadOnlyList<PlacedItem> skipped)
        {
            Accepted = accepted;
            Skipped = skipped;
        }
    }

    // Re-checks a saved layout against the room as it is now (the scan may have changed, or a different room may be
    // loaded). Items are checked in saved order against the room and the items already accepted; an item that is
    // outside the room, too close to a wall or overlapping an earlier item is skipped (not placed).
    public static class LayoutRevalidator
    {
        public static LayoutFit Fit(RoomFootprint room, IReadOnlyList<PlacedItem> items, float margin)
        {
            if (room == null)
            {
                throw new ArgumentNullException(nameof(room));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var accepted = new List<PlacedItem>(items.Count);
            var skipped = new List<PlacedItem>();
            foreach (PlacedItem item in items)
            {
                if (PlacementValidator.Validate(room, item, accepted, margin).IsValid)
                {
                    accepted.Add(item);
                }
                else
                {
                    skipped.Add(item);
                }
            }

            return new LayoutFit(accepted, skipped);
        }
    }
}
