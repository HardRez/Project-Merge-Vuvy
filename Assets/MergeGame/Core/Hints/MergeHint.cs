using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Hints
{
    /// <summary>A merge the player can make right now: drag the item on <see cref="From"/> onto <see cref="To"/>.</summary>
    public readonly struct MergeHint
    {
        public MergeHint(GridPosition from, GridPosition to, ItemId resultItemId)
        {
            From = from;
            To = to;
            ResultItemId = resultItemId;
        }

        public GridPosition From { get; }
        public GridPosition To { get; }
        public ItemId ResultItemId { get; }

        public override string ToString() => $"{From} -> {To} = {ResultItemId}";
    }
}
