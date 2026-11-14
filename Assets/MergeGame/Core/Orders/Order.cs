using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MergeGame.Core.Items;

namespace MergeGame.Core.Orders
{
    /// <summary>A customer asking for items. Equal requirements ask for that many distinct board items.</summary>
    public sealed class Order
    {
        public Order(int id, int customerIndex, IReadOnlyList<ItemId> requirements, int rewardPoints)
        {
            if (customerIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(customerIndex), customerIndex, "Customer index must not be negative.");
            if (rewardPoints < 0)
                throw new ArgumentOutOfRangeException(nameof(rewardPoints), rewardPoints, $"Order {id} has a negative reward.");

            Id = id;
            CustomerIndex = customerIndex;
            Requirements = CopyRequirements(requirements, $"Order {id}");
            RewardPoints = rewardPoints;
        }

        /// <summary>Unique within one session.</summary>
        public int Id { get; }

        /// <summary>Which customer avatar asks, in [0, <see cref="OrderSettings.CustomerCount"/>).</summary>
        public int CustomerIndex { get; }

        public IReadOnlyList<ItemId> Requirements { get; }
        public int RewardPoints { get; }

        public override string ToString() => $"Order {Id} (customer {CustomerIndex}: {string.Join(", ", Requirements)})";

        internal static ReadOnlyCollection<ItemId> CopyRequirements(IReadOnlyList<ItemId> requirements, string owner)
        {
            if (requirements == null)
                throw new ArgumentNullException(nameof(requirements));
            if (requirements.Count == 0)
                throw new ArgumentException($"{owner} must require at least one item.", nameof(requirements));

            var copy = new ItemId[requirements.Count];
            for (var i = 0; i < copy.Length; i++)
                copy[i] = requirements[i];
            return Array.AsReadOnly(copy);
        }
    }
}
