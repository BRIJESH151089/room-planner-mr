using System.Collections.Generic;
using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class PlacementHistoryTests
    {
        private static readonly FurnitureDefinition Chair = FurnitureCatalog.Get(FurnitureCatalog.ChairId); // 0.5 x 0.5
        private static readonly FurnitureDefinition Table = FurnitureCatalog.Get(FurnitureCatalog.TableId); // 1.2 x 0.8

        private static readonly RoomFootprint Room = new RoomFootprint(new[]
        {
            new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(4f, 4f), new Vector2(0f, 4f)
        }, height: 2.5f);

        private List<PlacedItem> items;
        private PlacementHistory history;

        [SetUp]
        public void SetUp()
        {
            items = new List<PlacedItem>();
            history = new PlacementHistory(items);
        }

        private static PlacedItem ChairAt(string id, float x, float z) => new PlacedItem(Chair, x, z, 0f, id);

        private string Ids() => string.Join(",", items.ConvertAll(i => i.Id));

        [Test]
        public void Place_ThenUndo_ThenRedo()
        {
            history.Place(ChairAt("a", 1f, 1f));
            Assert.AreEqual("a", Ids());

            Assert.IsTrue(history.TryUndo(out LayoutChange undone));
            Assert.AreEqual(LayoutChangeKind.Removed, undone.Kind);
            Assert.AreEqual("", Ids());

            Assert.IsTrue(history.TryRedo(out LayoutChange redone));
            Assert.AreEqual(LayoutChangeKind.Added, redone.Kind);
            Assert.AreEqual("a", Ids());
        }

        [Test]
        public void Undo_RevertsInReverseOrder_RedoReappliesInOriginalOrder()
        {
            history.Place(ChairAt("a", 1f, 1f));
            history.Place(ChairAt("b", 2f, 1f));
            history.Place(ChairAt("c", 3f, 1f));

            history.TryUndo(out LayoutChange first);
            history.TryUndo(out LayoutChange second);
            Assert.AreEqual("c", first.Item.Id);
            Assert.AreEqual("b", second.Item.Id);
            Assert.AreEqual("a", Ids());

            history.TryRedo(out LayoutChange third);
            history.TryRedo(out LayoutChange fourth);
            Assert.AreEqual("b", third.Item.Id);
            Assert.AreEqual("c", fourth.Item.Id);
            Assert.AreEqual("a,b,c", Ids());
        }

        [Test]
        public void NewAction_ClearsRedo()
        {
            history.Place(ChairAt("a", 1f, 1f));
            history.Place(ChairAt("b", 2f, 1f));
            history.TryUndo(out _);
            Assert.IsTrue(history.CanRedo);

            history.Place(ChairAt("c", 3f, 1f));

            Assert.IsFalse(history.CanRedo);
            Assert.IsFalse(history.TryRedo(out _));
            Assert.AreEqual("a,c", Ids());
        }

        [Test]
        public void History_KeepsAtMostFiftyEntries_DroppingTheOldest()
        {
            for (int i = 0; i < 60; i++)
            {
                history.Place(ChairAt("id" + i, 0.5f, 0.5f));
            }

            Assert.AreEqual(PlacementHistory.MaxEntries, history.UndoCount);

            int undone = 0;
            while (history.TryUndo(out _))
            {
                undone++;
            }

            Assert.AreEqual(50, undone);
            Assert.AreEqual(10, items.Count, "The first 10 placements are beyond the undo limit.");
            Assert.AreEqual("id9", items[9].Id);
        }

        [Test]
        public void Move_UndoAndRedo_RestoreExactPoses()
        {
            history.Place(new PlacedItem(Chair, 1.05f, 1.15f, 30f, "a"));
            history.Move("a", 2.35f, 3.05f, 285f);

            Assert.IsTrue(history.TryUndo(out LayoutChange undone));
            Assert.AreEqual(LayoutChangeKind.Moved, undone.Kind);
            Assert.AreEqual(1.05f, items[0].X);
            Assert.AreEqual(1.15f, items[0].Z);
            Assert.AreEqual(30f, items[0].YawDegrees);

            Assert.IsTrue(history.TryRedo(out _));
            Assert.AreEqual(2.35f, items[0].X);
            Assert.AreEqual(3.05f, items[0].Z);
            Assert.AreEqual(285f, items[0].YawDegrees);
            Assert.AreEqual("a", items[0].Id);
        }

        [Test]
        public void Remove_ThenUndo_RestoresSameIdAtSameIndex()
        {
            history.Place(ChairAt("a", 1f, 1f));
            history.Place(new PlacedItem(Table, 2.5f, 2.5f, 45f, "b"));
            history.Place(ChairAt("c", 3f, 1f));

            LayoutChange removed = history.Remove("b");
            Assert.AreEqual(LayoutChangeKind.Removed, removed.Kind);
            Assert.AreEqual("a,c", Ids());

            Assert.IsTrue(history.TryUndo(out LayoutChange restored));
            Assert.AreEqual(LayoutChangeKind.Added, restored.Kind);
            Assert.AreEqual("a,b,c", Ids());
            Assert.AreSame(Table, items[1].Definition);
            Assert.AreEqual(2.5f, items[1].X);
            Assert.AreEqual(45f, items[1].YawDegrees);
        }

        [Test]
        public void UndoOrRedo_WithNothingRecorded_ReturnsFalse()
        {
            Assert.IsFalse(history.TryUndo(out _));
            Assert.IsFalse(history.TryRedo(out _));
        }

        [Test]
        public void Commands_RequirePlacedItemsWithIds()
        {
            Assert.Throws<System.ArgumentException>(() => history.Place(new PlacedItem(Chair, 1f, 1f, 0f)));
            Assert.Throws<System.ArgumentException>(() => history.Remove(""));
            Assert.Throws<System.InvalidOperationException>(() => history.Move("missing", 1f, 1f, 0f));
        }

        [Test]
        public void MoveValidation_ExcludesTheMovedItemItself()
        {
            history.Place(new PlacedItem(Table, 2f, 2f, 0f, "t"));

            // Nudge the table 10 cm: it overlaps its own old spot, which must not count.
            PlacedItem candidate = items[0].MovedTo(2.1f, 2f, 0f);
            PlacementResult result = PlacementValidator.Validate(Room, candidate, items, PlacementValidator.WallMargin);

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void MoveValidation_StillChecksOtherItems()
        {
            history.Place(new PlacedItem(Table, 2f, 2f, 0f, "t"));
            history.Place(ChairAt("c", 3.2f, 2f));

            PlacedItem candidate = items[0].MovedTo(2.4f, 2f, 0f); // reaches x = 3.0, into the chair (2.95..3.45)
            PlacementResult result = PlacementValidator.Validate(Room, candidate, items, PlacementValidator.WallMargin);

            Assert.AreEqual(PlacementReason.OverlapsItem, result.Reason);
            Assert.AreEqual(1, result.OverlappingItemIndex);
        }

        [Test]
        public void MoveValidation_IntoWall_IsRejected()
        {
            history.Place(new PlacedItem(Table, 2f, 2f, 0f, "t"));

            PlacedItem candidate = items[0].MovedTo(3.7f, 2f, 0f); // reaches x = 4.3, through the wall at x = 4

            Assert.AreEqual(PlacementReason.OutsideRoom,
                PlacementValidator.Validate(Room, candidate, items, PlacementValidator.WallMargin).Reason);
        }
    }
}
