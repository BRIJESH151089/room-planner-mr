using System;
using System.Globalization;

namespace RoomPlanner.Core
{
    public static class FurnitureLabelFormatter
    {
        // Button label, e.g. "Bed  1.6 x 2.0 m" (width x depth, footprint only).
        public static string Format(FurnitureDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return string.Format(CultureInfo.InvariantCulture,
                "{0}  {1:0.0} x {2:0.0} m", definition.DisplayName, definition.Width, definition.Depth);
        }
    }
}
