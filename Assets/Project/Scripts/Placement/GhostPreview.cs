using System.Collections.Generic;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Placement
{
    // Shows where the selected item would go. Casts the ray from rayOrigin (the right controller anchor)
    // onto the room's floor plane, snaps the hit to a 5 cm grid in the room's own frame, validates it and
    // shows the visual green (valid) or red (invalid). Driven by PlacementController; does not allocate
    // per frame (the candidate is a struct, materials are only swapped when validity changes).
    public class GhostPreview : MonoBehaviour
    {
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private Transform visual;
        [SerializeField] private Renderer visualRenderer;
        [SerializeField] private Material validMaterial;
        [SerializeField] private Material invalidMaterial;
        [SerializeField] private float maxRayDistance = 10f;

        private FurnitureDefinition shownDefinition;
        private bool? shownValid;

        public bool HasCandidate { get; private set; }
        public PlacedItem Candidate { get; private set; }
        public PlacementResult Result { get; private set; }

        public void Hide()
        {
            HasCandidate = false;
            if (visual != null && visual.gameObject.activeSelf)
            {
                visual.gameObject.SetActive(false);
            }
        }

        // movingItemId: the Id of a placed item being moved, so validation ignores that item's old spot; null when placing.
        public void UpdatePreview(RoomFootprint room, FurnitureDefinition definition, float yawDegrees, IReadOnlyList<PlacedItem> placedItems,
            string movingItemId = null)
        {
            FloorFrame frame = room.Frame;
            if (rayOrigin == null
                || !FloorRay.TryHit(rayOrigin.position, rayOrigin.forward, frame.Height, out Vector3 hit, maxRayDistance))
            {
                Hide();
                return;
            }

            Vector2 local = GridSnapper.Snap(frame.ToLocal(hit.x, hit.z), GridSnapper.PositionStep);
            var candidate = new PlacedItem(definition, local.x, local.y, yawDegrees, movingItemId);
            PlacementResult result = PlacementValidator.Validate(room, candidate, placedItems, PlacementValidator.WallMargin);

            Candidate = candidate;
            Result = result;
            HasCandidate = true;

            Vector2 world = frame.ToWorld(local);
            transform.SetPositionAndRotation(
                new Vector3(world.x, frame.Height, world.y),
                Quaternion.Euler(0f, frame.ToWorldYaw(yawDegrees), 0f));

            if (shownDefinition != definition)
            {
                // The visual is a unit cube; scale it to the item and lift it so it stands on the floor.
                visual.localScale = new Vector3(definition.Width, definition.Height, definition.Depth);
                visual.localPosition = new Vector3(0f, definition.Height / 2f, 0f);
                shownDefinition = definition;
            }

            if (shownValid != result.IsValid)
            {
                visualRenderer.sharedMaterial = result.IsValid ? validMaterial : invalidMaterial;
                shownValid = result.IsValid;
            }

            if (!visual.gameObject.activeSelf)
            {
                visual.gameObject.SetActive(true);
            }
        }
    }
}
