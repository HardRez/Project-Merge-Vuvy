using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Orders;
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
        public static readonly int[] DefaultRewardPoints = { 5, 15, 30, 60, 120 };

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
            ScoreTable scoreTable = null,
            IReadOnlyList<InitialPlacement> initialPlacements = null,
            OrderSettings orderSettings = null,
            IOrderGenerator orderGenerator = null) =>
            new GameDefinition(
                catalog ?? DefaultCatalog(),
                new BoardSize(width, height),
                scoreTable ?? new ScoreTable(DefaultPoints),
                initialPlacements: initialPlacements,
                orderSettings: orderSettings,
                orderGenerator: orderGenerator);

        public static OrderSettings OrderSetup(
            int maxActiveOrders = 2,
            int minItems = 1,
            int maxItems = 3,
            int minLevel = 1,
            int maxLevel = 3,
            int customerCount = 4,
            int[] rewardPoints = null) =>
            new OrderSettings(
                maxActiveOrders, minItems, maxItems, minLevel, maxLevel, customerCount, new ScoreTable(rewardPoints ?? DefaultRewardPoints));

        /// <summary>A default 5x5 game whose orders come from <paramref name="generator"/>.</summary>
        public static GameDefinition DefinitionWithOrders(IOrderGenerator generator, OrderSettings settings = null, ItemCatalog catalog = null) =>
            Definition(catalog: catalog, orderSettings: settings ?? OrderSetup(), orderGenerator: generator);
    }
}
