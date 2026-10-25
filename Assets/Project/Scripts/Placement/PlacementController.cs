using System;
using System.Collections.Generic;
using RoomPlanner.Core;
using UnityEngine;
using UnityEngine.Events;

namespace RoomPlanner.Placement
{
    // Coordinates furniture editing and mirrors the Core item list in the scene. Does nothing until the room has loaded.
    //   Placing: a catalog item is selected; the ghost follows the ray and the trigger places it.
    //   Idle: no catalog item selected; the item the ray points at is highlighted and the trigger picks it up.
    //   Moving: an item is held; its prefab is hidden, the ghost shows the new spot, the trigger drops it if valid.
    // Every change to the item list goes through PlacementHistory, and each LayoutChange it returns is applied
    // to the scene by ApplyChange (spawn, destroy or move one prefab instance, found by item Id).
    // Inputs arrive as UnityEvents wired in the scene:
    //   RoomLoader.RoomLoaded -> OnRoomLoaded, CatalogPanel.ItemSelected -> Select, CatalogPanel.SelectionCleared -> ClearSelection,
    //   ControllerPlacementInput: TriggerPressed -> OnTrigger, GripPressed -> Cancel, DeletePressed -> Delete,
    //   UndoPressed -> Undo, RedoPressed -> Redo, RotateStepped -> Rotate, PointingAtUiChanged -> SetPointingAtUi.
    public class PlacementController : MonoBehaviour
    {
        [SerializeField] private GhostPreview ghost;
        [SerializeField] private SelectionController selection;
        [SerializeField] private FurniturePrefabLibrary library;
        [SerializeField] private Transform placedItemsParent;

        [Tooltip("Raised when placing is cancelled with the grip, so the catalog can clear its highlight.")]
        [SerializeField] private UnityEvent placingCancelled = new UnityEvent();

        [Tooltip("Raised after every place, move, delete, undo and redo (not after loading or clearing a whole layout).")]
        [SerializeField] private UnityEvent layoutEdited = new UnityEvent();

        [Tooltip("Raised with fresh measurements whenever the room or the placed items change (never per frame).")]
        [SerializeField] private UnityEvent<RoomMetrics> metricsChanged = new UnityEvent<RoomMetrics>();

        private readonly List<PlacedItem> placedItems = new List<PlacedItem>();
        private readonly Dictionary<string, GameObject> instances = new Dictionary<string, GameObject>();
        private PlacementHistory history;
        private RoomFootprint room;
        private FurnitureDefinition selected;
        private float yawDegrees;
        private bool pointingAtUi;
        private string hoveredId;
        private string movingId;
        private PlacedItem movingOriginal;
        private Vector2 movingGrabOffset;

        public IReadOnlyList<PlacedItem> PlacedItems => placedItems;
        public PlacementHistory History => history;
        public FurnitureDefinition Selected => selected;
        public float YawDegrees => yawDegrees;
        public bool IsRoomReady => room != null;
        public RoomFootprint Room => room;
        public string HoveredId => hoveredId;
        public bool IsMoving => movingId != null;
        public string MovingId => movingId;
        public UnityEvent PlacingCancelled => placingCancelled;
        public UnityEvent LayoutEdited => layoutEdited;
        public UnityEvent<RoomMetrics> MetricsChanged => metricsChanged;
        public RoomMetrics Metrics { get; private set; }

        private void Awake()
        {
            history = new PlacementHistory(placedItems);
        }

        public void OnRoomLoaded(RoomFootprint loadedRoom)
        {
            room = loadedRoom;
            RaiseMetrics();
        }

        public void Select(string id)
        {
            if (!FurnitureCatalog.TryGet(id, out FurnitureDefinition definition))
            {
                Debug.LogWarning($"{nameof(PlacementController)}: unknown furniture id '{id}'.");
                return;
            }

            CancelMove();
            selected = definition;
        }

        public void ClearSelection()
        {
            selected = null;
        }

