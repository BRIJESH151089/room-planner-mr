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

        // --- Validate: room footprint + already placed items ---

        private static readonly FurnitureDefinition Table = FurnitureCatalog.Get(FurnitureCatalog.TableId); // 1.2 x 0.8
        private static readonly FurnitureDefinition Chair = FurnitureCatalog.Get(FurnitureCatalog.ChairId); // 0.5 x 0.5
        private static readonly PlacedItem[] NoItems = new PlacedItem[0];

        // 4x4 square with the 2x2 top-right quarter (x 2..4, z 2..4) cut out.
        private static readonly RoomFootprint LRoom = new RoomFootprint(new[]
        {
            new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(4f, 2f),
            new Vector2(2f, 2f), new Vector2(2f, 4f), new Vector2(0f, 4f)
        }, height: 2.5f);

        private static PlacementReason Check(RoomFootprint room, PlacedItem candidate, params PlacedItem[] placed) =>
            PlacementValidator.Validate(room, candidate, placed, PlacementValidator.WallMargin).Reason;

        [Test]
        public void Validate_ItemInMiddleOfEmptyRoom_IsValid()
        {
            PlacementResult result = PlacementValidator.Validate(Room, new PlacedItem(Table, 2f, 2f, 0f), NoItems, PlacementValidator.WallMargin);

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(-1, result.OverlappingItemIndex);
        }

        [Test]
        public void Validate_ItemPartlyThroughWall_IsOutsideRoom()
        {
            Assert.AreEqual(PlacementReason.OutsideRoom, Check(Room, new PlacedItem(Table, 0.3f, 2f, 0f)));
        }

        [Test]
        public void Validate_ItemCloserToWallThanMargin_IsTooCloseToWall()
        {
            // Table half width 0.6: centre at x = 0.62 leaves a 2 cm gap, less than the 5 cm margin.
            Assert.AreEqual(PlacementReason.TooCloseToWall, Check(Room, new PlacedItem(Table, 0.62f, 2f, 0f)));
        }

        [Test]
        public void Validate_ItemExactlyAtMarginFromWall_IsValid()
        {
            Assert.AreEqual(PlacementReason.Valid, Check(Room, new PlacedItem(Table, 0.65f, 2f, 0f)));
        }

        [Test]
        public void Validate_ItemInCorner_RespectsBothWalls()
        {
            Assert.AreEqual(PlacementReason.Valid, Check(Room, new PlacedItem(Chair, 0.3f, 0.3f, 0f)));
            Assert.AreEqual(PlacementReason.TooCloseToWall, Check(Room, new PlacedItem(Chair, 0.3f, 0.27f, 0f)));
        }

        [Test]
        public void Validate_OverlappingPlacedItem_ReportsOverlapAndIndex()
        {
            var placed = new[] { new PlacedItem(Chair, 1f, 1f, 0f), new PlacedItem(Table, 2f, 2f, 0f) };

            PlacementResult result = PlacementValidator.Validate(Room, new PlacedItem(Chair, 2.5f, 2.2f, 0f), placed, PlacementValidator.WallMargin);

            Assert.AreEqual(PlacementReason.OverlapsItem, result.Reason);
            Assert.AreEqual(1, result.OverlappingItemIndex);
        }

        [Test]
        public void Validate_TouchingPlacedItem_IsValid()
        {
            // Table spans x 1.4..2.6; the chair (0.5 wide) centred at 2.85 spans 2.6..3.1.
            Assert.AreEqual(PlacementReason.Valid, Check(Room, new PlacedItem(Chair, 2.85f, 2f, 0f), new PlacedItem(Table, 2f, 2f, 0f)));
        }

        [Test]
        public void Validate_WallBeatsOverlap_WhenBothApply()
        {
            Assert.AreEqual(PlacementReason.OutsideRoom, Check(Room, new PlacedItem(Table, 0.3f, 2f, 0f), new PlacedItem(Table, 0.9f, 2f, 0f)));
        }

        [Test]
        public void Validate_LShapedRoom_ItemInCutOut_IsOutsideRoom()
        {
            Assert.AreEqual(PlacementReason.OutsideRoom, Check(LRoom, new PlacedItem(Chair, 3f, 3f, 0f)));
        }

        [Test]
        public void Validate_LShapedRoom_ItemInEachArm_IsValid()
        {
            Assert.AreEqual(PlacementReason.Valid, Check(LRoom, new PlacedItem(Table, 3f, 1f, 0f)));
            Assert.AreEqual(PlacementReason.Valid, Check(LRoom, new PlacedItem(Table, 1f, 3f, 90f)));
        }

        [Test]
        public void Validate_LShapedRoom_RotatedItemOverInnerCorner_IsOutsideRoom()
        {
            // A table turned 45 degrees and centred just inside the inner corner (2, 2): all four corners
            // lie inside the L, but the two inner walls cut through it.
            Assert.AreEqual(PlacementReason.OutsideRoom, Check(LRoom, new PlacedItem(Table, 1.75f, 1.75f, 45f)));
        }

        // A chair turned 45 degrees points one corner at the wall z = 0; that corner is 0.354 m from the centre.
        // The margin is measured as the true distance, not by growing the rectangle (which would push the
        // corner out by margin * 1.41 and reject a 6 cm gap).
        [Test]
        public void Validate_RotatedCornerSixCentimetresFromWall_IsValid()
        {
            float cornerReach = 0.25f * Mathf.Sqrt(2f);

            Assert.AreEqual(PlacementReason.Valid, Check(Room, new PlacedItem(Chair, 2f, cornerReach + 0.06f, 45f)));
        }

        [Test]
        public void Validate_RotatedCornerFourCentimetresFromWall_IsTooCloseToWall()
        {
            float cornerReach = 0.25f * Mathf.Sqrt(2f);

            Assert.AreEqual(PlacementReason.TooCloseToWall, Check(Room, new PlacedItem(Chair, 2f, cornerReach + 0.04f, 45f)));
        }

        [Test]
        public void Validate_SlantedWall_UsesTrueDistance()
        {
            // Bottom wall slopes from (0, 0) down to (4, -0.4). The chair's lower-left corner (0.3, 0.025)
            // is 5.47 cm from it, outside the 5 cm margin.
            var slanted = new RoomFootprint(new[]
            {
                new Vector2(0f, 0f), new Vector2(4f, -0.4f), new Vector2(4f, 4f), new Vector2(0f, 4f)
            }, height: 2.5f);

            Assert.AreEqual(PlacementReason.Valid, Check(slanted, new PlacedItem(Chair, 0.55f, 0.275f, 0f)));
            Assert.AreEqual(PlacementReason.TooCloseToWall, Check(slanted, new PlacedItem(Chair, 0.55f, 0.265f, 0f)));
        }

        [Test]
        public void Validate_EmptyRoom_IsOutsideRoom()
        {
            Assert.AreEqual(PlacementReason.OutsideRoom, Check(RoomFootprint.Empty, new PlacedItem(Chair, 0f, 0f, 0f)));
        }
    }
}
