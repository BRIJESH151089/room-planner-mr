using RoomPlanner.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RoomPlanner.Placement
{
    // Connects the two saved layouts (LayoutSlots over LayoutStore) to the placed furniture (PlacementController).
    // Session: written automatically autosaveDelaySeconds after the last edit (PlacementController.LayoutEdited ->
    // OnLayoutEdited), so a burst of edits writes once, plus when the app is paused or quits with a save pending.
    // After the room has loaded (RoomLoader.RoomLoaded -> OnRoomLoaded) the session is resumed once, re-checked
    // against the current room. Checkpoint: written only by the Save button (SaveNow) and restored by the Load
    // button (LoadFromDisk), which replaces what is placed now and starts a fresh undo history.
    // Clear All (ClearAll) empties the floor and the session; the checkpoint is kept.
    // StatusChanged carries short messages from LayoutStatusFormatter for the measurements panel.
    public class LayoutPersistence : MonoBehaviour
    {
        [SerializeField] private PlacementController placement;
        [SerializeField] private LayoutStore store;
        [SerializeField] private float autosaveDelaySeconds = 1f;
        [SerializeField] private UnityEvent<string> statusChanged = new UnityEvent<string>();

        private LayoutSlots slots;
        private bool autoLoadPending;
        private bool autoLoadDone;
        private bool savePending;
        private float saveDueTime;

        public bool IsSavePending => savePending;

        public UnityEvent<string> StatusChanged => statusChanged;
        public string LastStatus { get; private set; }

        private LayoutSlots Slots => slots ?? (slots = new LayoutSlots(store));

        public void OnRoomLoaded()
        {
            if (!autoLoadDone)
            {
                autoLoadPending = true;
            }
        }

        public void OnLayoutEdited()
        {
            savePending = true;
            saveDueTime = Time.unscaledTime + autosaveDelaySeconds;
        }

        // Save button: the current layout becomes the checkpoint.
        public void SaveNow()
        {
            if (!placement.IsRoomReady)
            {
                return;
            }

            Report(Slots.SaveCheckpoint(placement.PlacedItems)
                ? LayoutStatusFormatter.CheckpointSavedText
                : LayoutStatusFormatter.SaveFailedText);
        }

        // Load button: replaces what is placed now with the checkpoint (unsaved edits are discarded).
        public void LoadFromDisk()
        {
            if (!placement.IsRoomReady)
            {
                return;
            }

            LayoutRestore restore = Slots.RestoreCheckpoint(placement.Room);
            switch (restore.Outcome)
            {
                case LayoutRestoreOutcome.NoneSaved:
                    Report(LayoutStatusFormatter.NothingSavedText);
                    return;
                case LayoutRestoreOutcome.Unreadable:
                    Debug.LogWarning($"{nameof(LayoutPersistence)}: {restore.Error}");
                    Report(LayoutStatusFormatter.LoadFailedText);
                    return;
            }

            Apply(restore);
            SaveSession();
            Report(LayoutStatusFormatter.Restored(restore.Fit.Skipped.Count, restore.DroppedUnknownCount, restore.DroppedInvalidCount));
        }

        public void ClearAll()
        {
            if (!placement.IsRoomReady)
            {
                return;
            }

            savePending = false;
            placement.ClearAll();
            Report(Slots.ClearSession() ? LayoutStatusFormatter.ClearedText : LayoutStatusFormatter.SaveFailedText);
        }

        private void ResumeSession()
        {
            LayoutRestore restore = Slots.RestoreSession(placement.Room);
            if (restore.Outcome == LayoutRestoreOutcome.Unreadable)
            {
                Debug.LogWarning($"{nameof(LayoutPersistence)}: {restore.Error}");
                Report(LayoutStatusFormatter.LoadFailedText);
                return;
            }

            if (restore.Outcome == LayoutRestoreOutcome.NoneSaved)
            {
                return;
            }

            Apply(restore);
            int dropped = restore.Fit.Skipped.Count + restore.DroppedUnknownCount + restore.DroppedInvalidCount;
            if (restore.Fit.Accepted.Count > 0 || dropped > 0)
            {
                Report(LayoutStatusFormatter.Loaded(restore.Fit.Accepted.Count, restore.Fit.Skipped.Count, restore.DroppedUnknownCount, restore.DroppedInvalidCount));
            }
        }

        // Places the restored items; skipped ones are logged and stay out of the session from the next save.
        private void Apply(LayoutRestore restore)
        {
            savePending = false;
            placement.ReplaceLayout(restore.Fit.Accepted);
            foreach (PlacedItem skipped in restore.Fit.Skipped)
            {
                Debug.Log($"{nameof(LayoutPersistence)}: '{skipped.Definition.Id}' {skipped.Id} at ({skipped.X:0.00}, {skipped.Z:0.00}) does not fit this room; not placed.");
            }
        }

        // Autosave is silent unless it fails.
        private void SaveSession()
        {
            savePending = false;
            if (!Slots.SaveSession(placement.PlacedItems))
            {
                Report(LayoutStatusFormatter.SaveFailedText);
            }
        }

        private void Update()
        {
            // Wait until the placement controller has the room too (RoomLoaded listeners run in wiring order).
            if (autoLoadPending && placement.IsRoomReady)
            {
                autoLoadPending = false;
                autoLoadDone = true;
                ResumeSession();
            }

            if (savePending && Time.unscaledTime >= saveDueTime && placement.IsRoomReady)
            {
                SaveSession();
            }
        }

        // Quest apps are paused when the headset is taken off or the user goes Home; save what is pending then.
        private void OnApplicationPause(bool paused)
        {
            if (paused && savePending && placement.IsRoomReady)
            {
                SaveSession();
            }
        }

        private void OnApplicationQuit()
        {
            if (savePending && placement.IsRoomReady)
            {
                SaveSession();
            }
        }

        private void Report(string message)
        {
            LastStatus = message;
            Debug.Log($"{nameof(LayoutPersistence)}: {message}");
            statusChanged.Invoke(message);
        }
    }
}
