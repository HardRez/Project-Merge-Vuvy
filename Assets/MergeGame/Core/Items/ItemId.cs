using System;

namespace MergeGame.Core.Items
{
    /// <summary>Identifies an item kind: a chain plus a 1-based level within that chain.</summary>
    public readonly struct ItemId : IEquatable<ItemId>
    {
        public ItemId(string chainId, int level)
        {
            if (string.IsNullOrEmpty(chainId))
                throw new ArgumentException("Chain id must not be empty.", nameof(chainId));
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level), level, "Item levels start at 1.");

            ChainId = chainId;
            Level = level;
        }

        public string ChainId { get; }
        public int Level { get; }

        public ItemId NextLevel() => new ItemId(ChainId, Level + 1);

        public bool IsSameChain(ItemId other) => string.Equals(ChainId, other.ChainId, StringComparison.Ordinal);

        public bool Equals(ItemId other) => Level == other.Level && IsSameChain(other);
        public override bool Equals(object obj) => obj is ItemId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ChainId, Level);
        public override string ToString() => $"{ChainId}:{Level}";

        public static bool operator ==(ItemId left, ItemId right) => left.Equals(right);
        public static bool operator !=(ItemId left, ItemId right) => !left.Equals(right);
    }
}
