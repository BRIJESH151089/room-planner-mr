using RoomPlanner.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RoomPlanner.Placement
{
    // Connects the saved layout file (LayoutStore) to the placed furniture (PlacementController).
    // After the room has loaded successfully (RoomLoader.RoomLoaded -> OnRoomLoaded) it loads the saved layout once,
    // re-checked against the current room: items that no longer fit are skipped and counted in the status message.
    // The file is not rewritten by loading, so skipped items stay in it until the next save.
    // StatusChanged carries short messages from LayoutStatusFormatter for the measurements panel.
    public class LayoutPersistence : MonoBehaviour
    {
        [SerializeField] private PlacementController placement;
        [SerializeField] private LayoutStore store;
        [SerializeField] private UnityEvent<string> statusChanged = new UnityEvent<string>();

        private bool autoLoadPending;
        private bool autoLoadDone;

        public UnityEvent<string> StatusChanged => statusChanged;
        public string LastStatus { get; private set; }

        public void OnRoomLoaded()
        {
            if (!autoLoadDone)
            {
                autoLoadPending = true;
            }
        }

        // Loads the saved layout, replacing what is placed now.
        public void LoadFromDisk()
        {
            if (!placement.IsRoomReady)
            {
                return;
            }

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
        }

        private void Report(string message)
        {
            LastStatus = message;
            Debug.Log($"{nameof(LayoutPersistence)}: {message}");
            statusChanged.Invoke(message);
        }
    }
}
