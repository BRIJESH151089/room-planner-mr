using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class PlacementValidatorTests
    {
        private const float Margin = 0.5f;

        private static readonly RoomFootprint Room = new RoomFootprint(new[]
        {
            new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(4f, 4f), new Vector2(0f, 4f)
        }, height: 2.5f);

        [Test]
        public void IsWithinRoom_PositionWellInside_ReturnsTrue()
        {
            bool result = PlacementValidator.IsWithinRoom(Room, new Vector2(2f, 2f), new Vector2(1f, 1f), Margin);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsWithinRoom_PositionOutsideRoom_ReturnsFalse()
        {
            bool result = PlacementValidator.IsWithinRoom(Room, new Vector2(10f, 10f), new Vector2(1f, 1f), Margin);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsWithinRoom_PositionExactlyOnMargin_ReturnsTrue()
        {
            // Room spans 0..4 with a 0.5 margin, so a 1x1 item centered at x=1 touches the margin exactly.
            bool result = PlacementValidator.IsWithinRoom(Room, new Vector2(1f, 1f), new Vector2(1f, 1f), Margin);

            Assert.IsTrue(result);
        }

        [Test]
        public void IsWithinRoom_PositionJustInsideMargin_ReturnsFalse()
        {
            bool result = PlacementValidator.IsWithinRoom(Room, new Vector2(0.9f, 1f), new Vector2(1f, 1f), Margin);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsWithinRoom_ItemTooLargeForRoom_ReturnsFalse()
        {
            // Available space is 3x3 (room minus margins); a 4x4 item can never fit, at any position.
            bool result = PlacementValidator.IsWithinRoom(Room, new Vector2(2f, 2f), new Vector2(4f, 4f), Margin);

            Assert.IsFalse(result);
        }

        [Test]
        public void IsWithinRoom_ItemSpanningNotchOfUShapedRoom_ReturnsFalse()
        {
            // 6x4 room with a 2x2 notch cut into the top edge between x=2 and x=4.
            var uRoom = new RoomFootprint(new[]
            {
                new Vector2(0f, 0f), new Vector2(6f, 0f), new Vector2(6f, 4f), new Vector2(4f, 4f),
                new Vector2(4f, 2f), new Vector2(2f, 2f), new Vector2(2f, 4f), new Vector2(0f, 4f)
            }, height: 2.5f);

            // Item spans (1,1)..(5,3): all four of its corners are inside the room, but the notch cuts through it.
            bool result = PlacementValidator.IsWithinRoom(uRoom, new Vector2(3f, 2f), new Vector2(4f, 2f), margin: 0f);

            Assert.IsFalse(result);
        }
    }
}
