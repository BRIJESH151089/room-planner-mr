namespace RoomPlanner.Core
{
    public enum PlacementReason
    {
        Valid,
        OutsideRoom,
        TooCloseToWall,
        OverlapsItem
    }

    public readonly struct PlacementResult
    {
        public PlacementReason Reason { get; }

        // Index into the placed-items list when Reason is OverlapsItem, otherwise -1.
        public int OverlappingItemIndex { get; }

        public bool IsValid => Reason == PlacementReason.Valid;

        public PlacementResult(PlacementReason reason, int overlappingItemIndex = -1)
        {
            Reason = reason;
            OverlappingItemIndex = overlappingItemIndex;
        }
    }
}
