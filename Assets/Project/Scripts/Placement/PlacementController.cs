using System.Collections.Generic;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Placement
{
    // Coordinates furniture editing and mirrors the Core item list in the scene. Does nothing until the room has loaded.
    //   Placing: a catalog item is selected; the ghost follows the ray and the trigger places it.
    //   Idle: no catalog item selected; the item the ray points at is highlighted.
    // Every change to the item list goes through PlacementHistory, and each LayoutChange it returns is applied
    // to the scene by ApplyChange (spawn, destroy or move one prefab instance, found by item Id).
    // Inputs arrive as UnityEvents wired in the scene:
    //   RoomLoader.RoomLoaded -> OnRoomLoaded, CatalogPanel.ItemSelected -> Select, CatalogPanel.SelectionCleared -> ClearSelection,
    //   ControllerPlacementInput: PlacePressed -> Place, RotateStepped -> Rotate, PointingAtUiChanged -> SetPointingAtUi.
    public class PlacementController : MonoBehaviour
    {
        [SerializeField] private GhostPreview ghost;
        [SerializeField] private SelectionController selection;
        [SerializeField] private FurniturePrefabLibrary library;
        [SerializeField] private Transform placedItemsParent;

        private readonly List<PlacedItem> placedItems = new List<PlacedItem>();
        private readonly Dictionary<string, GameObject> instances = new Dictionary<string, GameObject>();
        private PlacementHistory history;
        private RoomFootprint room;
        private FurnitureDefinition selected;
        private float yawDegrees;
        private bool pointingAtUi;
        private string hoveredId;

        public IReadOnlyList<PlacedItem> PlacedItems => placedItems;
        public PlacementHistory History => history;
        public FurnitureDefinition Selected => selected;
        public float YawDegrees => yawDegrees;
        public bool IsRoomReady => room != null;
        public string HoveredId => hoveredId;

        private void Awake()
        {
            history = new PlacementHistory(placedItems);
        }

        public void OnRoomLoaded(RoomFootprint loadedRoom)
        {
            room = loadedRoom;
        }

        public void Select(string id)
        {
            if (!FurnitureCatalog.TryGet(id, out FurnitureDefinition definition))
            {
                Debug.LogWarning($"{nameof(PlacementController)}: unknown furniture id '{id}'.");
                return;
            }

            selected = definition;
        }

        public void ClearSelection()
        {
            selected = null;
        }

        // +1 turns clockwise seen from above, -1 counter-clockwise, in 15 degree steps.
        public void Rotate(int steps)
        {
            if (selected != null)
            {
                yawDegrees = YawStepper.Step(yawDegrees, steps);
            }
        }

        public void SetPointingAtUi(bool value)
        {
            pointingAtUi = value;
        }

        public void Place()
        {
            TryPlace();
        }

        public bool TryPlace()
        {
            if (!IsRoomReady || selected == null || pointingAtUi || !ghost.HasCandidate || !ghost.Result.IsValid)
            {
                return false;
            }

            ApplyChange(history.Place(ghost.Candidate.WithId(PlacedItem.NewId())));
            return true;
        }

        public bool TryGetInstance(string id, out GameObject instance) => instances.TryGetValue(id, out instance);

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
