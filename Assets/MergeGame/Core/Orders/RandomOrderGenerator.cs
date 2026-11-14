using System;
using System.Collections.Generic;
using MergeGame.Core.Items;
using MergeGame.Core.Spawning;

namespace MergeGame.Core.Orders
{
    /// <summary>
    /// Uniform picks within the <see cref="OrderSettings"/> bounds, among spawnable chains long enough for the minimum level.
    /// Rolls, in order: item count, then chain and level for each item.
    /// </summary>
    public sealed class RandomOrderGenerator : IOrderGenerator
    {
        private readonly OrderSettings settings;
        private readonly ItemChainDefinition[] eligibleChains;

        public RandomOrderGenerator(OrderSettings settings, ItemCatalog catalog)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            var eligible = new List<ItemChainDefinition>();
            foreach (var chain in catalog.Chains)
            {
                if (chain.IsSpawnable && chain.MaxLevel >= settings.MinRequestedLevel)
                    eligible.Add(chain);
            }

            if (eligible.Count == 0)
                throw new ArgumentException(
                    $"Orders need at least one spawnable chain that reaches level {settings.MinRequestedLevel}.", nameof(catalog));

            eligibleChains = eligible.ToArray();
        }

        public IReadOnlyList<ItemId> Generate(IRandom random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var itemCount = RollBetween(random, settings.MinItemsPerOrder, settings.MaxItemsPerOrder);

            var requirements = new ItemId[itemCount];
            for (var i = 0; i < itemCount; i++)
            {
                var chain = eligibleChains[random.Next(eligibleChains.Length)];
                var level = RollBetween(random, settings.MinRequestedLevel, Math.Min(settings.MaxRequestedLevel, chain.MaxLevel));
                requirements[i] = chain.ItemAtLevel(level);
            }

            return requirements;
        }

        private static int RollBetween(IRandom random, int min, int maxInclusive) => min + random.Next(maxInclusive - min + 1);
    }
}
