using MergeGame.Core.Grid;
using MergeGame.Core.Rules;

namespace MergeGame.Core.Events
{
    /// <summary>The board is unchanged; the dragged item returns to <see cref="From"/>.</summary>
    public sealed class DropRejected : GameEvent
    {
        public DropRejected(GridPosition from, GridPosition to, DropRejectReason reason)
        {
            From = from;
            To = to;
            Reason = reason;
        }

        public GridPosition From { get; }
        public GridPosition To { get; }
        public DropRejectReason Reason { get; }

        public override string ToString() => $"{nameof(DropRejected)} {From} -> {To} ({Reason})";
    }
}
