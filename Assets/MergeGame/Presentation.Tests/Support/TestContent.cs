using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Orders;
using MergeGame.Core.Scoring;

namespace MergeGame.Presentation.Tests.Support
{
    internal static class TestContent
    {
        public const string Wood = "wood";
        public const string Stone = "stone";
        public const int ChainLength = 5;

        public static readonly int[] MergePoints = { 0, 10, 20, 40, 80 };
        public static readonly int[] RewardPoints = { 5, 15, 30, 60, 120 };

        public static ItemId Item(string chainId, int level) => new ItemId(chainId, level);

        public static GridPosition At(int x, int y) => new GridPosition(x, y);

        public static string DisplayName(ItemId item) => $"{item.ChainId} {item.Level}";

        public static ItemCatalog Catalog()
        {
            return new ItemCatalog(new[] { Chain(Wood), Chain(Stone) });
        }

        public static ScoreTable MergeScoreTable() => new ScoreTable(MergePoints);

        public static OrderSettings OrderSetup(int slotCount) =>
            new OrderSettings(slotCount, 1, 3, 1, 3, 4, new ScoreTable(RewardPoints));

        private static ItemChainDefinition Chain(string id)
        {
            var levels = new List<ItemLevelDefinition>();
            for (var level = 1; level <= ChainLength; level++)
                levels.Add(new ItemLevelDefinition(level, DisplayName(Item(id, level))));
            return new ItemChainDefinition(id, levels, spawnWeight: 1, scoreMultiplier: 1f);
        }
    }
}
