using MergeGame.Core.Items;

namespace MergeGame.Core.Grid
{
    /// <summary>An item instance occupying a cell. Instance state beyond the item kind belongs here.</summary>
    public sealed class BoardItem
    {
        public BoardItem(ItemId itemId)
        {
            ItemId = itemId;
        }

        public ItemId ItemId { get; }

        public override string ToString() => ItemId.ToString();
    }
}
