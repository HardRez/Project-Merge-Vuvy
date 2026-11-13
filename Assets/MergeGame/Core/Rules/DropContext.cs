using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Rules
{
    /// <summary>
    /// A drop that already passed the structural checks: both cells are on the board, they differ, and the source holds an item.
    /// </summary>
    public sealed class DropContext
    {
        internal DropContext(IReadOnlyBoard board, ItemCatalog catalog, GridPosition from, GridPosition to, BoardItem sourceItem, BoardItem targetItem)
        {
            Board = board;
            Catalog = catalog;
            From = from;
            To = to;
            SourceItem = sourceItem;
            TargetItem = targetItem;
        }

        public IReadOnlyBoard Board { get; }
        public ItemCatalog Catalog { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }
        public BoardItem SourceItem { get; }

        /// <summary>The item on the target cell, or null when the target is empty.</summary>
        public BoardItem TargetItem { get; }

        public bool IsTargetEmpty => TargetItem == null;

        public DropOutcome Move() => DropOutcome.Move(From, To);
        public DropOutcome Merge(ItemId resultItemId) => DropOutcome.Merge(From, To, resultItemId);
        public DropOutcome Reject(DropRejectReason reason) => DropOutcome.Reject(From, To, reason);
    }
}
