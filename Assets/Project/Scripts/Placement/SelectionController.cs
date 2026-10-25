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
