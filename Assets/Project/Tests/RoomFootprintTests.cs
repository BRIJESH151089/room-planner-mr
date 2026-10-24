using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class RoomFootprintTests
    {
        private const float Tolerance = 1e-4f;

        // 4 m wide (x), 3 m deep (z).
        private static readonly Vector2[] RectangleCorners =
        {
            new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(4f, 3f), new Vector2(0f, 3f)
        };

        // 4x4 square with the 2x2 top-right quarter cut out.
        private static readonly Vector2[] LShapeCorners =
        {
            new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(4f, 2f),
            new Vector2(2f, 2f), new Vector2(2f, 4f), new Vector2(0f, 4f)
        };

        [Test]
        public void RectangularRoom_ExposesSizeAreaAndWalls()
        {
            var room = new RoomFootprint(RectangleCorners, height: 2.5f);

            Assert.AreEqual(4f, room.Width, Tolerance);
            Assert.AreEqual(3f, room.Depth, Tolerance);
            Assert.AreEqual(2.5f, room.Height, Tolerance);
            Assert.AreEqual(12f, room.Area, Tolerance);
            Assert.AreEqual(4, room.WallCount);
            Assert.IsFalse(room.IsEmpty);
        }

        [Test]
        public void RectangularRoom_WorksWithOffsetClockwiseCornersAndClosingPoint()
        {
            // Same 4x3 room, offset from the origin, listed clockwise, with the first corner repeated at the end.
            var room = new RoomFootprint(new[]
            {
                new Vector2(-2f, -1f), new Vector2(-2f, 2f), new Vector2(2f, 2f), new Vector2(2f, -1f), new Vector2(-2f, -1f)
            }, height: 2.5f);

            Assert.AreEqual(4f, room.Width, Tolerance);
            Assert.AreEqual(3f, room.Depth, Tolerance);
            Assert.AreEqual(12f, room.Area, Tolerance);
            Assert.AreEqual(4, room.WallCount);
            Assert.IsTrue(room.Contains(0f, 0f));
        }

        [Test]
        public void LShapedRoom_AreaExcludesCutOutButBoundingBoxDoesNot()
        {
            var room = new RoomFootprint(LShapeCorners, height: 2.4f);

            Assert.AreEqual(4f, room.Width, Tolerance);
            Assert.AreEqual(4f, room.Depth, Tolerance);
            Assert.AreEqual(12f, room.Area, Tolerance);
            Assert.AreEqual(6, room.WallCount);
        }

        [Test]
        public void LShapedRoom_ContainsOnlyPointsInTheRealPolygon()
        {
            var room = new RoomFootprint(LShapeCorners, height: 2.4f);

            Assert.IsTrue(room.Contains(1f, 1f));
            Assert.IsTrue(room.Contains(3f, 1f));
            Assert.IsTrue(room.Contains(1f, 3f));
            Assert.IsFalse(room.Contains(3f, 3f), "The cut-out corner is inside the bounding box but not the room.");
        }

        [Test]
        public void ZeroWalls_IsEmptyAndContainsNothing()
        {
            var room = new RoomFootprint(new Vector2[0], height: 0f);

            Assert.IsTrue(room.IsEmpty);
            Assert.AreEqual(0, room.WallCount);
            Assert.AreEqual(0f, room.Width);
            Assert.AreEqual(0f, room.Depth);
            Assert.AreEqual(0f, room.Area);
            Assert.IsFalse(room.Contains(0f, 0f));
            Assert.IsTrue(RoomFootprint.Empty.IsEmpty);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void FewerThanThreeCorners_Throws(int cornerCount)
        {
            var corners = new Vector2[cornerCount];
            for (int i = 0; i < cornerCount; i++)
            {
                corners[i] = new Vector2(i, i);
            }

            Assert.Throws<System.ArgumentException>(() => new RoomFootprint(corners, 2.5f));
        }

        [Test]
        public void NullCornersOrNegativeHeight_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new RoomFootprint(null, 2.5f));
            Assert.Throws<System.ArgumentException>(() => new RoomFootprint(RectangleCorners, -1f));
        }

        [Test]
        public void Contains_PointExactlyAtMarginFromWall_ReturnsTrue()
        {
            var room = new RoomFootprint(RectangleCorners, height: 2.5f);

            Assert.IsTrue(room.Contains(0.5f, 1.5f, margin: 0.5f));
        }

        [Test]
        public void Contains_PointCloserToWallThanMargin_ReturnsFalse()
        {
            var room = new RoomFootprint(RectangleCorners, height: 2.5f);

            Assert.IsFalse(room.Contains(0.49f, 1.5f, margin: 0.5f));
        }

        [Test]
        public void Contains_PointOnWallWithZeroMargin_ReturnsTrue()
        {
            var room = new RoomFootprint(RectangleCorners, height: 2.5f);

            Assert.IsTrue(room.Contains(0f, 1.5f));
        }

        [Test]
        public void Contains_PointOutsideRoom_ReturnsFalse()
        {
            var room = new RoomFootprint(RectangleCorners, height: 2.5f);

            Assert.IsFalse(room.Contains(-0.1f, 1.5f));
            Assert.IsFalse(room.Contains(5f, 5f));
        }

        [Test]
        public void Contains_NegativeMargin_Throws()
        {
            var room = new RoomFootprint(RectangleCorners, height: 2.5f);

            Assert.Throws<System.ArgumentException>(() => room.Contains(1f, 1f, -0.1f));
        }
    }
}
