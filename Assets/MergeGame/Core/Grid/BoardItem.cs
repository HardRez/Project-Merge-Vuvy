using MergeGame.Core.Items;

namespace MergeGame.Core.Grid
{
    /// <summary>An item instance occupying a cell. Instance state beyond the item kind belongs here.</summary>
    public sealed class BoardItem
    {
        /// <param name="isLocked">
        /// A locked ("webbed") item cannot be dragged, discarded or delivered, but a matching free item can be merged
        /// onto it, which produces an unlocked item.
        /// </param>
        public BoardItem(ItemId itemId, bool isLocked = false)
        {
            ItemId = itemId;
            IsLocked = isLocked;
        }

        public ItemId ItemId { get; }
        public bool IsLocked { get; }

        public override string ToString() => IsLocked ? $"{ItemId} (locked)" : ItemId.ToString();
    }
}
