using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Presentation.Selection
{
    public sealed class ItemInfo
    {
        /// <param name="displayName">The catalog's content name for the item, not text composed for the UI.</param>
        /// <param name="nextItemId">What merging two of these produces; null at the last level of the chain.</param>
        public ItemInfo(
            GridPosition position,
            ItemId itemId,
            string displayName,
            int maxLevel,
            bool isLocked,
            ItemId? nextItemId,
            bool canDiscard)
        {
            Position = position;
            ItemId = itemId;
            DisplayName = displayName;
            MaxLevel = maxLevel;
            IsLocked = isLocked;
            NextItemId = nextItemId;
            CanDiscard = canDiscard;
        }

        public GridPosition Position { get; }
        public ItemId ItemId { get; }
        public string DisplayName { get; }
        public int Level => ItemId.Level;

        /// <summary>The last level of the item's chain.</summary>
        public int MaxLevel { get; }

        public bool IsMaxLevel => Level == MaxLevel;
        public bool IsLocked { get; }
        public ItemId? NextItemId { get; }
        public bool CanDiscard { get; }

        public override string ToString() => $"{ItemId} at {Position}";
    }
}
