using System;
using System.Collections.Generic;

namespace MergeGame.Core.Items
{
    /// <summary>Immutable lookup of every item chain in a game. Adding a chain is a data change only.</summary>
    public sealed class ItemCatalog
    {
        private readonly ItemChainDefinition[] chains;
        private readonly Dictionary<string, ItemChainDefinition> chainsById;

        public ItemCatalog(IEnumerable<ItemChainDefinition> chains)
        {
            if (chains == null)
                throw new ArgumentNullException(nameof(chains));

            var list = new List<ItemChainDefinition>();
            chainsById = new Dictionary<string, ItemChainDefinition>(StringComparer.Ordinal);
            foreach (var chain in chains)
            {
                if (chain == null)
                    throw new ArgumentException("Item catalog contains a null chain.", nameof(chains));
                if (chainsById.ContainsKey(chain.Id))
                    throw new ArgumentException($"Item catalog contains the chain id '{chain.Id}' more than once.", nameof(chains));

                chainsById.Add(chain.Id, chain);
                list.Add(chain);
                HighestLevel = Math.Max(HighestLevel, chain.MaxLevel);
            }

            if (list.Count == 0)
                throw new ArgumentException("Item catalog must contain at least one chain.", nameof(chains));

            this.chains = list.ToArray();
        }

        public IReadOnlyList<ItemChainDefinition> Chains => chains;

        /// <summary>The highest level reached by any chain; data such as the score table must cover it.</summary>
        public int HighestLevel { get; }

        public bool Contains(ItemId item) => chainsById.TryGetValue(item.ChainId ?? string.Empty, out var chain) && chain.HasLevel(item.Level);

        public bool TryGetChain(string chainId, out ItemChainDefinition chain) => chainsById.TryGetValue(chainId ?? string.Empty, out chain);

        public ItemChainDefinition GetChain(string chainId)
        {
            if (!TryGetChain(chainId, out var chain))
                throw new ArgumentException($"Item catalog has no chain '{chainId}'.", nameof(chainId));
            return chain;
        }

        public ItemLevelDefinition GetLevel(ItemId item) => GetChainOf(item).GetLevel(item.Level);

        public bool IsMaxLevel(ItemId item) => item.Level == GetChainOf(item).MaxLevel;

        public bool TryGetNext(ItemId item, out ItemId next)
        {
            if (IsMaxLevel(item))
            {
                next = default;
                return false;
            }

            next = item.NextLevel();
            return true;
        }

        private ItemChainDefinition GetChainOf(ItemId item)
        {
            if (!Contains(item))
                throw new ArgumentException($"Item '{item}' is not defined in the item catalog.", nameof(item));
            return chainsById[item.ChainId];
        }
    }
}
