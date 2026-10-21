using UnityEngine;

namespace RoomPlanner.Core
{
    public static class PlacementValidator
    {
        // position and itemSize are in the room's horizontal (x, z) plane; position is the item's center.
        public static bool IsWithinRoom(RoomFootprint room, Vector2 position, Vector2 itemSize)
        {
            float halfWidth = itemSize.x / 2f;
            float halfDepth = itemSize.y / 2f;

            float minX = room.Margin + halfWidth;
            float maxX = room.Width - room.Margin - halfWidth;
            float minZ = room.Margin + halfDepth;
            float maxZ = room.Depth - room.Margin - halfDepth;

            if (minX > maxX || minZ > maxZ)
            {
                return false;
            }

            return position.x >= minX && position.x <= maxX
                && position.y >= minZ && position.y <= maxZ;
        }
    }
}
