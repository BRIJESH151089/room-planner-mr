using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using RoomPlanner.Core;

namespace RoomPlanner.Tests
{
    public class FurnitureCatalogTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void Default_HasFiveItemsWithUniqueIds()
        {
            var ids = new HashSet<string>();
            foreach (FurnitureDefinition definition in FurnitureCatalog.Default)
            {
                Assert.IsTrue(ids.Add(definition.Id), $"Duplicate id {definition.Id}");
            }

            Assert.AreEqual(5, ids.Count);
        }

        [TestCase(FurnitureCatalog.BedId, 1.6f, 2.0f, 0.5f)]
        [TestCase(FurnitureCatalog.SofaId, 2.0f, 0.9f, 0.85f)]
        [TestCase(FurnitureCatalog.TableId, 1.2f, 0.8f, 0.75f)]
        [TestCase(FurnitureCatalog.ChairId, 0.5f, 0.5f, 0.9f)]
        [TestCase(FurnitureCatalog.WardrobeId, 1.0f, 0.6f, 2.0f)]
        public void Get_ReturnsRealisticSizes(string id, float width, float depth, float height)
        {
            FurnitureDefinition definition = FurnitureCatalog.Get(id);

            Assert.AreEqual(id, definition.Id);
            Assert.AreEqual(width, definition.Width, Tolerance);
            Assert.AreEqual(depth, definition.Depth, Tolerance);
            Assert.AreEqual(height, definition.Height, Tolerance);
        }

        [Test]
        public void TryGet_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(FurnitureCatalog.TryGet("piano", out FurnitureDefinition definition));
            Assert.IsNull(definition);
        }

        [Test]
        public void Get_UnknownId_Throws()
        {
            Assert.Throws<KeyNotFoundException>(() => FurnitureCatalog.Get("piano"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Definition_WithEmptyId_Throws(string id)
        {
            Assert.Throws<System.ArgumentException>(() => new FurnitureDefinition(id, "Bed", 1f, 1f, 1f));
        }

        [TestCase(0f, 1f, 1f)]
        [TestCase(1f, -1f, 1f)]
        [TestCase(1f, 1f, 0f)]
        public void Definition_WithNonPositiveSize_Throws(float width, float depth, float height)
        {
            Assert.Throws<System.ArgumentException>(() => new FurnitureDefinition("x", "X", width, depth, height));
        }

        [Test]
        public void LabelFormatter_ShowsNameAndFootprintWithDotDecimalInAnyCulture()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

                Assert.AreEqual("Bed  1.6 x 2.0 m", FurnitureLabelFormatter.Format(FurnitureCatalog.Get(FurnitureCatalog.BedId)));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
