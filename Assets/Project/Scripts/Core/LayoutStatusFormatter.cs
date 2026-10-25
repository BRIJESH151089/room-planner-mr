using System.Collections.Generic;
using System.Globalization;

namespace RoomPlanner.Core
{
    // Short messages about saving and loading, shown on the measurements panel.
    public static class LayoutStatusFormatter
    {
        public const string NothingSavedText = "No saved layout yet.";
        public const string LoadFailedText = "Saved layout could not be read.";
        public const string SaveFailedText = "Could not save the layout.";
        public const string ClearedText = "Layout cleared.";
        public const string ClearConfirmText = "Press Clear All again to confirm.";
        public const string CheckpointSavedText = "Layout saved.";
        public const string CheckpointRestoredText = "Saved layout restored.";

        // Load button: the saved layout came back; mentions items that did not fit or could not be read.
        public static string Restored(int skippedNoFit, int droppedUnknown, int droppedInvalid)
        {
            var parts = new List<string> { CheckpointRestoredText };
            AddDropped(parts, skippedNoFit, droppedUnknown, droppedInvalid);
            return string.Join(" ", parts);
        }

        public static string Loaded(int placed, int skippedNoFit, int droppedUnknown, int droppedInvalid)
        {
            var parts = new List<string> { string.Format(CultureInfo.InvariantCulture, "Loaded {0} {1}.", placed, placed == 1 ? "item" : "items") };
            AddDropped(parts, skippedNoFit, droppedUnknown, droppedInvalid);
            return string.Join(" ", parts);
        }

        private static void AddDropped(List<string> parts, int skippedNoFit, int droppedUnknown, int droppedInvalid)
        {
            if (skippedNoFit > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0} did not fit this room.", skippedNoFit));
            }

            if (droppedUnknown > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0} not in the catalog.", droppedUnknown));
            }

            if (droppedInvalid > 0)
            {
                parts.Add(string.Format(CultureInfo.InvariantCulture, "{0} unreadable.", droppedInvalid));
            }
        }

        public static string Saved(int count) =>
            string.Format(CultureInfo.InvariantCulture, "Saved {0} {1}.", count, count == 1 ? "item" : "items");
    }
}
