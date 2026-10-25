using System;
using System.Collections.Generic;

namespace RoomPlanner.Core
{
    // Two independent saved layouts:
    //   Session    - written automatically after every edit, resumed when the app starts.
    //   Checkpoint - written only by the Save button, restored by the Load button.
    public enum LayoutSlot
    {
        Session,
        Checkpoint
    }

    // Where slots are kept (files on device; an in-memory fake in tests). Implementations must not throw.
    public interface ILayoutStorage
    {
        bool Exists(LayoutSlot slot);

        // False when the slot has nothing saved or cannot be read.
        bool TryRead(LayoutSlot slot, out string json);

        bool Write(LayoutSlot slot, string json);

        bool Delete(LayoutSlot slot);
    }

    public enum LayoutRestoreOutcome
    {
        Restored,
        NoneSaved,
        Unreadable
    }

    public sealed class LayoutRestore
    {
        private static readonly LayoutFit EmptyFit = new LayoutFit(Array.Empty<PlacedItem>(), Array.Empty<PlacedItem>());

        public LayoutRestoreOutcome Outcome { get; }

        // Items to place (re-validated against the current room) and items that no longer fit. Empty unless Restored.
        public LayoutFit Fit { get; }

        public int DroppedUnknownCount { get; }
        public int DroppedInvalidCount { get; }
        public string Error { get; }

        private LayoutRestore(LayoutRestoreOutcome outcome, LayoutFit fit, int droppedUnknown, int droppedInvalid, string error)
        {
            Outcome = outcome;
            Fit = fit;
            DroppedUnknownCount = droppedUnknown;
            DroppedInvalidCount = droppedInvalid;
            Error = error;
        }

        internal static LayoutRestore Restored(LayoutFit fit, LayoutLoadResult file) =>
            new LayoutRestore(LayoutRestoreOutcome.Restored, fit, file.DroppedUnknownCount, file.DroppedInvalidCount, null);

        internal static LayoutRestore NoneSaved() => new LayoutRestore(LayoutRestoreOutcome.NoneSaved, EmptyFit, 0, 0, null);

        internal static LayoutRestore Unreadable(string error) => new LayoutRestore(LayoutRestoreOutcome.Unreadable, EmptyFit, 0, 0, error);
    }

    // The save/load rules on top of a storage: what each button and the autosave read and write.
    // Clearing the room clears only the session; the checkpoint survives until the next Save.
    public sealed class LayoutSlots
    {
        private readonly ILayoutStorage storage;

        public LayoutSlots(ILayoutStorage storage)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public bool HasCheckpoint => storage.Exists(LayoutSlot.Checkpoint);

        public bool SaveSession(IReadOnlyList<PlacedItem> items) => storage.Write(LayoutSlot.Session, LayoutSerializer.Serialize(items));

        public bool SaveCheckpoint(IReadOnlyList<PlacedItem> items) => storage.Write(LayoutSlot.Checkpoint, LayoutSerializer.Serialize(items));

        // Clear All: the session becomes an empty layout (so a restart resumes an empty room); the checkpoint is untouched.
        public bool ClearSession() => SaveSession(Array.Empty<PlacedItem>());

        public LayoutRestore RestoreSession(RoomFootprint room) => Restore(LayoutSlot.Session, room);

        public LayoutRestore RestoreCheckpoint(RoomFootprint room) => Restore(LayoutSlot.Checkpoint, room);

        private LayoutRestore Restore(LayoutSlot slot, RoomFootprint room)
        {
            if (room == null)
            {
                throw new ArgumentNullException(nameof(room));
            }

            if (!storage.Exists(slot))
            {
                return LayoutRestore.NoneSaved();
            }

            if (!storage.TryRead(slot, out string json))
            {
                return LayoutRestore.Unreadable($"The {slot} layout could not be read.");
            }

            LayoutLoadResult file = LayoutSerializer.Deserialize(json);
            if (file.HasError)
            {
                return LayoutRestore.Unreadable(file.Error);
            }

            return LayoutRestore.Restored(LayoutRevalidator.Fit(room, file.Items, PlacementValidator.WallMargin), file);
        }
    }
}
