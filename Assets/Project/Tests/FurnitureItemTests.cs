using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class FurnitureItemTests
    {
        [Test]
        public void Constructor_WithValidArgs_SetsProperties()
        {
            var item = new FurnitureItem("Sofa", new Vector2(2f, 1f), SurfaceType.Floor);

            Assert.AreEqual("Sofa", item.Name);
            Assert.AreEqual(new Vector2(2f, 1f), item.Size);
            Assert.AreEqual(SurfaceType.Floor, item.AllowedSurface);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void Constructor_WithInvalidName_Throws(string name)
        {
            Assert.Throws<System.ArgumentException>(() =>
                new FurnitureItem(name, new Vector2(1f, 1f), SurfaceType.Wall));
        }

        [TestCase(0f, 1f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, 1f)]
        public void Constructor_WithNonPositiveSize_Throws(float width, float depth)
        {
            Assert.Throws<System.ArgumentException>(() =>
                new FurnitureItem("Lamp", new Vector2(width, depth), SurfaceType.Floor));
        }
    }
}
