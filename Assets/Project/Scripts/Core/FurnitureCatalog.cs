using System;
using System.Collections.Generic;

namespace RoomPlanner.Core
{
    public static class FurnitureCatalog
    {
        public const string BedId = "bed";
        public const string SofaId = "sofa";
        public const string TableId = "table";
        public const string ChairId = "chair";
        public const string WardrobeId = "wardrobe";

        // Typical real-world sizes in metres (width x depth x height).
        public static IReadOnlyList<FurnitureDefinition> Default { get; } = new[]
        {
            new FurnitureDefinition(BedId, "Bed", 1.6f, 2.0f, 0.5f),
            new FurnitureDefinition(SofaId, "Sofa", 2.0f, 0.9f, 0.85f),
            new FurnitureDefinition(TableId, "Table", 1.2f, 0.8f, 0.75f),
            new FurnitureDefinition(ChairId, "Chair", 0.5f, 0.5f, 0.9f),
            new FurnitureDefinition(WardrobeId, "Wardrobe", 1.0f, 0.6f, 2.0f)
        };

        public static bool TryGet(string id, out FurnitureDefinition definition)
        {
            foreach (FurnitureDefinition candidate in Default)
            {
                if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public static FurnitureDefinition Get(string id)
        {
            if (!TryGet(id, out FurnitureDefinition definition))
            {
                throw new KeyNotFoundException($"No furniture with id '{id}'.");
            }

            return definition;
        }
    }
}
