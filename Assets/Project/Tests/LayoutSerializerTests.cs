using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using RoomPlanner.Core;

namespace RoomPlanner.Tests
{
    public class LayoutSerializerTests
    {
        private static readonly FurnitureDefinition Bed = FurnitureCatalog.Get(FurnitureCatalog.BedId);
        private static readonly FurnitureDefinition Chair = FurnitureCatalog.Get(FurnitureCatalog.ChairId);

        private static void AssertSame(PlacedItem expected, PlacedItem actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreSame(expected.Definition, actual.Definition);
            Assert.AreEqual(expected.X, actual.X, 0f, "posX must round-trip exactly");
            Assert.AreEqual(expected.Z, actual.Z, 0f, "posZ must round-trip exactly");
            Assert.AreEqual(expected.YawDegrees, actual.YawDegrees, 0f, "yaw must round-trip exactly");
        }

        [Test]
        public void RoundTrip_NoItems()
        {
            LayoutLoadResult result = LayoutSerializer.Deserialize(LayoutSerializer.Serialize(new PlacedItem[0]));

            Assert.IsFalse(result.HasError);
            Assert.AreEqual(0, result.Items.Count);
        }

        [Test]
        public void RoundTrip_OneItem()
        {
            var item = new PlacedItem(Bed, 1.25f, -0.5f, 90f, "a1");

            LayoutLoadResult result = LayoutSerializer.Deserialize(LayoutSerializer.Serialize(new[] { item }));

            Assert.IsFalse(result.HasError);
            Assert.AreEqual(1, result.Items.Count);
            AssertSame(item, result.Items[0]);
        }

        [Test]
        public void RoundTrip_ManyItems_IsExactIncludingAwkwardFloats()
        {
            var items = new List<PlacedItem>();
            float[] awkward = { 0.1f, 1f / 3f, -2.35f, 1e-7f, 123456.78f, -0f, 359.99997f, float.Epsilon };
            for (int i = 0; i < 40; i++)
            {
                FurnitureDefinition definition = FurnitureCatalog.Default[i % FurnitureCatalog.Default.Count];
                items.Add(new PlacedItem(definition, awkward[i % awkward.Length], awkward[(i + 3) % awkward.Length], i * 15f % 360f, PlacedItem.NewId()));
            }

            LayoutLoadResult result = LayoutSerializer.Deserialize(LayoutSerializer.Serialize(items));

            Assert.IsFalse(result.HasError);
            Assert.AreEqual(items.Count, result.Items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                AssertSame(items[i], result.Items[i]);
            }
        }

        [Test]
        public void Serialize_IsReadableJsonWithVersionAndFieldNames()
        {
            string json = LayoutSerializer.Serialize(new[] { new PlacedItem(Chair, 2.35f, 0.5f, 15f, "abc") });

            StringAssert.Contains("\"version\": 1", json);
            StringAssert.Contains("\"id\": \"abc\"", json);
            StringAssert.Contains("\"definitionId\": \"chair\"", json);
            StringAssert.Contains("\"posX\": 2.35", json);
            StringAssert.Contains("\"posZ\": 0.5", json);
            StringAssert.Contains("\"yawDegrees\": 15", json);
            StringAssert.Contains("\n", json, "Pretty-printed, one item per line.");
        }

        [Test]
        public void Serialize_UsesDotDecimalInAnyCulture()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                string json = LayoutSerializer.Serialize(new[] { new PlacedItem(Chair, 2.35f, 0.5f, 15f, "abc") });

                StringAssert.Contains("\"posX\": 2.35", json);
                AssertSame(new PlacedItem(Chair, 2.35f, 0.5f, 15f, "abc"), LayoutSerializer.Deserialize(json).Items[0]);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void Serialize_ItemWithoutId_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => LayoutSerializer.Serialize(new[] { new PlacedItem(Chair, 0f, 0f, 0f) }));
        }

        [Test]
        public void Deserialize_UnknownDefinition_IsDroppedAndCounted()
        {
            const string json = "{ \"version\": 1, \"items\": [" +
                                "{ \"id\": \"a\", \"definitionId\": \"bed\", \"posX\": 1, \"posZ\": 2, \"yawDegrees\": 0 }," +
                                "{ \"id\": \"b\", \"definitionId\": \"piano\", \"posX\": 1, \"posZ\": 2, \"yawDegrees\": 0 }," +
                                "{ \"id\": \"c\", \"definitionId\": \"spaceship\", \"posX\": 1, \"posZ\": 2, \"yawDegrees\": 0 } ] }";

            LayoutLoadResult result = LayoutSerializer.Deserialize(json);

            Assert.IsFalse(result.HasError);
            Assert.AreEqual(1, result.Items.Count);
            Assert.AreEqual("a", result.Items[0].Id);
            Assert.AreEqual(2, result.DroppedUnknownCount);
        }

        [Test]
        public void Deserialize_MissingOrDuplicateIds_AreDroppedAsInvalid()
        {
            const string json = "{ \"version\": 1, \"items\": [" +
                                "{ \"id\": \"a\", \"definitionId\": \"bed\", \"posX\": 1, \"posZ\": 2, \"yawDegrees\": 0 }," +
                                "{ \"id\": \"a\", \"definitionId\": \"sofa\", \"posX\": 3, \"posZ\": 2, \"yawDegrees\": 0 }," +
                                "{ \"definitionId\": \"chair\", \"posX\": 1, \"posZ\": 2, \"yawDegrees\": 0 } ] }";

            LayoutLoadResult result = LayoutSerializer.Deserialize(json);

            Assert.AreEqual(1, result.Items.Count);
            Assert.AreSame(Bed, result.Items[0].Definition);
            Assert.AreEqual(2, result.DroppedInvalidCount);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("{ not json")]
        [TestCase("{ \"version\": 1, \"items\": [ { \"id\": ")]
        public void Deserialize_EmptyOrMalformed_ReturnsEmptyLayoutWithError(string json)
        {
            LayoutLoadResult result = null;
            Assert.DoesNotThrow(() => result = LayoutSerializer.Deserialize(json));

            Assert.IsTrue(result.HasError);
            Assert.IsNotEmpty(result.Error);
            Assert.AreEqual(0, result.Items.Count);
        }

        [TestCase("{ \"items\": [] }")]
        [TestCase("{ \"version\": 99, \"items\": [] }")]
        public void Deserialize_MissingOrNewerVersion_ReturnsError(string json)
        {
            LayoutLoadResult result = LayoutSerializer.Deserialize(json);

            Assert.IsTrue(result.HasError);
            StringAssert.Contains("version", result.Error);
        }

        [Test]
        public void Deserialize_NoItemsArray_IsAnEmptyLayout()
        {
            LayoutLoadResult result = LayoutSerializer.Deserialize("{ \"version\": 1 }");

            Assert.IsFalse(result.HasError);
            Assert.AreEqual(0, result.Items.Count);
        }
    }
}
