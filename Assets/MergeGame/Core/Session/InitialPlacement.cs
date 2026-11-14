using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Session
{
    /// <summary>An item the designer puts on the board before the first move.</summary>
    public readonly struct InitialPlacement
    {
        public InitialPlacement(GridPosition position, ItemId itemId, bool isLocked = false)
        {
            Position = position;
            ItemId = itemId;
            IsLocked = isLocked;
        }

        public GridPosition Position { get; }
        public ItemId ItemId { get; }
        public bool IsLocked { get; }

        public override string ToString() => $"{ItemId}{(IsLocked ? " (locked)" : string.Empty)} at {Position}";
    }
}
