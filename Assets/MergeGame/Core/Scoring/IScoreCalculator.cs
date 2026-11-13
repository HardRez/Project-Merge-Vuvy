using MergeGame.Core.Items;

namespace MergeGame.Core.Scoring
{
    public interface IScoreCalculator
    {
        int PointsForMerge(ItemId resultItemId);
    }
}
