using System;
using System.Globalization;

namespace RoomPlanner.Core
{
    public static class RoomStatusFormatter
    {
        public const string LoadingText = "Loading room...";
        public const string NoRoomFoundText = "No room found. Run Space Setup on your Quest, then restart.";
        public const string PermissionDeniedText = "Allow Spatial data for this app in Settings > Privacy & Safety, then restart.";
        public const string ErrorText = "Could not load the room. Restart the app to try again.";

        public static string Loading() => LoadingText;

        // Invariant culture so the decimal separator is always "." regardless of the headset's locale.
        public static string Loaded(RoomFootprint room)
        {
            if (room == null)
            {
                throw new ArgumentNullException(nameof(room));
            }

            return string.Format(CultureInfo.InvariantCulture,
                "Room loaded: {0:0.0} x {1:0.0} m, {2} walls", room.Width, room.Depth, room.WallCount);
        }

        public static string Failed(RoomLoadFailure failure)
        {
            switch (failure)
            {
                case RoomLoadFailure.NoRoomFound:
                    return NoRoomFoundText;
                case RoomLoadFailure.PermissionDenied:
                    return PermissionDeniedText;
                default:
                    return ErrorText;
            }
        }
    }
}
