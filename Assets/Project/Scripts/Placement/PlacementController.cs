using System.Collections.Generic;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Placement
{
    // Holds the selected item, its yaw and the items placed so far, drives the ghost every frame and
    // spawns the prefab when placement is confirmed. Does nothing until the room has loaded.
    // Inputs arrive as UnityEvents wired in the scene:
    //   RoomLoader.RoomLoaded -> OnRoomLoaded, CatalogPanel.ItemSelected -> Select,
    //   ControllerPlacementInput: PlacePressed -> Place, RotateStepped -> Rotate, PointingAtUiChanged -> SetPointingAtUi.
    public class PlacementController : MonoBehaviour
    {
        [SerializeField] private GhostPreview ghost;
        [SerializeField] private FurniturePrefabLibrary library;
        [SerializeField] private Transform placedItemsParent;

        private readonly List<PlacedItem> placedItems = new List<PlacedItem>();
        private RoomFootprint room;
        private FurnitureDefinition selected;
        private float yawDegrees;
        private bool pointingAtUi;

        public IReadOnlyList<PlacedItem> PlacedItems => placedItems;
        public FurnitureDefinition Selected => selected;
        public float YawDegrees => yawDegrees;
        public bool IsRoomReady => room != null;

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

            PlacedItem item = ghost.Candidate.WithId(PlacedItem.NewId());
            if (!library.TryGetPrefab(item.Definition.Id, out GameObject prefab))
            {
                Debug.LogWarning($"{nameof(PlacementController)}: no prefab for '{item.Definition.Id}'.");
                return false;
            }

            placedItems.Add(item);

            FloorFrame frame = room.Frame;
            Vector2 world = frame.ToWorld(new Vector2(item.X, item.Z));
            GameObject instance = Instantiate(prefab,
                new Vector3(world.x, frame.Height, world.y),
                Quaternion.Euler(0f, frame.ToWorldYaw(item.YawDegrees), 0f),
                placedItemsParent);
            instance.name = $"{item.Definition.DisplayName} {placedItems.Count}";
            return true;
        }

        private void Update()
        {
            if (!IsRoomReady || selected == null || pointingAtUi)
            {
                ghost.Hide();
                return;
            }

            ghost.UpdatePreview(room, selected, yawDegrees, placedItems);
        }
    }
}
