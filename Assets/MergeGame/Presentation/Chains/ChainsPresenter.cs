using System;
using System.Collections.Generic;
using MergeGame.Core.Items;

namespace MergeGame.Presentation.Chains
{
    /// <summary>Gives the chains popup every chain of the catalog, in the catalog's order, each from level 1 to its last level.</summary>
    public sealed class ChainsPresenter : IPresenter
    {
        private readonly IChainsView view;
        private readonly ItemCatalog catalog;

        public ChainsPresenter(IChainsView view, ItemCatalog catalog)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public void Initialize()
        {
            var chains = new List<ChainLevels>(catalog.Chains.Count);
            foreach (var chain in catalog.Chains)
            {
                var levels = new ItemId[chain.MaxLevel];
                for (var level = 1; level <= chain.MaxLevel; level++)
                    levels[level - 1] = chain.ItemAtLevel(level);
                chains.Add(new ChainLevels(chain.Id, levels));
            }

            view.ShowChains(chains);
        }

        public void Dispose()
        {
        }
    }
}
