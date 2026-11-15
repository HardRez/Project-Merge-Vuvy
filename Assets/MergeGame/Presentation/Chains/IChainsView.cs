using System.Collections.Generic;

namespace MergeGame.Presentation.Chains
{
    /// <summary>The chains popup: every item chain from its first level to its last. Opening and closing it is the view's own business.</summary>
    public interface IChainsView
    {
        void ShowChains(IReadOnlyList<ChainLevels> chains);
    }
}
