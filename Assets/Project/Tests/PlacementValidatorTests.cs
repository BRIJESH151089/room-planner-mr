using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class PlacementValidatorTests
    {
        private static readonly RoomFootprint Room = new RoomFootprint(width: 4f, depth: 4f, margin: 0.5f);

        [Test]
        public void IsWithinRoom_PositionWellInside_ReturnsTrue()
        {
            bool result = PlacementValidator.IsWithinRoom(Room, new Vector2(2f, 2f), new Vector2(1f, 1f));

            Assert.IsTrue(result);
        }

        [Test]
        public void IsWithinRoom_PositionOutsideRoom_ReturnsFalse()
        {
            bool result = PlacementValidator.IsWithinRoom(Room, new Vector2(10f, 10f), new Vector2(1f, 1f));

            Assert.IsFalse(result);
        }

        [Test]
        public void IsWithinRoom_PositionExactlyOnMargin_ReturnsTrue()
        {
            // Room spans 0..4 with a 0.5 margin, so a 1x1 item centered at x=1 touches the margin exactly.
            bool result = PlacementValidator.IsWithinRoom(Room, new Vector2(1f, 1f), new Vector2(1f, 1f));

            Assert.IsTrue(result);
        }

        [Test]
        public void IsWithinRoom_PositionJustInsideMargin_ReturnsFalse()
        {
            bool result = PlacementValidator.IsWithinRoom(Room, new Vector2(0.9f, 1f), new Vector2(1f, 1f));

            Assert.IsFalse(result);
        }

        [Test]
        public void IsWithinRoom_ItemTooLargeForRoom_ReturnsFalse()
        {
            // Available space is 3x3 (room minus margins); a 4x4 item can never fit, at any position.
            bool result = PlacementValidator.IsWithinRoom(Room, new Vector2(2f, 2f), new Vector2(4f, 4f));

            Assert.IsFalse(result);
        }
    }
}
