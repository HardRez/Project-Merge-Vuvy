namespace MergeGame.Core.Rules
{
    /// <summary>
    /// One link of the drop rule pipeline. A rule either decides the outcome (returns true) or passes the drop on to the
    /// next rule. Rules only evaluate; they must not change any state, because the same pipeline drives drag previews.
    /// </summary>
    public interface IDropRule
    {
        bool TryResolve(DropContext context, out DropOutcome outcome);
    }
}
