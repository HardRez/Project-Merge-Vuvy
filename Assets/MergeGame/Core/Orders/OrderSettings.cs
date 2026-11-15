using System;
using System.Collections.Generic;
using MergeGame.Core.Items;
using MergeGame.Core.Scoring;

namespace MergeGame.Core.Orders
{
    /// <summary>Designer data for customer orders. These bounds bind every <see cref="IOrderGenerator"/>, not just the default one.</summary>
    public sealed class OrderSettings
    {
        /// <param name="maxRequestedLevel">May exceed a chain's length; requests are capped at each chain's max level.</param>
        /// <param name="rewardPointsByLevel">
        /// Points per requested item by its level, scaled by the chain's score multiplier and rounded per item like merge points.
        /// </param>
        public OrderSettings(
            int maxActiveOrders,
            int minItemsPerOrder,
            int maxItemsPerOrder,
            int minRequestedLevel,
            int maxRequestedLevel,
            int customerCount,
            ScoreTable rewardPointsByLevel)
        {
            if (maxActiveOrders < 1)
                throw new ArgumentOutOfRangeException(nameof(maxActiveOrders), maxActiveOrders, "At least one order must be active at a time.");
            if (minItemsPerOrder < 1)
                throw new ArgumentOutOfRangeException(nameof(minItemsPerOrder), minItemsPerOrder, "An order must ask for at least one item.");
            if (maxItemsPerOrder < minItemsPerOrder)
                throw new ArgumentOutOfRangeException(
                    nameof(maxItemsPerOrder), maxItemsPerOrder, $"Max items per order must be at least the minimum ({minItemsPerOrder}).");
            if (minRequestedLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(minRequestedLevel), minRequestedLevel, "Requested levels start at 1.");
            if (maxRequestedLevel < minRequestedLevel)
                throw new ArgumentOutOfRangeException(
                    nameof(maxRequestedLevel), maxRequestedLevel, $"Max requested level must be at least the minimum ({minRequestedLevel}).");
            if (customerCount < 1)
                throw new ArgumentOutOfRangeException(nameof(customerCount), customerCount, "At least one customer is needed to place orders.");

            MaxActiveOrders = maxActiveOrders;
            MinItemsPerOrder = minItemsPerOrder;
            MaxItemsPerOrder = maxItemsPerOrder;
            MinRequestedLevel = minRequestedLevel;
            MaxRequestedLevel = maxRequestedLevel;
            CustomerCount = customerCount;
            RewardPointsByLevel = rewardPointsByLevel ?? throw new ArgumentNullException(nameof(rewardPointsByLevel));
        }

        public int MaxActiveOrders { get; }
        public int MinItemsPerOrder { get; }
        public int MaxItemsPerOrder { get; }
        public int MinRequestedLevel { get; }
        public int MaxRequestedLevel { get; }
        /// <summary>
        /// Customers available to place orders. Active orders get distinct customers while
        /// <see cref="CustomerCount"/> &gt;= <see cref="MaxActiveOrders"/>; otherwise some customers appear twice.
        /// A replacement order never comes from the customer it replaces unless there is only one customer.
        /// </summary>
        public int CustomerCount { get; }
        public ScoreTable RewardPointsByLevel { get; }

        internal void EnsureFits(ItemCatalog catalog)
        {
            if (MinRequestedLevel > catalog.HighestLevel)
                throw new ArgumentException(
                    $"Orders request level {MinRequestedLevel} or higher, but no chain in the item catalog goes beyond level {catalog.HighestLevel}.");

            var highest = Math.Min(MaxRequestedLevel, catalog.HighestLevel);
            if (RewardPointsByLevel.HighestLevel < highest)
                throw new ArgumentException(
                    $"Order reward table defines points up to level {RewardPointsByLevel.HighestLevel}, but orders can request level {highest}.");
        }

        internal int RewardFor(IReadOnlyList<ItemId> requirements, ItemCatalog catalog)
        {
            var reward = 0;
            foreach (var item in requirements)
                reward += ChainPoints.Scale(RewardPointsByLevel.GetPoints(item.Level), catalog.GetChain(item.ChainId).ScoreMultiplier);
            return reward;
        }

        internal void EnsureAllows(IReadOnlyList<ItemId> requirements, ItemCatalog catalog)
        {
            if (requirements.Count < MinItemsPerOrder || requirements.Count > MaxItemsPerOrder)
                throw new InvalidOperationException(
                    $"Order generator asked for {requirements.Count} items; orders need {MinItemsPerOrder}..{MaxItemsPerOrder}.");

            foreach (var item in requirements)
            {
                if (!catalog.Contains(item))
                    throw new InvalidOperationException($"Order generator asked for {item}, which is not in the item catalog.");
                if (item.Level < MinRequestedLevel || item.Level > MaxRequestedLevel)
                    throw new InvalidOperationException(
                        $"Order generator asked for {item}; requested levels must be {MinRequestedLevel}..{MaxRequestedLevel}.");
            }
        }
    }
}
