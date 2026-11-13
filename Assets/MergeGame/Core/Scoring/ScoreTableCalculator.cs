using System;
using MergeGame.Core.Items;

namespace MergeGame.Core.Scoring
{
    /// <summary>Score table points for the created level, scaled by the chain's multiplier and rounded half away from zero.</summary>
    public sealed class ScoreTableCalculator : IScoreCalculator
    {
        private readonly ScoreTable table;
        private readonly ItemCatalog catalog;

        public ScoreTableCalculator(ScoreTable table, ItemCatalog catalog)
        {
            this.table = table ?? throw new ArgumentNullException(nameof(table));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            table.EnsureCovers(catalog);
        }

        public int PointsForMerge(ItemId resultItemId)
        {
            var multiplier = catalog.GetChain(resultItemId.ChainId).ScoreMultiplier;
            var points = table.GetPoints(resultItemId.Level) * (double)multiplier;
            return (int)Math.Round(points, MidpointRounding.AwayFromZero);
        }
    }
}
