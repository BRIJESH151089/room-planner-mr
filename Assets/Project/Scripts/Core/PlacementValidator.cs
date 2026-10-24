using UnityEngine;

namespace RoomPlanner.Core
{
    public static class PlacementValidator
    {
        // position and itemSize are in the room's horizontal (x, z) plane; position is the item's center.
        // The item is an axis-aligned rectangle (rotation is not supported yet). It fits when all four of its
        // corners are inside the room with the margin, and no room corner pokes into it (this catches the
        // inner corner of an L-shaped room, where all four item corners can be inside but the item is not).
        public static bool IsWithinRoom(RoomFootprint room, Vector2 position, Vector2 itemSize, float margin)
        {
            float halfWidth = itemSize.x / 2f;
            float halfDepth = itemSize.y / 2f;

            float minX = position.x - halfWidth;
            float maxX = position.x + halfWidth;
            float minZ = position.y - halfDepth;
            float maxZ = position.y + halfDepth;

            if (!room.Contains(minX, minZ, margin) || !room.Contains(maxX, minZ, margin)
                || !room.Contains(minX, maxZ, margin) || !room.Contains(maxX, maxZ, margin))
            {
                return false;
            }

            foreach (Vector2 corner in room.Corners)
            {
                if (corner.x > minX - margin && corner.x < maxX + margin
                    && corner.y > minZ - margin && corner.y < maxZ + margin)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
