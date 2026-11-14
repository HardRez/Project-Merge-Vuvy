using System;
using MergeGame.Core.Items;

namespace MergeGame.Presentation.Tests.Support
{
    /// <summary>An item as <see cref="FakeBoardView"/> currently shows it.</summary>
    internal readonly struct ShownItem : IEquatable<ShownItem>
    {
        public ShownItem(ItemId itemId, bool isLocked)
        {
            ItemId = itemId;
            IsLocked = isLocked;
        }

        public ItemId ItemId { get; }
        public bool IsLocked { get; }

        public bool Equals(ShownItem other) => ItemId == other.ItemId && IsLocked == other.IsLocked;
        public override bool Equals(object obj) => obj is ShownItem other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ItemId, IsLocked);
        public override string ToString() => IsLocked ? $"{ItemId} (locked)" : ItemId.ToString();
    }
}
