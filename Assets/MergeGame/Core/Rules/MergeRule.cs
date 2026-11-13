namespace MergeGame.Core.Rules
{
    /// <summary>
    /// Resolves every drop onto an occupied cell: same chain and same level below the chain's max level merges into the
    /// next level; anything else is rejected so the dragged item returns to its cell.
    /// </summary>
    public sealed class MergeRule : IDropRule
    {
        public bool TryResolve(DropContext context, out DropOutcome outcome)
        {
            if (context.IsTargetEmpty)
            {
                outcome = null;
                return false;
            }

            var source = context.SourceItem.ItemId;
            var target = context.TargetItem.ItemId;

            if (!source.IsSameChain(target))
                outcome = context.Reject(DropRejectReason.DifferentChain);
            else if (source.Level != target.Level)
                outcome = context.Reject(DropRejectReason.DifferentLevel);
            else if (context.Catalog.TryGetNext(target, out var next))
                outcome = context.Merge(next);
            else
                outcome = context.Reject(DropRejectReason.MaxLevel);

            return true;
        }
    }
}
