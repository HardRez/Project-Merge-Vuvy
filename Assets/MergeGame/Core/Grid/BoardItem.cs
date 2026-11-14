using System;
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
            : this(itemId, isLocked, false)
        {
        }

        /// <param name="isHidden">
        /// A hidden item (a closed box) is locked and additionally cannot be merged onto. It is revealed as a locked item
        /// when a neighbouring locked item is freed by a merge.
        /// </param>
        /// <exception cref="ArgumentException">The item is hidden but not locked.</exception>
        public BoardItem(ItemId itemId, bool isLocked, bool isHidden)
        {
            if (isHidden && !isLocked)
                throw new ArgumentException($"Hidden item {itemId} must also be locked.", nameof(isLocked));

            ItemId = itemId;
            IsLocked = isLocked;
            IsHidden = isHidden;
        }

        public ItemId ItemId { get; }
        public bool IsLocked { get; }

        /// <summary>Always implies <see cref="IsLocked"/>.</summary>
        public bool IsHidden { get; }

        public static BoardItem Hidden(ItemId itemId) => new BoardItem(itemId, true, true);

        public override string ToString() => IsHidden ? $"{ItemId} (hidden)" : IsLocked ? $"{ItemId} (locked)" : ItemId.ToString();
    }
}
