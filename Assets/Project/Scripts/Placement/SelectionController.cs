using System.Collections.Generic;
using RoomPlanner.Core;
using UnityEngine;

namespace RoomPlanner.Placement
{
    // Finds the placed item the right controller points at (RayVsOrientedBox against each item's box, so the
    // prefabs need no colliders) and highlights it by swapping its renderers to the highlight material.
    // Driven by PlacementController. Per frame it only does the ray test; renderer lists and material swaps
    // happen when the highlighted item changes, into reused lists, so there are no per-frame allocations.
    public class SelectionController : MonoBehaviour
    {
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private float maxRayDistance = 10f;

        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Material> originalMaterials = new List<Material>();
        private GameObject highlighted;

        public GameObject Highlighted => highlighted;

        // Index into items of the nearest item hit by the controller ray, or -1.
        public int FindPointedItem(RoomFootprint room, IReadOnlyList<PlacedItem> items)
        {
            if (rayOrigin == null || items.Count == 0)
            {
                return -1;
            }

            return RayVsOrientedBox.FindNearest(rayOrigin.position, rayOrigin.forward, room.Frame, items, maxRayDistance, out _);
        }

        // Where the controller ray grabs this item, on the floor plane in the room frame (see GrabOffset.ChooseGrabPoint).
        public bool TryGetGrabPoint(RoomFootprint room, in PlacedItem item, out Vector2 grabPoint)
        {
            grabPoint = new Vector2(item.X, item.Z);
            if (rayOrigin == null)
            {
                return false;
            }

            FloorFrame frame = room.Frame;
            Vector3 origin = rayOrigin.position;
            Vector3 direction = rayOrigin.forward;

            bool hasFloorHit = FloorRay.TryHit(origin, direction, frame.Height, out Vector3 floorWorld, maxRayDistance);
            Vector2 floorHit = hasFloorHit ? frame.ToLocal(floorWorld.x, floorWorld.z) : default;

            // Box hit in the room frame (RayVsOrientedBox works in that frame, with y measured from the floor).
            Vector2 planarOrigin = frame.ToLocal(origin.x, origin.z);
            Vector2 planarDirection = PlanarRotation.InverseRotate(new Vector2(direction.x, direction.z), frame.YawDegrees);
            var localOrigin = new Vector3(planarOrigin.x, origin.y - frame.Height, planarOrigin.y);
            var localDirection = new Vector3(planarDirection.x, direction.y, planarDirection.y);
            Vector2 boxHit = grabPoint;
            if (RayVsOrientedBox.TryIntersect(localOrigin, localDirection, item.Footprint, item.Definition.Height, out float distance, maxRayDistance))
            {
                Vector3 hit = localOrigin + localDirection.normalized * distance;
                boxHit = new Vector2(hit.x, hit.z);
            }

            grabPoint = GrabOffset.ChooseGrabPoint(item, hasFloorHit, floorHit, boxHit);
            return true;
        }

        // Pass null to clear the highlight.
        public void SetHighlighted(GameObject instance)
        {
            if (instance == highlighted)
            {
                return;
            }

            RestoreMaterials();
            highlighted = instance;
            if (instance == null)
            {
                return;
            }

            instance.GetComponentsInChildren(renderers);
            for (int i = 0; i < renderers.Count; i++)
            {
                originalMaterials.Add(renderers[i].sharedMaterial);
                renderers[i].sharedMaterial = highlightMaterial;
            }
        }

        private void RestoreMaterials()
        {
            // The instance may already be destroyed (deleted); destroyed renderers compare equal to null.
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sharedMaterial = originalMaterials[i];
                }
            }

            renderers.Clear();
            originalMaterials.Clear();
        }
    }
}
