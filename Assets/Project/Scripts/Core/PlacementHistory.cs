using System;
using System.Collections.Generic;

namespace RoomPlanner.Core
{
    public enum LayoutChangeKind
    {
        Added,
        Removed,
        Moved
    }

    // What a command did to the item list, so the scene can mirror it (spawn, destroy or move one object).
    public readonly struct LayoutChange
    {
        public LayoutChangeKind Kind { get; }
        public PlacedItem Item { get; }

        public LayoutChange(LayoutChangeKind kind, PlacedItem item)
        {
            Kind = kind;
            Item = item;
        }
    }

    public interface IPlacementCommand
    {
        LayoutChange Apply(List<PlacedItem> items);
        LayoutChange Revert(List<PlacedItem> items);
    }

    public sealed class PlaceCommand : IPlacementCommand
    {
        private readonly PlacedItem item;

        public PlaceCommand(PlacedItem item)
        {
            this.item = PlacementHistory.RequireId(item);
        }

        public LayoutChange Apply(List<PlacedItem> items)
        {
            items.Add(item);
            return new LayoutChange(LayoutChangeKind.Added, item);
        }

        public LayoutChange Revert(List<PlacedItem> items)
        {
            items.RemoveAt(PlacementHistory.IndexOfRequired(items, item.Id));
            return new LayoutChange(LayoutChangeKind.Removed, item);
        }
    }

    public sealed class RemoveCommand : IPlacementCommand
    {
        private readonly string id;
        private PlacedItem removed;
        private int index = -1;

        public RemoveCommand(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id must not be empty.", nameof(id));
            }

            this.id = id;
        }

        public LayoutChange Apply(List<PlacedItem> items)
        {
            index = PlacementHistory.IndexOfRequired(items, id);
            removed = items[index];
            items.RemoveAt(index);
            return new LayoutChange(LayoutChangeKind.Removed, removed);
        }

        // Puts the item back at the same position in the list, with the same Id.
        public LayoutChange Revert(List<PlacedItem> items)
        {
            items.Insert(Math.Min(index, items.Count), removed);
            return new LayoutChange(LayoutChangeKind.Added, removed);
        }
    }

    public sealed class MoveCommand : IPlacementCommand
    {
        private readonly PlacedItem before;
        private readonly PlacedItem after;

        public MoveCommand(PlacedItem before, PlacedItem after)
        {
            PlacementHistory.RequireId(before);
            if (!string.Equals(before.Id, after.Id, StringComparison.Ordinal))
            {
                throw new ArgumentException("A move must keep the item's Id.", nameof(after));
            }

            this.before = before;
            this.after = after;
        }

        public LayoutChange Apply(List<PlacedItem> items) => Replace(items, after);

        public LayoutChange Revert(List<PlacedItem> items) => Replace(items, before);

        private static LayoutChange Replace(List<PlacedItem> items, PlacedItem pose)
        {
            items[PlacementHistory.IndexOfRequired(items, pose.Id)] = pose;
            return new LayoutChange(LayoutChangeKind.Moved, pose);
        }
    }

    // Undo/redo for furniture edits. Every edit goes through Execute (or Place/Remove/Move), which applies it to
    // the item list and records it; a new edit clears the redo stack. At most MaxEntries edits can be undone;
    // older ones are dropped.
    public sealed class PlacementHistory
    {
        public const int MaxEntries = 50;

        private readonly List<PlacedItem> items;
        private readonly List<IPlacementCommand> undo = new List<IPlacementCommand>();
        private readonly List<IPlacementCommand> redo = new List<IPlacementCommand>();

        public PlacementHistory(List<PlacedItem> items)
        {
            this.items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public IReadOnlyList<PlacedItem> Items => items;
        public int UndoCount => undo.Count;
        public int RedoCount => redo.Count;
        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;

        public LayoutChange Place(PlacedItem item) => Execute(new PlaceCommand(item));

        public LayoutChange Remove(string id) => Execute(new RemoveCommand(id));

        public LayoutChange Move(string id, float x, float z, float yawDegrees)
        {
            PlacedItem before = items[IndexOfRequired(items, id)];
            return Execute(new MoveCommand(before, before.MovedTo(x, z, yawDegrees)));
        }

        public LayoutChange Execute(IPlacementCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            LayoutChange change = command.Apply(items);
            undo.Add(command);
            if (undo.Count > MaxEntries)
            {
                undo.RemoveAt(0);
            }

            redo.Clear();
            return change;
        }

        // Forgets all undo and redo steps (used when a whole layout is loaded or cleared). The item list is untouched.
        public void Clear()
        {
            undo.Clear();
            redo.Clear();
        }

        public bool TryUndo(out LayoutChange change)
        {
            change = default;
            if (undo.Count == 0)
            {
                return false;
            }

            IPlacementCommand command = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            change = command.Revert(items);
            redo.Add(command);
            return true;
        }

        public bool TryRedo(out LayoutChange change)
        {
            change = default;
            if (redo.Count == 0)
            {
                return false;
            }

            IPlacementCommand command = redo[redo.Count - 1];
            redo.RemoveAt(redo.Count - 1);
            change = command.Apply(items);
            undo.Add(command);
            return true;
        }

        public static int IndexOf(IReadOnlyList<PlacedItem> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i].Id, id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        internal static int IndexOfRequired(List<PlacedItem> list, string id)
        {
            int index = IndexOf(list, id);
            if (index < 0)
            {
                throw new InvalidOperationException($"No placed item with id '{id}'.");
            }

            return index;
        }

        internal static PlacedItem RequireId(PlacedItem item)
        {
            if (!item.HasId)
            {
                throw new ArgumentException("Only placed items with an Id can be recorded in the history.", nameof(item));
            }

            return item;
        }
    }
}
