using System.Globalization;
using System.Threading;
using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class RoomMetricsTests
    {
        private const float Tolerance = 1e-4f;

        private static readonly FurnitureDefinition Bed = FurnitureCatalog.Get(FurnitureCatalog.BedId); // 1.6 x 2.0 = 3.2 m2
        private static readonly FurnitureDefinition Chair = FurnitureCatalog.Get(FurnitureCatalog.ChairId); // 0.5 x 0.5 = 0.25 m2

        // 4 m x 3 m.
        private static readonly RoomFootprint Rectangle = new RoomFootprint(new[]
        {
            new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(4f, 3f), new Vector2(0f, 3f)
        }, height: 2.5f);

        // 4x4 square with the 2x2 top-right quarter cut out: 12 m2.
        private static readonly RoomFootprint LRoom = new RoomFootprint(new[]
        {
            new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(4f, 2f),
            new Vector2(2f, 2f), new Vector2(2f, 4f), new Vector2(0f, 4f)
        }, height: 2.5f);

        [Test]
        public void RectangularRoom_Empty()
        {
            RoomMetrics metrics = RoomMetrics.Compute(Rectangle, new PlacedItem[0]);

            Assert.AreEqual(4f, metrics.Width, Tolerance);
            Assert.AreEqual(3f, metrics.Depth, Tolerance);
            Assert.AreEqual(12f, metrics.FloorArea, Tolerance);
            Assert.AreEqual(0f, metrics.UsedArea, Tolerance);
            Assert.AreEqual(12f, metrics.FreeArea, Tolerance);
            Assert.AreEqual(0, metrics.ItemCount);
        }

        [Test]
        public void RectangularRoom_WithItems_SumsFootprintsIgnoringRotation()
        {
            var items = new[]
            {
                new PlacedItem(Bed, 1f, 1.5f, 90f, "a"),
                new PlacedItem(Chair, 3f, 1f, 45f, "b"),
                new PlacedItem(Chair, 3f, 2f, 0f, "c")
            };

            RoomMetrics metrics = RoomMetrics.Compute(Rectangle, items);

            Assert.AreEqual(3.7f, metrics.UsedArea, Tolerance);
            Assert.AreEqual(12f - 3.7f, metrics.FreeArea, Tolerance);
            Assert.AreEqual(3, metrics.ItemCount);
        }

        [Test]
        public void LShapedRoom_UsesPolygonArea_NotBoundingBox()
        {
            RoomMetrics metrics = RoomMetrics.Compute(LRoom, new[] { new PlacedItem(Bed, 1f, 1f, 0f, "a") });

            Assert.AreEqual(4f, metrics.Width, Tolerance);
            Assert.AreEqual(4f, metrics.Depth, Tolerance);
            Assert.AreEqual(12f, metrics.FloorArea, Tolerance, "Shoelace area, not 4 x 4 = 16.");
            Assert.AreEqual(3.2f, metrics.UsedArea, Tolerance);
            Assert.AreEqual(8.8f, metrics.FreeArea, Tolerance);
        }

        [Test]
        public void FreeArea_IsNeverNegative()
        {
            var tiny = new RoomFootprint(new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) }, 2.5f);
            var items = new[] { new PlacedItem(Bed, 0.5f, 0.5f, 0f, "a") }; // 3.2 m2 listed in a 1 m2 room

            RoomMetrics metrics = RoomMetrics.Compute(tiny, items);

            Assert.AreEqual(0f, metrics.FreeArea);
            Assert.AreEqual(0f, new RoomMetrics(2f, 2f, 1f, 5f, 1).FreeArea);
        }

        [Test]
        public void EmptyRoom_IsAllZero()
        {
            RoomMetrics metrics = RoomMetrics.Compute(RoomFootprint.Empty, new PlacedItem[0]);

            Assert.AreEqual(0f, metrics.FloorArea);
            Assert.AreEqual(0f, metrics.FreeArea);
            Assert.AreEqual(0, metrics.ItemCount);
        }

        [Test]
        public void Formatter_RoundsToOneDecimal()
        {
            var metrics = new RoomMetrics(6.819f, 5.449f, 32.882f, 3.75f, 3);

            Assert.AreEqual("Room: 6.8 x 5.4 m", MetricsFormatter.Room(metrics));
            Assert.AreEqual("Floor: 32.9 m2", MetricsFormatter.Floor(metrics));
            Assert.AreEqual("Used: 3.8 m2", MetricsFormatter.Used(metrics));
            Assert.AreEqual("Free: 29.1 m2", MetricsFormatter.Free(metrics));
            Assert.AreEqual("Items: 3", MetricsFormatter.Items(metrics));
        }

        [Test]
        public void Formatter_All_IsFiveLinesInOrder()
        {
            var metrics = new RoomMetrics(4f, 3f, 12f, 3.2f, 1);

            Assert.AreEqual("Room: 4.0 x 3.0 m\nFloor: 12.0 m2\nUsed: 3.2 m2\nFree: 8.8 m2\nItems: 1", MetricsFormatter.All(metrics));
        }

        [Test]
        public void Formatter_UsesDotDecimalInAnyCulture()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

                Assert.AreEqual("Floor: 12.5 m2", MetricsFormatter.Floor(new RoomMetrics(4f, 3f, 12.5f, 0f, 0)));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
