using System.Globalization;
using System.Threading;
using NUnit.Framework;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Tests
{
    public class RoomStatusFormatterTests
    {
        private static readonly RoomFootprint Room = new RoomFootprint(new[]
        {
            new Vector2(0f, 0f), new Vector2(4.23f, 0f), new Vector2(4.23f, 3.16f), new Vector2(0f, 3.16f)
        }, height: 2.5f);

        [Test]
        public void Loading_ReturnsLoadingText()
        {
            Assert.AreEqual("Loading room...", RoomStatusFormatter.Loading());
        }

        [Test]
        public void Loaded_ShowsSizeRoundedToOneDecimalAndWallCount()
        {
            Assert.AreEqual("Room loaded: 4.2 x 3.2 m, 4 walls", RoomStatusFormatter.Loaded(Room));
        }

        [Test]
        public void Loaded_UsesDotDecimalSeparatorInAnyCulture()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

                Assert.AreEqual("Room loaded: 4.2 x 3.2 m, 4 walls", RoomStatusFormatter.Loaded(Room));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void Loaded_WithNullRoom_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => RoomStatusFormatter.Loaded(null));
        }

        [Test]
        public void Failed_NoRoomFound_AsksUserToRunSpaceSetup()
        {
            Assert.AreEqual("No room found. Run Space Setup on your Quest, then restart.",
                RoomStatusFormatter.Failed(RoomLoadFailure.NoRoomFound));
        }

        [Test]
        public void Failed_PermissionDenied_ExplainsHowToAllowIt()
        {
            Assert.AreEqual("Spatial data permission denied. Allow it for this app in the Quest settings, then restart.",
                RoomStatusFormatter.Failed(RoomLoadFailure.PermissionDenied));
        }

        [Test]
        public void Failed_Error_ReturnsGenericMessage()
        {
            Assert.AreEqual("Could not load the room. Restart the app to try again.",
                RoomStatusFormatter.Failed(RoomLoadFailure.Error));
        }
    }
}
