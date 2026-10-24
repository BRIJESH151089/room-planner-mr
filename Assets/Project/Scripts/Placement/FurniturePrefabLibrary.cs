using System;
using UnityEngine;

namespace RoomPlanner.Placement
{
    // Maps catalog ids (FurnitureCatalog) to prefabs. Each prefab's root sits on the floor at the item's
    // centre, with Width along local x and Depth along local z, matching FurnitureDefinition.
    public class FurniturePrefabLibrary : MonoBehaviour
    {
        [Serializable]
        private struct Entry
        {
            public string id;
            public GameObject prefab;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public bool TryGetPrefab(string id, out GameObject prefab)
        {
            foreach (Entry entry in entries)
            {
                if (string.Equals(entry.id, id, StringComparison.Ordinal) && entry.prefab != null)
                {
                    prefab = entry.prefab;
                    return true;
                }
            }

            prefab = null;
            return false;
        }
    }
}
