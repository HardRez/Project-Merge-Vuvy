using System;
using MergeGame.Core.Items;

namespace MergeGame.Presentation.Orders
{
    /// <summary>One item an order asks for, and whether the board currently holds a free one for it.</summary>
    public readonly struct RequirementState : IEquatable<RequirementState>
    {
        public RequirementState(ItemId itemId, bool isFulfilled)
        {
            ItemId = itemId;
            IsFulfilled = isFulfilled;
        }

        public ItemId ItemId { get; }
        public bool IsFulfilled { get; }

        public bool Equals(RequirementState other) => ItemId == other.ItemId && IsFulfilled == other.IsFulfilled;
        public override bool Equals(object obj) => obj is RequirementState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ItemId, IsFulfilled);
        public override string ToString() => $"{ItemId}{(IsFulfilled ? " (on board)" : string.Empty)}";
    }
}
