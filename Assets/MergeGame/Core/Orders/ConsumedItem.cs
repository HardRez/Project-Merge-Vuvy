using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Orders
{
    /// <summary>A board item that was handed over to a customer, and the cell it was taken from.</summary>
    public readonly struct ConsumedItem
    {
        public ConsumedItem(GridPosition position, ItemId itemId)
        {
            Position = position;
            ItemId = itemId;
        }

        public GridPosition Position { get; }
        public ItemId ItemId { get; }

        public override string ToString() => $"{ItemId} from {Position}";
    }
}
