using System.Collections.Generic;
using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class LayoutLoadTests
    {
        private static readonly FurnitureDefinition Table = FurnitureCatalog.Get(FurnitureCatalog.TableId); // 1.2 x 0.8
        private static readonly FurnitureDefinition Chair = FurnitureCatalog.Get(FurnitureCatalog.ChairId); // 0.5 x 0.5

        private static readonly RoomFootprint Room = new RoomFootprint(new[]
        {
            new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(4f, 4f), new Vector2(0f, 4f)
        }, height: 2.5f);

        private static readonly RoomFootprint SmallRoom = new RoomFootprint(new[]
        {
            new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(2f, 2f), new Vector2(0f, 2f)
        }, height: 2.5f);

        [Test]
        public void Fit_AllItemsThatFit_AreAcceptedInOrder()
        {
            var items = new[] { new PlacedItem(Table, 1f, 1f, 0f, "a"), new PlacedItem(Chair, 3f, 3f, 0f, "b") };

            LayoutFit fit = LayoutRevalidator.Fit(Room, items, PlacementValidator.WallMargin);

            Assert.AreEqual(2, fit.Accepted.Count);
            Assert.AreEqual("a", fit.Accepted[0].Id);
            Assert.AreEqual("b", fit.Accepted[1].Id);
            Assert.AreEqual(0, fit.Skipped.Count);
        }

        [Test]
        public void Fit_SmallerRoom_SkipsItemsOutsideIt()
        {
            // Saved in the 4x4 room; loaded into a 2x2 room where the chair at (3, 3) is outside.
            var items = new[] { new PlacedItem(Chair, 1f, 1f, 0f, "inside"), new PlacedItem(Chair, 3f, 3f, 0f, "outside") };

            LayoutFit fit = LayoutRevalidator.Fit(SmallRoom, items, PlacementValidator.WallMargin);

            Assert.AreEqual(1, fit.Accepted.Count);
            Assert.AreEqual("inside", fit.Accepted[0].Id);
            Assert.AreEqual("outside", fit.Skipped[0].Id);
        }

        [Test]
        public void Fit_OverlappingItems_KeepsTheFirstSkipsTheLater()
        {
            var items = new[] { new PlacedItem(Table, 2f, 2f, 0f, "first"), new PlacedItem(Chair, 2.2f, 2f, 0f, "second") };

            LayoutFit fit = LayoutRevalidator.Fit(Room, items, PlacementValidator.WallMargin);

            Assert.AreEqual("first", fit.Accepted[0].Id);
            Assert.AreEqual("second", fit.Skipped[0].Id);
        }

        [Test]
        public void Fit_ItemTooCloseToWall_IsSkipped()
        {
            LayoutFit fit = LayoutRevalidator.Fit(Room, new[] { new PlacedItem(Chair, 0.27f, 2f, 0f, "a") }, PlacementValidator.WallMargin);

            Assert.AreEqual(0, fit.Accepted.Count);
            Assert.AreEqual(1, fit.Skipped.Count);
        }

        [Test]
        public void Fit_EmptyRoom_SkipsEverything()
        {
            LayoutFit fit = LayoutRevalidator.Fit(RoomFootprint.Empty, new[] { new PlacedItem(Chair, 1f, 1f, 0f, "a") }, PlacementValidator.WallMargin);

            Assert.AreEqual(0, fit.Accepted.Count);
            Assert.AreEqual(1, fit.Skipped.Count);
        }

        [Test]
        public void History_Clear_ForgetsUndoAndRedoButKeepsItems()
        {
            var items = new List<PlacedItem>();
            var history = new PlacementHistory(items);
            history.Place(new PlacedItem(Chair, 1f, 1f, 0f, "a"));
            history.Place(new PlacedItem(Chair, 2f, 1f, 0f, "b"));
            history.TryUndo(out _);

            history.Clear();

            Assert.IsFalse(history.CanUndo);
            Assert.IsFalse(history.CanRedo);
            Assert.AreEqual(1, items.Count);
        }

        [Test]
        public void StatusFormatter_Loaded_MentionsSkippedAndUnreadableOnlyWhenPresent()
        {
            Assert.AreEqual("Loaded 3 items.", LayoutStatusFormatter.Loaded(3, 0, 0, 0));
            Assert.AreEqual("Loaded 1 item.", LayoutStatusFormatter.Loaded(1, 0, 0, 0));
            Assert.AreEqual("Loaded 2 items. 1 did not fit this room.", LayoutStatusFormatter.Loaded(2, 1, 0, 0));
            Assert.AreEqual("Loaded 0 items. 2 did not fit this room. 1 not in the catalog. 2 unreadable.", LayoutStatusFormatter.Loaded(0, 2, 1, 2));
        }

        [Test]
        public void StatusFormatter_Saved()
        {
            Assert.AreEqual("Saved 4 items.", LayoutStatusFormatter.Saved(4));
            Assert.AreEqual("Saved 1 item.", LayoutStatusFormatter.Saved(1));
        }
    }
}
