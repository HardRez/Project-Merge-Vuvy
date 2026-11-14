using System;
using System.Collections.Generic;

namespace MergeGame.Presentation.Orders
{
    /// <summary>Everything an order card shows. Value equality lets the presenter skip refreshes that change nothing.</summary>
    public sealed class OrderCard : IEquatable<OrderCard>
    {
        public OrderCard(int slotIndex, int customerIndex, IReadOnlyList<RequirementState> requirements, int rewardPoints, bool canDeliver)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));

            SlotIndex = slotIndex;
            CustomerIndex = customerIndex;
            Requirements = new List<RequirementState>(requirements).AsReadOnly();
            RewardPoints = rewardPoints;
            CanDeliver = canDeliver;
        }

        public int SlotIndex { get; }
        public int CustomerIndex { get; }
        public IReadOnlyList<RequirementState> Requirements { get; }
        public int RewardPoints { get; }
        public bool CanDeliver { get; }

        /// <summary>Derived from <see cref="CanDeliver"/> and the requirements, so it never disagrees with them.</summary>
        public CustomerMood Mood
        {
            get
            {
                if (CanDeliver)
                    return CustomerMood.Ready;
                foreach (var requirement in Requirements)
                {
                    if (requirement.IsFulfilled)
                        return CustomerMood.Hopeful;
                }

                return CustomerMood.Waiting;
            }
        }

        public bool Equals(OrderCard other)
        {
            if (other == null)
                return false;
            if (SlotIndex != other.SlotIndex || CustomerIndex != other.CustomerIndex || RewardPoints != other.RewardPoints
                || CanDeliver != other.CanDeliver || Requirements.Count != other.Requirements.Count)
                return false;

            for (var i = 0; i < Requirements.Count; i++)
            {
                if (!Requirements[i].Equals(other.Requirements[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj) => obj is OrderCard other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(SlotIndex, CustomerIndex, RewardPoints, CanDeliver, Requirements.Count);

        public override string ToString() =>
            $"Slot {SlotIndex}: customer {CustomerIndex} wants {string.Join(", ", Requirements)} for {RewardPoints}{(CanDeliver ? " (ready)" : string.Empty)}";
    }
}
