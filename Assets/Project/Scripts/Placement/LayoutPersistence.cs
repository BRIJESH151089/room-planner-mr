using RoomPlanner.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RoomPlanner.Placement
{
    // Connects the saved layout file (LayoutStore) to the placed furniture (PlacementController).
    // After the room has loaded successfully (RoomLoader.RoomLoaded -> OnRoomLoaded) it loads the saved layout once,
    // re-checked against the current room: items that no longer fit are skipped and counted in the status message.
    // The file is not rewritten by loading, so skipped items stay in it until the next save.
    // Saving: automatically autosaveDelaySeconds after the last edit (PlacementController.LayoutEdited -> OnLayoutEdited),
    // so a burst of edits writes once, plus when the app is paused or quits with a save pending; and on demand
    // from the Save button (SaveNow). Clear All (ClearAll) empties the layout and deletes the file.
    // StatusChanged carries short messages from LayoutStatusFormatter for the measurements panel.
    public class LayoutPersistence : MonoBehaviour
    {
        [SerializeField] private PlacementController placement;
        [SerializeField] private LayoutStore store;
        [SerializeField] private float autosaveDelaySeconds = 1f;
        [SerializeField] private UnityEvent<string> statusChanged = new UnityEvent<string>();

        private bool autoLoadPending;
        private bool autoLoadDone;
        private bool savePending;
        private float saveDueTime;

        public bool IsSavePending => savePending;

        public UnityEvent<string> StatusChanged => statusChanged;
        public string LastStatus { get; private set; }

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

        public void SaveNow()
        {
            if (!placement.IsRoomReady)
            {
                return;
            }

            savePending = false;
            Report(store.Save(placement.PlacedItems)
                ? LayoutStatusFormatter.Saved(placement.PlacedItems.Count)
                : LayoutStatusFormatter.SaveFailedText);
        }

        public void ClearAll()
        {
            if (!placement.IsRoomReady)
            {
                return;
            }

            savePending = false;
            placement.ClearAll();
            store.Clear();
            Report(LayoutStatusFormatter.ClearedText);
        }

        // Loads the saved layout, replacing what is placed now (unsaved edits are discarded).
        public void LoadFromDisk()
        {
            if (!placement.IsRoomReady)
            {
                return;
            }

            savePending = false;

            if (!store.TryLoad(out LayoutLoadResult result))
            {
                Report(LayoutStatusFormatter.NothingSavedText);
                return;
            }

            if (result.HasError)
            {
                Debug.LogWarning($"{nameof(LayoutPersistence)}: {result.Error}");
                Report(LayoutStatusFormatter.LoadFailedText);
                return;
            }

            LayoutFit fit = placement.ReplaceLayout(result.Items);
            foreach (PlacedItem skipped in fit.Skipped)
            {
                Debug.Log($"{nameof(LayoutPersistence)}: '{skipped.Definition.Id}' {skipped.Id} at ({skipped.X:0.00}, {skipped.Z:0.00}) does not fit this room; not placed.");
            }

            Report(LayoutStatusFormatter.Loaded(fit.Accepted.Count, fit.Skipped.Count, result.DroppedUnknownCount, result.DroppedInvalidCount));
        }

        private void Update()
        {
            // Wait until the placement controller has the room too (RoomLoaded listeners run in wiring order).
            if (autoLoadPending && placement.IsRoomReady)
            {
                autoLoadPending = false;
                autoLoadDone = true;
                LoadFromDisk();
            }

            if (savePending && Time.unscaledTime >= saveDueTime)
            {
                SaveNow();
            }
        }

        // Quest apps are paused when the headset is taken off or the user goes Home; save what is pending then.
        private void OnApplicationPause(bool paused)
        {
            if (paused && savePending)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            if (savePending)
            {
                SaveNow();
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
