namespace MergeGame.Core.Rules
{
    /// <summary>Locked items stay where they are; they can only be freed by merging a matching item onto them.</summary>
    public sealed class LockedSourceRule : IDropRule
    {
        public bool TryResolve(DropContext context, out DropOutcome outcome)
        {
            outcome = context.SourceItem.IsLocked ? context.Reject(DropRejectReason.SourceLocked) : null;
            return outcome != null;
        }
    }
}
