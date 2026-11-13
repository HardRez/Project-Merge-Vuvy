using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Scoring;
using MergeGame.Core.Session;

namespace MergeGame.Core.Tests.Support
{
    internal static class TestContent
    {
        public const string Wood = "wood";
        public const string Stone = "stone";
        public const string Flower = "flower";
        public const int ChainLength = 5;

        public static readonly int[] DefaultPoints = { 0, 10, 20, 40, 80 };

        public static ItemId Item(string chainId, int level) => new ItemId(chainId, level);

        public static ItemChainDefinition Chain(string id, int maxLevel = ChainLength, int spawnWeight = 1, float scoreMultiplier = 1f)
        {
            var levels = new List<ItemLevelDefinition>();
            for (var level = 1; level <= maxLevel; level++)
                levels.Add(new ItemLevelDefinition(level, $"{id} {level}"));
            return new ItemChainDefinition(id, levels, spawnWeight, scoreMultiplier);
        }

        public static ItemCatalog Catalog(params ItemChainDefinition[] chains) => new ItemCatalog(chains);

        public static ItemCatalog DefaultCatalog() => Catalog(Chain(Wood), Chain(Stone), Chain(Flower));

        public static GameDefinition Definition(
            int width = 5,
            int height = 5,
            ItemCatalog catalog = null,
            ScoreTable scoreTable = null) =>
            new GameDefinition(catalog ?? DefaultCatalog(), new BoardSize(width, height), scoreTable ?? new ScoreTable(DefaultPoints));
    }
}
