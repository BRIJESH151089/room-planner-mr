using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class FloorRayAndYawTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void FloorRay_PointingDown_HitsFloor()
        {
            bool hit = FloorRay.TryHit(new Vector3(1f, 1.5f, 2f), new Vector3(0f, -1f, 1f), floorHeight: 0f, out Vector3 point);

            Assert.IsTrue(hit);
            Assert.AreEqual(1f, point.x, Tolerance);
            Assert.AreEqual(0f, point.y, Tolerance);
            Assert.AreEqual(3.5f, point.z, Tolerance);
        }

        [Test]
        public void FloorRay_UsesGivenFloorHeight()
        {
            bool hit = FloorRay.TryHit(new Vector3(0f, 1f, 0f), Vector3.down, floorHeight: -0.4f, out Vector3 point);

            Assert.IsTrue(hit);
            Assert.AreEqual(-0.4f, point.y, Tolerance);
        }

        [Test]
        public void FloorRay_PointingUp_Misses()
        {
            Assert.IsFalse(FloorRay.TryHit(new Vector3(0f, 1.5f, 0f), new Vector3(0f, 0.5f, 1f), 0f, out _));
        }

        [Test]
        public void FloorRay_ParallelToFloor_Misses()
        {
            Assert.IsFalse(FloorRay.TryHit(new Vector3(0f, 1.5f, 0f), Vector3.forward, 0f, out _));
        }

        [Test]
        public void FloorRay_OriginBelowFloor_Misses()
        {
            Assert.IsFalse(FloorRay.TryHit(new Vector3(0f, -1f, 0f), Vector3.down, 0f, out _));
        }

        [Test]
        public void FloorRay_HitBeyondMaxDistance_Misses()
        {
            // Shallow ray: hits about 15 m away.
            Assert.IsFalse(FloorRay.TryHit(new Vector3(0f, 1.5f, 0f), new Vector3(0f, -0.1f, 1f), 0f, out _, maxDistance: 10f));
        }

        [TestCase(0f, 1, 15f)]
        [TestCase(345f, 1, 0f)]
        [TestCase(0f, -1, 345f)]
        [TestCase(350f, 2, 15f)]
        [TestCase(720f, 0, 0f)]
        public void YawStepper_Step_WrapsAround(float start, int steps, float expected)
        {
            Assert.AreEqual(expected, YawStepper.Step(start, steps), Tolerance);
        }

        [TestCase(7f, 0f)]
        [TestCase(8f, 15f)]
        [TestCase(-8f, 345f)]
        [TestCase(359f, 0f)]
        public void YawStepper_Snap_RoundsToNearestStep(float yaw, float expected)
        {
            Assert.AreEqual(expected, YawStepper.Snap(yaw), Tolerance);
        }

        [Test]
        public void GridSnapper_SnapsToFiveCentimetres()
        {
            Vector2 snapped = GridSnapper.Snap(new Vector2(1.23f, -0.026f), GridSnapper.PositionStep);

            Assert.AreEqual(1.25f, snapped.x, Tolerance);
            Assert.AreEqual(-0.05f, snapped.y, Tolerance);
        }

        [Test]
        public void AxisStepDetector_OneStepPerPush()
        {
            var detector = new AxisStepDetector();

            Assert.AreEqual(1, detector.Update(0.9f));
            Assert.AreEqual(0, detector.Update(1f), "Holding the stick must not repeat.");
            Assert.AreEqual(0, detector.Update(0.5f), "Not released yet (above the release threshold).");
            Assert.AreEqual(0, detector.Update(0.8f), "Still the same push.");
            Assert.AreEqual(0, detector.Update(0f));
            Assert.AreEqual(-1, detector.Update(-0.75f));
            Assert.AreEqual(0, detector.Update(0.1f));
            Assert.AreEqual(0, detector.Update(0.6f), "Below the press threshold.");
        }
    }
}
