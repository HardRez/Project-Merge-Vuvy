namespace MergeGame.Core.Rules
{
    public sealed class MoveToEmptyCellRule : IDropRule
    {
        public bool TryResolve(DropContext context, out DropOutcome outcome)
        {
            outcome = context.IsTargetEmpty ? context.Move() : null;
            return outcome != null;
        }
    }
}
