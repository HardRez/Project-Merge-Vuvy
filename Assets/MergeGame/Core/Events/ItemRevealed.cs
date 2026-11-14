using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Events
{
    /// <summary>The hidden item on <see cref="Position"/> is now a visible, still locked item.</summary>
    public sealed class ItemRevealed : GameEvent
    {
        public ItemRevealed(GridPosition position, ItemId itemId)
        {
            Position = position;
            ItemId = itemId;
        }

        public GridPosition Position { get; }
        public ItemId ItemId { get; }

        public override string ToString() => $"{nameof(ItemRevealed)} {ItemId} at {Position}";
    }
}
