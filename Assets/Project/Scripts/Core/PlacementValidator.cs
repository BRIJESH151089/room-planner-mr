using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoomPlanner.Core
{
    public static class PlacementValidator
    {
        // Gap kept between furniture and walls, in metres.
        public const float WallMargin = 0.05f;

        // position and itemSize are in the room's horizontal (x, z) plane; position is the item's center.
        // The item is an axis-aligned rectangle (yaw 0).
        public static bool IsWithinRoom(RoomFootprint room, Vector2 position, Vector2 itemSize, float margin)
        {
            return CheckRoom(room, new OrientedRect(position, itemSize.x, itemSize.y, 0f), margin) == PlacementReason.Valid;
        }

        // Checks a candidate against the room and the items already placed, in that order:
        // OutsideRoom wins over TooCloseToWall, which wins over OverlapsItem. Does not allocate.
        public static PlacementResult Validate(RoomFootprint room, in PlacedItem candidate, IReadOnlyList<PlacedItem> placedItems, float margin)
        {
            if (placedItems == null)
            {
                throw new ArgumentNullException(nameof(placedItems));
            }

            OrientedRect footprint = candidate.Footprint;
            PlacementReason roomCheck = CheckRoom(room, footprint, margin);
            if (roomCheck != PlacementReason.Valid)
            {
                return new PlacementResult(roomCheck);
            }

            for (int i = 0; i < placedItems.Count; i++)
            {
                if (OverlapChecker.Overlaps(footprint, placedItems[i].Footprint))
                {
                    return new PlacementResult(PlacementReason.OverlapsItem, i);
                }
            }

            return new PlacementResult(PlacementReason.Valid);
        }

        // OutsideRoom: a corner is outside the outline, or a wall cuts into the item (this catches the inner
        // corner of an L-shaped room, where all four item corners can be inside but the item is not).
        // TooCloseToWall: the exact shortest distance between the item and some wall is less than `margin`.
        // Exactly at the margin is allowed.
        private static PlacementReason CheckRoom(RoomFootprint room, in OrientedRect rect, float margin)
        {
            if (room == null)
            {
                throw new ArgumentNullException(nameof(room));
            }

            if (margin < 0f)
            {
                throw new ArgumentException("Margin must not be negative.", nameof(margin));
            }

            if (room.IsEmpty)
            {
                return PlacementReason.OutsideRoom;
            }

            Span<Vector2> corners = stackalloc Vector2[4];
            rect.GetCorners(corners);
            for (int i = 0; i < corners.Length; i++)
            {
                if (!room.Contains(corners[i].x, corners[i].y))
                {
                    return PlacementReason.OutsideRoom;
                }
            }

            bool tooClose = false;
            IReadOnlyList<Vector2> walls = room.Corners;
            for (int i = 0; i < walls.Count; i++)
            {
                Vector2 start = walls[i];
                Vector2 end = walls[(i + 1) % walls.Count];
                if (OverlapChecker.SegmentOverlaps(start, end, rect))
                {
                    return PlacementReason.OutsideRoom;
                }

                if (margin > 0f && PlanarGeometry.SegmentToRectDistance(start, end, rect) < margin - OverlapChecker.Epsilon)
                {
                    tooClose = true;
                }
            }

            return tooClose ? PlacementReason.TooCloseToWall : PlacementReason.Valid;
        }
    }
}
