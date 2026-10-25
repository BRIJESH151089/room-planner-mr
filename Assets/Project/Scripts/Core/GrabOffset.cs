using UnityEngine;

namespace RoomPlanner.Core
{
    // Keeps the spot where an item was grabbed under the pointer while it is moved, so a long item (bed, wardrobe)
    // does not snap its centre to the pointer on pickup. All points are on the floor plane in the room footprint's frame.
    //   1. ChooseGrabPoint: where on the item it was grabbed.
    //   2. FromGrabPoint: offset from that point to the item's centre, in the item's own yaw frame.
    //   3. CentreFromPointer: the item's new centre for a new pointer position and the current yaw, so the offset
    //      turns with the item when it is rotated during the move.
    public static class GrabOffset
    {
        // While moving, the pointer is the controller ray's hit on the floor. If that hit at pickup lies on the item's
        // footprint (pointing at or through a low item) it is the grab point, which makes pickup jump-free. Otherwise
        // (e.g. aiming at the top of a tall item, where the floor hit is beyond it) use where the ray hit the item's box.
        public static Vector2 ChooseGrabPoint(in PlacedItem item, bool hasFloorHit, Vector2 floorHit, Vector2 boxHit)
        {
            return hasFloorHit && IsOnFootprint(item, floorHit) ? floorHit : boxHit;
        }

        // Offset from the grab point to the item's centre, in the item's yaw frame. The grab point is clamped onto the
        // footprint first, so the offset is never longer than half the item's diagonal.
        public static Vector2 FromGrabPoint(in PlacedItem item, Vector2 grabPoint)
        {
            Vector2 local = PlanarRotation.InverseRotate(grabPoint - new Vector2(item.X, item.Z), item.YawDegrees);
            float halfWidth = item.Definition.Width / 2f;
            float halfDepth = item.Definition.Depth / 2f;
            local.x = Mathf.Clamp(local.x, -halfWidth, halfWidth);
            local.y = Mathf.Clamp(local.y, -halfDepth, halfDepth);
            return -local;
        }

        public static Vector2 CentreFromPointer(Vector2 pointer, Vector2 localOffset, float yawDegrees)
        {
            return pointer + PlanarRotation.Rotate(localOffset, yawDegrees);
        }

        public static bool IsOnFootprint(in PlacedItem item, Vector2 point)
        {
            Vector2 local = PlanarRotation.InverseRotate(point - new Vector2(item.X, item.Z), item.YawDegrees);
            return Mathf.Abs(local.x) <= item.Definition.Width / 2f && Mathf.Abs(local.y) <= item.Definition.Depth / 2f;
        }
    }
}
