using System;
using UnityEngine;

namespace RoomPlanner.Core
{
    public class FurnitureItem
    {
        public string Name { get; }
        public Vector2 Size { get; }
        public SurfaceType AllowedSurface { get; }

        public FurnitureItem(string name, Vector2 size, SurfaceType allowedSurface)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name must not be null or empty.", nameof(name));
            }

            if (size.x <= 0f || size.y <= 0f)
            {
                throw new ArgumentException("Size components must be positive.", nameof(size));
            }

            Name = name;
            Size = size;
            AllowedSurface = allowedSurface;
        }
    }
}