        // +1 turns clockwise seen from above, -1 counter-clockwise, in 15 degree steps.
        public void Rotate(int steps)
        {
            if (selected != null || IsMoving)
            {
                yawDegrees = YawStepper.Step(yawDegrees, steps);
            }
        }

        public void SetPointingAtUi(bool value)
        {
            pointingAtUi = value;
        }

        // Right trigger: drop the held item, place the selected catalog item, or pick up the pointed item.
        public void OnTrigger()
        {
            if (!IsRoomReady || pointingAtUi)
            {
                return;
            }

            if (IsMoving)
            {
                TryDrop();
            }
            else if (selected != null)
            {
                TryPlace();
            }
            else if (hoveredId != null)
            {
                PickUp(hoveredId);
            }
        }

        // Right grip: put a held item back where it was, or stop placing.
        public void Cancel()
        {
            if (IsMoving)
            {
                CancelMove();
            }
            else if (selected != null)
            {
                selected = null;
                placingCancelled.Invoke();
            }
        }

        // B: delete the held item, or the highlighted one.
        public void Delete()
        {
            if (!IsRoomReady || pointingAtUi)
            {
                return;
            }

            string id = IsMoving ? movingId : hoveredId;
            if (id == null)
            {
                return;
            }

            if (IsMoving)
            {
                EndMove();
            }

            ApplyEdit(history.Remove(id));
        }

        // X: undo the last place, move or delete. While an item is held, X only puts it back (cancels the move).
        public void Undo()
        {
            if (IsMoving)
            {
                CancelMove();
                return;
            }

            if (history.TryUndo(out LayoutChange change))
            {
                ApplyEdit(change);
            }
        }

        // Y: redo. Ignored while an item is held.
        public void Redo()
        {
            if (!IsMoving && history.TryRedo(out LayoutChange change))
            {
                ApplyEdit(change);
            }
        }

        public bool TryPlace()
        {
            if (!IsRoomReady || selected == null || pointingAtUi || !ghost.HasCandidate || !ghost.Result.IsValid)
            {
                return false;
            }

            ApplyEdit(history.Place(ghost.Candidate.WithId(PlacedItem.NewId())));
            return true;
        }

        public bool PickUp(string id)
        {
            int index = PlacementHistory.IndexOf(placedItems, id);
            if (index < 0 || !instances.TryGetValue(id, out GameObject instance))
            {
                return false;
            }

            selection.SetHighlighted(null);
            movingOriginal = placedItems[index];
            movingId = id;

            // Keep the grabbed spot under the pointer instead of snapping the item's centre to it.
            movingGrabOffset = selection.TryGetGrabPoint(room, movingOriginal, out Vector2 grabPoint)
                ? GrabOffset.FromGrabPoint(movingOriginal, grabPoint)
                : Vector2.zero;
            yawDegrees = movingOriginal.YawDegrees;
            instance.SetActive(false);
            return true;
        }

        public bool TryDrop()
        {
            if (!IsMoving || !ghost.HasCandidate || !ghost.Result.IsValid)
            {
                return false;
            }

            PlacedItem target = ghost.Candidate;
            string id = movingId;
            EndMove();

            // Dropping exactly where it was is not an edit worth an undo step.
            if (target.X != movingOriginal.X || target.Z != movingOriginal.Z || target.YawDegrees != movingOriginal.YawDegrees)
            {
                ApplyEdit(history.Move(id, target.X, target.Z, target.YawDegrees));
            }

            return true;
        }

        public void CancelMove()
        {
            if (IsMoving)
            {
                EndMove();
            }
        }

        public bool TryGetInstance(string id, out GameObject instance) => instances.TryGetValue(id, out instance);

