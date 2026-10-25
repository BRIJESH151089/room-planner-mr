using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class GrabOffsetTests
    {
        private const float Tolerance = 1e-4f;

        private static readonly FurnitureDefinition Bed = FurnitureCatalog.Get(FurnitureCatalog.BedId); // 1.6 wide x 2.0 deep

        private static void AssertNear(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, "x");
            Assert.AreEqual(expected.y, actual.y, Tolerance, "z");
        }

        [Test]
        public void CentreGrab_HasNoOffset_CentreFollowsPointer()
        {
            var bed = new PlacedItem(Bed, 2f, 3f, 0f, "b");

            Vector2 offset = GrabOffset.FromGrabPoint(bed, new Vector2(2f, 3f));

            AssertNear(Vector2.zero, offset);
            AssertNear(new Vector2(5f, 5f), GrabOffset.CentreFromPointer(new Vector2(5f, 5f), offset, 0f));
        }

        [Test]
        public void OffCentreGrab_KeepsGrabbedSpotUnderPointer()
        {
            // Grabbed at the foot end of the bed, 0.8 m from its centre along z.
            var bed = new PlacedItem(Bed, 2f, 3f, 0f, "b");
            Vector2 offset = GrabOffset.FromGrabPoint(bed, new Vector2(2f, 3.8f));

            Vector2 centre = GrabOffset.CentreFromPointer(new Vector2(4f, 4f), offset, 0f);

            AssertNear(new Vector2(4f, 3.2f), centre);
        }

        [Test]
        public void PointerNotMoved_CentreStaysPut_NoJumpOnPickup()
        {
            var bed = new PlacedItem(Bed, 1.25f, -0.5f, 30f, "b");
            var grab = new Vector2(1.6f, -0.1f);

            Vector2 offset = GrabOffset.FromGrabPoint(bed, grab);

            AssertNear(new Vector2(1.25f, -0.5f), GrabOffset.CentreFromPointer(grab, offset, 30f));
        }

        [Test]
        public void RotatedItem_OffsetIsInItsOwnFrame()
        {
            // Bed turned 90 degrees: its local +z (foot end) now points along world +x.
            var bed = new PlacedItem(Bed, 0f, 0f, 90f, "b");
            Vector2 offset = GrabOffset.FromGrabPoint(bed, new Vector2(0.8f, 0f)); // 0.8 m towards the foot end

            AssertNear(new Vector2(0f, -0.8f), offset);
            AssertNear(new Vector2(2.2f, 1f), GrabOffset.CentreFromPointer(new Vector2(3f, 1f), offset, 90f));
        }

        [Test]
        public void TurningDuringMove_OffsetTurnsWithTheItem()
        {
            var bed = new PlacedItem(Bed, 0f, 0f, 0f, "b");
            Vector2 offset = GrabOffset.FromGrabPoint(bed, new Vector2(0f, 0.8f)); // foot end, item at yaw 0

            // Rotated a quarter turn while held: the foot end (still under the pointer) now points along +x.
            Vector2 centre = GrabOffset.CentreFromPointer(new Vector2(5f, 5f), offset, 90f);

            AssertNear(new Vector2(4.2f, 5f), centre);
        }

        [Test]
        public void GrabPointOutsideFootprint_IsClampedOntoIt()
        {
            var bed = new PlacedItem(Bed, 0f, 0f, 0f, "b");

            Vector2 offset = GrabOffset.FromGrabPoint(bed, new Vector2(5f, -7f));

            AssertNear(new Vector2(-0.8f, 1f), offset); // clamped to the corner (0.8, -1.0)
        }

        [Test]
        public void ChooseGrabPoint_FloorHitOnFootprint_IsPreferred()
        {
            var bed = new PlacedItem(Bed, 0f, 0f, 0f, "b");

            AssertNear(new Vector2(0.3f, 0.4f), GrabOffset.ChooseGrabPoint(bed, true, new Vector2(0.3f, 0.4f), new Vector2(0.5f, 0.5f)));
        }

        [Test]
        public void ChooseGrabPoint_FloorHitBeyondItemOrMissing_UsesBoxHit()
        {
            var bed = new PlacedItem(Bed, 0f, 0f, 0f, "b");

            AssertNear(new Vector2(0.5f, 0.5f), GrabOffset.ChooseGrabPoint(bed, true, new Vector2(3f, 3f), new Vector2(0.5f, 0.5f)));
            AssertNear(new Vector2(0.5f, 0.5f), GrabOffset.ChooseGrabPoint(bed, false, default, new Vector2(0.5f, 0.5f)));
        }

        [Test]
        public void IsOnFootprint_RespectsRotation()
        {
            var bed = new PlacedItem(Bed, 0f, 0f, 90f, "b"); // now 2.0 along x, 1.6 along z

            Assert.IsTrue(GrabOffset.IsOnFootprint(bed, new Vector2(0.95f, 0f)));
            Assert.IsFalse(GrabOffset.IsOnFootprint(bed, new Vector2(0f, 0.95f)));
        }
    }
}
