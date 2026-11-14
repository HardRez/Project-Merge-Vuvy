using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Events
{
    /// <summary>The item from <see cref="From"/> and the one on <see cref="To"/> were consumed; the result now sits on <see cref="To"/>.</summary>
    public sealed class ItemsMerged : GameEvent
    {
        public ItemsMerged(GridPosition from, GridPosition to, ItemId consumedItemId, ItemId resultItemId, int awardedPoints, bool targetWasLocked)
        {
            From = from;
            To = to;
            ConsumedItemId = consumedItemId;
            ResultItemId = resultItemId;
            AwardedPoints = awardedPoints;
            TargetWasLocked = targetWasLocked;
        }

        public GridPosition From { get; }
        public GridPosition To { get; }
        public ItemId ConsumedItemId { get; }
        public ItemId ResultItemId { get; }
        public int AwardedPoints { get; }

        /// <summary>The merge freed a locked item; the result is always unlocked.</summary>
        public bool TargetWasLocked { get; }

        public override string ToString() =>
            $"{nameof(ItemsMerged)} 2x {ConsumedItemId} {From} -> {To} = {ResultItemId} (+{AwardedPoints}){(TargetWasLocked ? " unlocked" : string.Empty)}";
    }
}
