using UnityEngine;

namespace RoomPlanner.Core
{
    // Where a RoomFootprint sits in the world: the footprint's (x, z) coordinates are measured from
    // (OriginX, OriginZ) along axes turned by YawDegrees, and the floor is at world y = Height.
    // A pure rotation plus translation, so distances and areas are the same in both frames.
    // default(FloorFrame) is the identity: footprint coordinates are world coordinates and the floor is at y = 0.
    public readonly struct FloorFrame
    {
        public float OriginX { get; }
        public float OriginZ { get; }
        public float Height { get; }
        public float YawDegrees { get; }

        public FloorFrame(float originX, float originZ, float height, float yawDegrees)
        {
            OriginX = originX;
            OriginZ = originZ;
            Height = height;
            YawDegrees = yawDegrees;
        }

        public Vector2 ToLocal(float worldX, float worldZ) =>
            PlanarRotation.InverseRotate(new Vector2(worldX - OriginX, worldZ - OriginZ), YawDegrees);

        public Vector2 ToWorld(Vector2 local) => PlanarRotation.Rotate(local, YawDegrees) + new Vector2(OriginX, OriginZ);

        public float ToWorldYaw(float localYawDegrees) => localYawDegrees + YawDegrees;
    }
}
