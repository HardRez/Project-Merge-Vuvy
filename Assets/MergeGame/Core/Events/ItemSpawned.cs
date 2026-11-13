using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Events
{
    public sealed class ItemSpawned : GameEvent
    {
        public ItemSpawned(GridPosition position, ItemId itemId)
        {
            Position = position;
            ItemId = itemId;
        }

        public GridPosition Position { get; }
        public ItemId ItemId { get; }

        public override string ToString() => $"{nameof(ItemSpawned)} {ItemId} at {Position}";
    }
}
