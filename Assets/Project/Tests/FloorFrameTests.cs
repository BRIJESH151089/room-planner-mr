using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class FloorFrameTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void Default_IsIdentity()
        {
            var frame = default(FloorFrame);

            Assert.AreEqual(new Vector2(1.5f, -2f), frame.ToLocal(1.5f, -2f));
            Assert.AreEqual(new Vector2(1.5f, -2f), frame.ToWorld(new Vector2(1.5f, -2f)));
            Assert.AreEqual(0f, frame.Height);
            Assert.AreEqual(30f, frame.ToWorldYaw(30f));
        }

        [Test]
        public void Yaw90_MatchesUnityConvention_LocalForwardBecomesWorldRight()
        {
            var frame = new FloorFrame(0f, 0f, 0f, 90f);

            Vector2 world = frame.ToWorld(new Vector2(0f, 1f));
            Vector3 unity = Quaternion.Euler(0f, 90f, 0f) * Vector3.forward;

            Assert.AreEqual(unity.x, world.x, Tolerance);
            Assert.AreEqual(unity.z, world.y, Tolerance);
        }

        [Test]
        public void ToLocal_UndoesToWorld()
        {
            var frame = new FloorFrame(2f, -1f, 0.3f, 37f);

            Vector2 world = frame.ToWorld(new Vector2(1.2f, 3.4f));
            Vector2 back = frame.ToLocal(world.x, world.y);

            Assert.AreEqual(1.2f, back.x, Tolerance);
            Assert.AreEqual(3.4f, back.y, Tolerance);
        }

        [Test]
        public void RoomFootprint_KeepsFrame_AndDefaultsToIdentity()
        {
            var corners = new[] { new Vector2(0f, 0f), new Vector2(2f, 0f), new Vector2(2f, 2f) };
            var frame = new FloorFrame(1f, 2f, -0.1f, 45f);

            Assert.AreEqual(frame, new RoomFootprint(corners, 2.5f, frame).Frame);
            Assert.AreEqual(default(FloorFrame), new RoomFootprint(corners, 2.5f).Frame);
        }
    }
}
