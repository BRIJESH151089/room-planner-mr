using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class RayVsOrientedBoxTests
    {
        private const float Tolerance = 1e-4f;

        // 2 m wide (x), 1 m deep (z), 1 m tall, centred at the origin.
        private static readonly OrientedRect Box = new OrientedRect(Vector2.zero, 2f, 1f, 0f);

        private static readonly FurnitureDefinition Table = FurnitureCatalog.Get(FurnitureCatalog.TableId); // 1.2 x 0.8 x 0.75
        private static readonly FurnitureDefinition Wardrobe = FurnitureCatalog.Get(FurnitureCatalog.WardrobeId); // 1.0 x 0.6 x 2.0

        [Test]
        public void RayFromSide_HitsAtFrontFace()
        {
            bool hit = RayVsOrientedBox.TryIntersect(new Vector3(-5f, 0.5f, 0f), Vector3.right, Box, 1f, out float distance);

            Assert.IsTrue(hit);
            Assert.AreEqual(4f, distance, Tolerance);
        }

        [Test]
        public void RayFromAbove_HitsTop()
        {
            bool hit = RayVsOrientedBox.TryIntersect(new Vector3(0.5f, 3f, 0.2f), Vector3.down, Box, 1f, out float distance);

            Assert.IsTrue(hit);
            Assert.AreEqual(2f, distance, Tolerance);
        }

        [Test]
        public void RayPassingBeside_Misses()
        {
            Assert.IsFalse(RayVsOrientedBox.TryIntersect(new Vector3(-5f, 0.5f, 0.6f), Vector3.right, Box, 1f, out _));
        }

        [Test]
        public void RayOverTheTop_Misses()
        {
            Assert.IsFalse(RayVsOrientedBox.TryIntersect(new Vector3(-5f, 1.1f, 0f), Vector3.right, Box, 1f, out _));
        }

        [Test]
        public void RayPointingAway_Misses()
        {
            Assert.IsFalse(RayVsOrientedBox.TryIntersect(new Vector3(-5f, 0.5f, 0f), Vector3.left, Box, 1f, out _));
        }

        [Test]
        public void OriginInsideBox_HitsAtZero()
        {
            bool hit = RayVsOrientedBox.TryIntersect(new Vector3(0f, 0.5f, 0f), Vector3.forward, Box, 1f, out float distance);

            Assert.IsTrue(hit);
            Assert.AreEqual(0f, distance, Tolerance);
        }

        [Test]
        public void HitBeyondMaxDistance_Misses()
        {
            Assert.IsFalse(RayVsOrientedBox.TryIntersect(new Vector3(-5f, 0.5f, 0f), Vector3.right, Box, 1f, out _, maxDistance: 3f));
        }

        [Test]
        public void RotatedBox_RayThroughEmptyCornerOfBoundingBox_Misses()
        {
            // A 1x1 box turned 45 degrees is a diamond reaching 0.707 along x and z. The point (0.45, 0.45) is inside
            // its axis-aligned bounding box but outside the diamond (|x| + |z| = 0.9 > 0.707).
            var diamond = new OrientedRect(Vector2.zero, 1f, 1f, 45f);

            Assert.IsFalse(RayVsOrientedBox.TryIntersect(new Vector3(0.45f, 3f, 0.45f), Vector3.down, diamond, 1f, out _));
            Assert.IsTrue(RayVsOrientedBox.TryIntersect(new Vector3(0.3f, 3f, 0.3f), Vector3.down, diamond, 1f, out _));
        }

        [Test]
        public void RotatedBox_RayAlongDiagonal_HitsAtTip()
        {
            var diamond = new OrientedRect(Vector2.zero, 1f, 1f, 45f);

            bool hit = RayVsOrientedBox.TryIntersect(new Vector3(-3f, 0.5f, 0f), Vector3.right, diamond, 1f, out float distance);

            Assert.IsTrue(hit);
            Assert.AreEqual(3f - Mathf.Sqrt(0.5f), distance, Tolerance);
        }

        [Test]
        public void FindNearest_ReturnsClosestOfSeveral()
        {
            var items = new[]
            {
                new PlacedItem(Table, 6f, 0f, 0f, "far"),
                new PlacedItem(Table, 2f, 0f, 0f, "near"),
                new PlacedItem(Table, 4f, 0f, 0f, "middle")
            };

            int index = RayVsOrientedBox.FindNearest(new Vector3(0f, 0.5f, 0f), Vector3.right, default, items, 20f, out float distance);

            Assert.AreEqual(1, index);
            Assert.AreEqual(2f - 0.6f, distance, Tolerance);
        }

        [Test]
        public void FindNearest_TallItemBehindShortOne_HitAboveShortOne()
        {
            // The ray at 1.5 m passes over the 0.75 m table and hits the 2 m wardrobe behind it.
            var items = new[] { new PlacedItem(Table, 2f, 0f, 0f, "t"), new PlacedItem(Wardrobe, 4f, 0f, 0f, "w") };

            int index = RayVsOrientedBox.FindNearest(new Vector3(0f, 1.5f, 0f), Vector3.right, default, items, 20f, out _);

            Assert.AreEqual(1, index);
        }

        [Test]
        public void FindNearest_NothingHit_ReturnsMinusOne()
        {
            var items = new[] { new PlacedItem(Table, 2f, 0f, 0f, "t") };

            Assert.AreEqual(-1, RayVsOrientedBox.FindNearest(new Vector3(0f, 1.5f, 0f), Vector3.left, default, items, 20f, out _));
            Assert.AreEqual(-1, RayVsOrientedBox.FindNearest(new Vector3(0f, 1.5f, 0f), Vector3.right, default, new PlacedItem[0], 20f, out _));
        }

        [Test]
        public void FindNearest_UsesFloorFrame()
        {
            // Room frame turned 90 degrees and raised: room-local +z points along world +x, floor at y = 0.3.
            var frame = new FloorFrame(10f, 0f, 0.3f, 90f);
            var items = new[] { new PlacedItem(Table, 0f, 2f, 0f, "t") }; // world centre (12, 0.3 .. 1.05, 0)

            int hitIndex = RayVsOrientedBox.FindNearest(new Vector3(10f, 0.8f, 0f), Vector3.right, frame, items, 20f, out float distance);
            int missIndex = RayVsOrientedBox.FindNearest(new Vector3(10f, 0.2f, 0f), Vector3.right, frame, items, 20f, out _);

            Assert.AreEqual(0, hitIndex);
            Assert.AreEqual(2f - 0.4f, distance, Tolerance); // table depth 0.8 runs along world x here
            Assert.AreEqual(-1, missIndex, "Below the raised floor.");
        }
    }
}
