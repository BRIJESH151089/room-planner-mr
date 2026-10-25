using System.Collections.Generic;
using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class LayoutSlotsTests
    {
        // In-memory stand-in for the files on the headset.
        private sealed class MemoryStorage : ILayoutStorage
        {
            public readonly Dictionary<LayoutSlot, string> Slots = new Dictionary<LayoutSlot, string>();
            public bool FailWrites;

            public bool Exists(LayoutSlot slot) => Slots.ContainsKey(slot);

            public bool TryRead(LayoutSlot slot, out string json) => Slots.TryGetValue(slot, out json);

            public bool Write(LayoutSlot slot, string json)
            {
                if (FailWrites)
                {
                    return false;
                }

                Slots[slot] = json;
                return true;
            }

            public bool Delete(LayoutSlot slot) => Slots.Remove(slot);
        }

        private static readonly FurnitureDefinition Chair = FurnitureCatalog.Get(FurnitureCatalog.ChairId);
        private static readonly FurnitureDefinition Table = FurnitureCatalog.Get(FurnitureCatalog.TableId);

        private static readonly RoomFootprint Room = new RoomFootprint(new[]
        {
            new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(4f, 4f), new Vector2(0f, 4f)
        }, height: 2.5f);

        private MemoryStorage storage;
        private LayoutSlots slots;

        [SetUp]
        public void SetUp()
        {
            storage = new MemoryStorage();
            slots = new LayoutSlots(storage);
        }

        private static string Ids(LayoutRestore restore)
        {
            var ids = new List<string>();
            foreach (PlacedItem item in restore.Fit.Accepted)
            {
                ids.Add(item.Id);
            }

            return string.Join(",", ids);
        }

        [Test]
        public void SessionAndCheckpoint_AreIndependent()
        {
            slots.SaveSession(new[] { new PlacedItem(Chair, 1f, 1f, 0f, "session") });
            slots.SaveCheckpoint(new[] { new PlacedItem(Table, 2f, 2f, 0f, "checkpoint") });

            Assert.AreEqual("session", Ids(slots.RestoreSession(Room)));
            Assert.AreEqual("checkpoint", Ids(slots.RestoreCheckpoint(Room)));
        }

        [Test]
        public void Autosave_DoesNotOverwriteCheckpoint()
        {
            slots.SaveCheckpoint(new[] { new PlacedItem(Chair, 1f, 1f, 0f, "saved") });

            slots.SaveSession(new[] { new PlacedItem(Chair, 3f, 3f, 0f, "moved-after-save") });

            Assert.AreEqual("saved", Ids(slots.RestoreCheckpoint(Room)));
        }

        [Test]
        public void Load_ReturnsCheckpointExactly_NotTheCurrentSession()
        {
            var saved = new PlacedItem(Table, 1.25f, 2.35f, 30f, "t");
            slots.SaveCheckpoint(new[] { saved });
            slots.SaveSession(new[] { saved.MovedTo(3f, 1f, 90f) }); // user moved it after saving

            LayoutRestore restore = slots.RestoreCheckpoint(Room);

            Assert.AreEqual(LayoutRestoreOutcome.Restored, restore.Outcome);
            PlacedItem back = restore.Fit.Accepted[0];
            Assert.AreEqual("t", back.Id);
            Assert.AreEqual(1.25f, back.X);
            Assert.AreEqual(2.35f, back.Z);
            Assert.AreEqual(30f, back.YawDegrees);
        }

        [Test]
        public void ClearSession_EmptiesSession_ButKeepsCheckpoint()
        {
            slots.SaveSession(new[] { new PlacedItem(Chair, 1f, 1f, 0f, "a") });
            slots.SaveCheckpoint(new[] { new PlacedItem(Chair, 1f, 1f, 0f, "a") });

            Assert.IsTrue(slots.ClearSession());

            LayoutRestore session = slots.RestoreSession(Room);
            Assert.AreEqual(LayoutRestoreOutcome.Restored, session.Outcome);
            Assert.AreEqual(0, session.Fit.Accepted.Count);
            Assert.IsTrue(slots.HasCheckpoint);
            Assert.AreEqual("a", Ids(slots.RestoreCheckpoint(Room)));
        }

        [Test]
        public void Load_WithNoCheckpoint_ReturnsNoneSavedAndNoItems()
        {
            slots.SaveSession(new[] { new PlacedItem(Chair, 1f, 1f, 0f, "a") });

            LayoutRestore restore = slots.RestoreCheckpoint(Room);

            Assert.IsFalse(slots.HasCheckpoint);
            Assert.AreEqual(LayoutRestoreOutcome.NoneSaved, restore.Outcome);
            Assert.AreEqual(0, restore.Fit.Accepted.Count);
            Assert.AreEqual(0, restore.Fit.Skipped.Count);
        }

        [Test]
        public void Resume_WithNoSession_ReturnsNoneSaved()
        {
            Assert.AreEqual(LayoutRestoreOutcome.NoneSaved, slots.RestoreSession(Room).Outcome);
        }

        [Test]
        public void Load_RevalidatesAgainstTheCurrentRoom()
        {
            slots.SaveCheckpoint(new[]
            {
                new PlacedItem(Chair, 1f, 1f, 0f, "fits"),
                new PlacedItem(Chair, 3.5f, 3.5f, 0f, "outside-small-room")
            });
            var smallRoom = new RoomFootprint(new[] { new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(2f, 2f), new Vector2(0f, 2f) }, 2.5f);

            LayoutRestore restore = slots.RestoreCheckpoint(smallRoom);

            Assert.AreEqual("fits", Ids(restore));
            Assert.AreEqual(1, restore.Fit.Skipped.Count);
            Assert.AreEqual("outside-small-room", restore.Fit.Skipped[0].Id);
        }

        [Test]
        public void Load_UnreadableCheckpoint_ReportsUnreadableWithoutThrowing()
        {
            storage.Slots[LayoutSlot.Checkpoint] = "{ this is not json";

            LayoutRestore restore = null;
            Assert.DoesNotThrow(() => restore = slots.RestoreCheckpoint(Room));

            Assert.AreEqual(LayoutRestoreOutcome.Unreadable, restore.Outcome);
            Assert.IsNotEmpty(restore.Error);
            Assert.AreEqual(0, restore.Fit.Accepted.Count);
        }

        [Test]
        public void Load_ReportsUnknownFurnitureInTheCheckpoint()
        {
            storage.Slots[LayoutSlot.Checkpoint] = "{ \"version\": 1, \"items\": [" +
                "{ \"id\": \"a\", \"definitionId\": \"chair\", \"posX\": 1, \"posZ\": 1, \"yawDegrees\": 0 }," +
                "{ \"id\": \"b\", \"definitionId\": \"piano\", \"posX\": 2, \"posZ\": 2, \"yawDegrees\": 0 } ] }";

            LayoutRestore restore = slots.RestoreCheckpoint(Room);

            Assert.AreEqual("a", Ids(restore));
            Assert.AreEqual(1, restore.DroppedUnknownCount);
        }

        [Test]
        public void Save_ReportsStorageFailure()
        {
            storage.FailWrites = true;

            Assert.IsFalse(slots.SaveCheckpoint(new[] { new PlacedItem(Chair, 1f, 1f, 0f, "a") }));
            Assert.IsFalse(slots.HasCheckpoint);
        }

        [Test]
        public void StatusFormatter_CheckpointMessages()
        {
            Assert.AreEqual("Layout saved.", LayoutStatusFormatter.CheckpointSavedText);
            Assert.AreEqual("Saved layout restored.", LayoutStatusFormatter.Restored(0, 0, 0));
            Assert.AreEqual("Saved layout restored. 2 did not fit this room.", LayoutStatusFormatter.Restored(2, 0, 0));
            Assert.AreEqual("No saved layout yet.", LayoutStatusFormatter.NothingSavedText);
        }
    }
}