        // Replaces everything with a saved layout, re-checked against the current room (see LayoutRevalidator):
        // items that no longer fit are skipped and reported in the result. Undo history starts fresh.
        public LayoutFit ReplaceLayout(IReadOnlyList<PlacedItem> items)
        {
            CancelMove();
            selection.SetHighlighted(null);
            foreach (GameObject instance in instances.Values)
            {
                Destroy(instance);
            }

            instances.Clear();
            placedItems.Clear();
            history.Clear();

            LayoutFit fit = IsRoomReady
                ? LayoutRevalidator.Fit(room, items, PlacementValidator.WallMargin)
                : new LayoutFit(Array.Empty<PlacedItem>(), items);
            foreach (PlacedItem item in fit.Accepted)
            {
                placedItems.Add(item);
                ApplyChange(new LayoutChange(LayoutChangeKind.Added, item));
            }

            RaiseMetrics();
            return fit;
        }

        public void ClearAll()
        {
            ReplaceLayout(Array.Empty<PlacedItem>());
        }

        private void EndMove()
        {
            if (instances.TryGetValue(movingId, out GameObject instance))
            {
                instance.SetActive(true);
            }

            movingId = null;
            ghost.Hide();
        }

        private void ApplyEdit(LayoutChange change)
        {
            ApplyChange(change);
            RaiseMetrics();
            layoutEdited.Invoke();
        }

        private void RaiseMetrics()
        {
            if (IsRoomReady)
            {
                Metrics = RoomMetrics.Compute(room, placedItems);
                metricsChanged.Invoke(Metrics);
            }
        }

        private void ApplyChange(LayoutChange change)
        {
            PlacedItem item = change.Item;
            switch (change.Kind)
            {
                case LayoutChangeKind.Added:
                    if (!library.TryGetPrefab(item.Definition.Id, out GameObject prefab))
                    {
                        Debug.LogWarning($"{nameof(PlacementController)}: no prefab for '{item.Definition.Id}'.");
                        return;
                    }

                    GameObject instance = Instantiate(prefab, WorldPosition(item), WorldRotation(item), placedItemsParent);
                    instance.name = $"{item.Definition.DisplayName} {item.Id.Substring(0, 6)}";
                    instances[item.Id] = instance;
                    break;

                case LayoutChangeKind.Removed:
                    if (instances.TryGetValue(item.Id, out GameObject removed))
                    {
                        if (selection.Highlighted == removed)
                        {
                            selection.SetHighlighted(null);
                        }

                        instances.Remove(item.Id);
                        Destroy(removed);
                    }

                    break;

                case LayoutChangeKind.Moved:
                    if (instances.TryGetValue(item.Id, out GameObject moved))
                    {
                        moved.transform.SetPositionAndRotation(WorldPosition(item), WorldRotation(item));
                    }

                    break;
            }
        }

        private Vector3 WorldPosition(in PlacedItem item)
        {
            FloorFrame frame = room.Frame;
            Vector2 world = frame.ToWorld(new Vector2(item.X, item.Z));
            return new Vector3(world.x, frame.Height, world.y);
        }

        private Quaternion WorldRotation(in PlacedItem item) => Quaternion.Euler(0f, room.Frame.ToWorldYaw(item.YawDegrees), 0f);

        private void Update()
        {
            if (!IsRoomReady || pointingAtUi)
            {
                ghost.Hide();
                SetHovered(-1);
                return;
            }

            if (IsMoving)
            {
                SetHovered(-1);
                ghost.UpdatePreview(room, movingOriginal.Definition, yawDegrees, placedItems, movingId, movingGrabOffset);
                return;
            }

            if (selected != null)
            {
                SetHovered(-1);
                ghost.UpdatePreview(room, selected, yawDegrees, placedItems);
                return;
            }

            ghost.Hide();
            SetHovered(selection.FindPointedItem(room, placedItems));
        }

        private void SetHovered(int index)
        {
            hoveredId = index >= 0 ? placedItems[index].Id : null;
            GameObject instance = null;
            if (hoveredId != null)
            {
                instances.TryGetValue(hoveredId, out instance);
            }

            selection.SetHighlighted(instance);
        }
    }
}
