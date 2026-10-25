using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace RoomPlanner.Core
{
    // Result of reading a saved layout. Never null; on any problem Items is empty and HasError is true.
    public sealed class LayoutLoadResult
    {
        public IReadOnlyList<PlacedItem> Items { get; }
        public int DroppedUnknownCount { get; }
        public int DroppedInvalidCount { get; }
        public bool HasError { get; }
        public string Error { get; }

        internal LayoutLoadResult(List<PlacedItem> items, int droppedUnknown, int droppedInvalid)
        {
            Items = items;
            DroppedUnknownCount = droppedUnknown;
            DroppedInvalidCount = droppedInvalid;
        }

        internal LayoutLoadResult(string error)
        {
            Items = Array.Empty<PlacedItem>();
            HasError = true;
            Error = error;
        }
    }

    // Saves placed furniture as human-readable JSON and reads it back exactly.
    //   { "version": 1, "items": [ { "id": "...", "definitionId": "sofa", "posX": 1.25, "posZ": -0.5, "yawDegrees": 90 } ] }
    // Positions are in the room footprint's own frame (see FloorFrame), so a layout follows the room, not the world.
    // Numbers are written in the shortest form that parses back to the same float, so round-trips are exact.
    public static class LayoutSerializer
    {
        public const int SchemaVersion = 1;

        [Serializable]
        private sealed class LayoutFile
        {
            public int version;
            public List<ItemRecord> items;
        }

        [Serializable]
        private sealed class ItemRecord
        {
            public string id;
            public string definitionId;
            public float posX;
            public float posZ;
            public float yawDegrees;
        }

        public static string Serialize(IReadOnlyList<PlacedItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var json = new StringBuilder();
            json.Append("{\n  \"version\": ").Append(SchemaVersion).Append(",\n  \"items\": [");
            for (int i = 0; i < items.Count; i++)
            {
                PlacedItem item = items[i];
                if (!item.HasId)
                {
                    throw new ArgumentException("Only placed items with an Id can be saved.", nameof(items));
                }

                json.Append(i == 0 ? "\n" : ",\n");
                json.Append("    { \"id\": ").Append(Quote(item.Id))
                    .Append(", \"definitionId\": ").Append(Quote(item.Definition.Id))
                    .Append(", \"posX\": ").Append(Number(item.X))
                    .Append(", \"posZ\": ").Append(Number(item.Z))
                    .Append(", \"yawDegrees\": ").Append(Number(item.YawDegrees))
                    .Append(" }");
            }

            json.Append(items.Count == 0 ? "]\n}\n" : "\n  ]\n}\n");
            return json.ToString();
        }

        // Items whose definitionId is not in FurnitureCatalog are dropped (DroppedUnknownCount). Items with a missing
        // or duplicate id or a non-finite number are dropped as invalid (DroppedInvalidCount). Malformed or empty
        // JSON, or an unsupported version, gives an empty layout with HasError set; this never throws.
        public static LayoutLoadResult Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new LayoutLoadResult("The layout file is empty.");
            }

            LayoutFile file;
            try
            {
                file = JsonUtility.FromJson<LayoutFile>(json);
            }
            catch (Exception exception)
            {
                return new LayoutLoadResult("The layout file is not valid JSON: " + exception.Message);
            }

            if (file == null)
            {
                return new LayoutLoadResult("The layout file is not valid JSON.");
            }

            if (file.version < 1 || file.version > SchemaVersion)
            {
                return new LayoutLoadResult($"Unsupported layout version {file.version} (expected 1 to {SchemaVersion}).");
            }

            var items = new List<PlacedItem>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            int unknown = 0;
            int invalid = 0;
            foreach (ItemRecord record in file.items ?? new List<ItemRecord>())
            {
                if (record == null || string.IsNullOrWhiteSpace(record.id) || !seenIds.Add(record.id)
                    || !IsFinite(record.posX) || !IsFinite(record.posZ) || !IsFinite(record.yawDegrees))
                {
                    invalid++;
                    continue;
                }

                if (!FurnitureCatalog.TryGet(record.definitionId, out FurnitureDefinition definition))
                {
                    unknown++;
                    continue;
                }

                items.Add(new PlacedItem(definition, record.posX, record.posZ, record.yawDegrees, record.id));
            }

            return new LayoutLoadResult(items, unknown, invalid);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static string Number(float value)
        {
            string text = value.ToString("R", CultureInfo.InvariantCulture);
            if (float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture) != value)
            {
                text = value.ToString("G9", CultureInfo.InvariantCulture);
            }

            return text;
        }

        private static string Quote(string text)
        {
            var quoted = new StringBuilder(text.Length + 2).Append('"');
            foreach (char c in text)
            {
                switch (c)
                {
                    case '"': quoted.Append("\\\""); break;
                    case '\\': quoted.Append("\\\\"); break;
                    case '\n': quoted.Append("\\n"); break;
                    case '\r': quoted.Append("\\r"); break;
                    case '\t': quoted.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            quoted.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            quoted.Append(c);
                        }

                        break;
                }
            }

            return quoted.Append('"').ToString();
        }
    }
}
