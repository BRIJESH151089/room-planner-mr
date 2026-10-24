using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class OverlapCheckerTests
    {
        private static OrientedRect Square(float x, float z, float size = 2f, float yaw = 0f) =>
            new OrientedRect(new Vector2(x, z), size, size, yaw);

        [Test]
        public void Separate_DoNotOverlap()
        {
            Assert.IsFalse(OverlapChecker.Overlaps(Square(0f, 0f), Square(5f, 0f)));
        }

        [Test]
        public void Overlapping_Overlap()
        {
            Assert.IsTrue(OverlapChecker.Overlaps(Square(0f, 0f), Square(1.5f, 0.5f)));
        }

        [Test]
        public void TouchingEdges_DoNotOverlap()
        {
            // Both 2 m wide: the first spans x -1..1, the second x 1..3.
            Assert.IsFalse(OverlapChecker.Overlaps(Square(0f, 0f), Square(2f, 0f)));
        }

        [Test]
        public void TouchingCorners_DoNotOverlap()
        {
            Assert.IsFalse(OverlapChecker.Overlaps(Square(0f, 0f), Square(2f, 2f)));
        }

        [Test]
        public void OneInsideAnother_Overlap()
        {
            Assert.IsTrue(OverlapChecker.Overlaps(Square(0f, 0f, size: 4f), Square(0.5f, 0.5f, size: 1f)));
        }

        [Test]
        public void Rotated45_BoundingBoxesOverlapButShapesDoNot()
        {
            // The diamond's nearest edge is the line x + z = 2.99, past the square's corner (1, 1).
            // An axis-aligned bounding box test would wrongly report an overlap here.
            Assert.IsFalse(OverlapChecker.Overlaps(Square(0f, 0f), Square(2.2f, 2.2f, yaw: 45f)));
        }

        [Test]
        public void Rotated45_PokingIntoSide_Overlap()
        {
            // The diamond's left tip reaches x = 2.3 - 1.414 = 0.886, inside the square's right edge at x = 1.
            Assert.IsTrue(OverlapChecker.Overlaps(Square(0f, 0f), Square(2.3f, 0f, yaw: 45f)));
        }

        [Test]
        public void Segment_CrossingRect_Overlaps()
        {
            Assert.IsTrue(OverlapChecker.SegmentOverlaps(new Vector2(-5f, 0f), new Vector2(5f, 0f), Square(0f, 0f)));
        }

        [Test]
        public void Segment_AlongRectEdge_DoesNotOverlap()
        {
            Assert.IsFalse(OverlapChecker.SegmentOverlaps(new Vector2(-5f, 1f), new Vector2(5f, 1f), Square(0f, 0f)));
        }

        [Test]
        public void PlacedItem_RotatedNinetyDegrees_SwapsWidthAndDepth()
        {
            var sofa = new PlacedItem(FurnitureCatalog.Get(FurnitureCatalog.SofaId), 0f, 0f, 90f); // 2.0 wide, 0.9 deep
            Vector2[] corners = sofa.GetCorners();

            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (Vector2 corner in corners)
            {
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minZ = Mathf.Min(minZ, corner.y);
                maxZ = Mathf.Max(maxZ, corner.y);
            }

            Assert.AreEqual(0.9f, maxX - minX, 1e-4f);
            Assert.AreEqual(2.0f, maxZ - minZ, 1e-4f);
        }
    }
}
