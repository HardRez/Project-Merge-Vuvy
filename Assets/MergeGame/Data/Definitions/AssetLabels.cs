using MergeGame.Data.Authoring;

namespace MergeGame.Data.Definitions
{
    /// <summary>How problem messages name assets, so the factory and the validator read alike.</summary>
    internal static class AssetLabels
    {
        public static string Chain(ItemChainAsset chain) =>
            string.IsNullOrWhiteSpace(chain.Id) ? $"Chain asset '{chain.name}'" : $"Chain '{chain.Id}'";

        public static string Cell(InitialPlacementEntry placement) => $"({placement.X}, {placement.Y})";
    }
}
