using System;
using System.Collections.Generic;
using MergeGame.Core.Items;

namespace MergeGame.Presentation.Chains
{
    /// <summary>One item chain as the chains popup shows it: its items from the first level to the last.</summary>
    public sealed class ChainLevels
    {
        public ChainLevels(string chainId, IReadOnlyList<ItemId> levels)
        {
            ChainId = chainId ?? throw new ArgumentNullException(nameof(chainId));
            Levels = levels ?? throw new ArgumentNullException(nameof(levels));
        }

        public string ChainId { get; }

        /// <summary>Level 1 first.</summary>
        public IReadOnlyList<ItemId> Levels { get; }
    }
}
