using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Events
{
    public sealed class ItemMoved : GameEvent
    {
        public ItemMoved(GridPosition from, GridPosition to, ItemId itemId)
        {
            From = from;
            To = to;
            ItemId = itemId;
        }

        public GridPosition From { get; }
        public GridPosition To { get; }
        public ItemId ItemId { get; }

        public override string ToString() => $"{nameof(ItemMoved)} {ItemId} {From} -> {To}";
    }
}
