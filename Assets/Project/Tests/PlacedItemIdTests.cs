using System.Collections.Generic;
using NUnit.Framework;
using RoomPlanner.Core;

namespace RoomPlanner.Tests
{
    public class PlacedItemIdTests
    {
        private static readonly FurnitureDefinition Chair = FurnitureCatalog.Get(FurnitureCatalog.ChairId);

        [Test]
        public void Candidate_HasNoId()
        {
            var candidate = new PlacedItem(Chair, 1f, 2f, 15f);

            Assert.IsFalse(candidate.HasId);
            Assert.IsNull(candidate.Id);
        }

        [Test]
        public void NewId_Is32HexCharactersAndUnique()
        {
            var ids = new HashSet<string>();
            for (int i = 0; i < 1000; i++)
            {
                string id = PlacedItem.NewId();
                StringAssert.IsMatch("^[0-9a-f]{32}$", id);
                Assert.IsTrue(ids.Add(id));
            }
        }

        [Test]
        public void WithId_KeepsPositionAndAddsId()
        {
            PlacedItem placed = new PlacedItem(Chair, 1f, 2f, 15f).WithId("abc");

            Assert.AreEqual("abc", placed.Id);
            Assert.AreEqual(1f, placed.X);
            Assert.AreEqual(2f, placed.Z);
            Assert.AreEqual(15f, placed.YawDegrees);
            Assert.AreSame(Chair, placed.Definition);
        }

        [Test]
        public void MovedTo_KeepsIdAndDefinition()
        {
            PlacedItem placed = new PlacedItem(Chair, 1f, 2f, 15f, "abc");

            PlacedItem moved = placed.MovedTo(3f, 4f, 90f);

            Assert.AreEqual("abc", moved.Id);
            Assert.AreSame(Chair, moved.Definition);
            Assert.AreEqual(3f, moved.X);
            Assert.AreEqual(4f, moved.Z);
            Assert.AreEqual(90f, moved.YawDegrees);
        }

        [TestCase("")]
        [TestCase("  ")]
        public void EmptyId_Throws(string id)
        {
            Assert.Throws<System.ArgumentException>(() => new PlacedItem(Chair, 0f, 0f, 0f, id));
        }
    }
}
