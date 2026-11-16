using System;
using MergeGame.Core.Items;

namespace MergeGame.Presentation.Tests.Support
{
    /// <summary>An item as <see cref="FakeBoardView"/> currently shows it. A hidden item shows no item id.</summary>
    internal readonly struct ShownItem : IEquatable<ShownItem>
    {
        public ShownItem(ItemId itemId, bool isLocked)
            : this(itemId, isLocked, false)
        {
        }

        private ShownItem(ItemId itemId, bool isLocked, bool isHidden)
        {
            ItemId = itemId;
            IsLocked = isLocked;
            IsHidden = isHidden;
        }

        public static ShownItem Box => new ShownItem(default, true, true);

        public ItemId ItemId { get; }
        public bool IsLocked { get; }
        public bool IsHidden { get; }

        public bool Equals(ShownItem other) => ItemId == other.ItemId && IsLocked == other.IsLocked && IsHidden == other.IsHidden;
        public override bool Equals(object obj) => obj is ShownItem other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ItemId, IsLocked, IsHidden);
        public override string ToString() => IsHidden ? "box" : IsLocked ? $"{ItemId} (locked)" : ItemId.ToString();
    }
}
