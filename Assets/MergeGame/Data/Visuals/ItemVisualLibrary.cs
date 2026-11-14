using System;
using System.Collections.Generic;
using System.Linq;
using MergeGame.Core.Items;
using MergeGame.Data.Authoring;
using MergeGame.Data.Definitions;
using Spine.Unity;
using UnityEngine;

namespace MergeGame.Data.Visuals
{
    /// <summary>Looks up icons and names in the chain and order assets of one <see cref="GameConfigAsset"/>.</summary>
    public sealed class ItemVisualLibrary : IItemVisuals
    {
        private readonly Dictionary<string, ItemChainAsset> chainsById = new Dictionary<string, ItemChainAsset>(StringComparer.Ordinal);
        private readonly CustomerEntry[] customers;

        /// <exception cref="GameConfigException">A chain slot is empty, or a chain id is missing or used twice.</exception>
        public ItemVisualLibrary(GameConfigAsset config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            for (var i = 0; i < config.Chains.Count; i++)
            {
                var chain = config.Chains[i];
                if (chain == null)
                    throw new GameConfigException($"Game config '{config.name}': chain slot {i} is empty.");
                if (string.IsNullOrWhiteSpace(chain.Id))
                    throw new GameConfigException($"Game config '{config.name}': {AssetLabels.Chain(chain)} has no id.");
                if (chainsById.ContainsKey(chain.Id))
                    throw new GameConfigException($"Game config '{config.name}': the chain id '{chain.Id}' is used more than once.");
                chainsById.Add(chain.Id, chain);
            }

            customers = config.Orders == null ? Array.Empty<CustomerEntry>() : config.Orders.Customers.ToArray();
        }

        public int CustomerCount => customers.Length;

        public Sprite GetIcon(ItemId item) => GetLevel(item).Icon;

        public string GetDisplayName(ItemId item) => GetLevel(item).DisplayName;

        public string GetChainDisplayName(string chainId) => GetChain(chainId).DisplayName;

        public Sprite GetCustomerAvatar(int customerIndex) => GetCustomer(customerIndex)?.Avatar;

        public SkeletonDataAsset GetCustomerPortrait(int customerIndex) => GetCustomer(customerIndex)?.Portrait;

        private CustomerEntry GetCustomer(int customerIndex)
        {
            if (customerIndex < 0 || customerIndex >= customers.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(customerIndex), customerIndex, $"There are {customers.Length} customers (indices 0..{customers.Length - 1}).");
            return customers[customerIndex];
        }

        private ItemLevelEntry GetLevel(ItemId item)
        {
            var chain = GetChain(item.ChainId);
            if (item.Level < 1 || item.Level > chain.MaxLevel)
                throw new ArgumentException($"No visuals for item '{item}'; chain '{chain.Id}' has levels 1..{chain.MaxLevel}.", nameof(item));
            return chain.Levels[item.Level - 1];
        }

        private ItemChainAsset GetChain(string chainId)
        {
            if (chainId == null || !chainsById.TryGetValue(chainId, out var chain))
                throw new ArgumentException($"No visuals for chain '{chainId}'; it is not in the game config.", nameof(chainId));
            return chain;
        }
    }
}
