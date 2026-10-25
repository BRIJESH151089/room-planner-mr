namespace RoomPlanner.Core
{
    // First-use hint: shown once the room has loaded while nothing is placed yet, and gone for good after the first
    // item is placed (it does not come back if everything is deleted or cleared later in the same session).
    public sealed class OnboardingHint
    {
        public const string Text = "Point at the panel on your left and pull the trigger to pick furniture. Aim at the floor to place it.";

        private bool roomLoaded;
        private bool dismissed;

        public bool IsVisible => roomLoaded && !dismissed;

        // itemCount: items in the room after loading (a restored layout counts, so returning users skip the hint).
        public void OnRoomReady(int itemCount)
        {
            roomLoaded = true;
            if (itemCount > 0)
            {
                dismissed = true;
            }
        }

        public void OnItemCountChanged(int itemCount)
        {
            if (roomLoaded && itemCount > 0)
            {
                dismissed = true;
            }
        }
    }
}
