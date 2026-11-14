namespace MergeGame.Core.Rules
{
    /// <summary>A closed box is not a merge target; anything dropped on it returns.</summary>
    public sealed class HiddenTargetRule : IDropRule
    {
        public bool TryResolve(DropContext context, out DropOutcome outcome)
        {
            outcome = !context.IsTargetEmpty && context.TargetItem.IsHidden ? context.Reject(DropRejectReason.TargetHidden) : null;
            return outcome != null;
        }
    }
}
