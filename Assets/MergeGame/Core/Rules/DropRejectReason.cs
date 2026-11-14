namespace MergeGame.Core.Rules
{
    public enum DropRejectReason
    {
        None,
        OutOfBounds,
        SameCell,
        SourceEmpty,
        DifferentChain,
        DifferentLevel,
        MaxLevel,
        SourceLocked,

        /// <summary>The target is a closed box, which is not a merge target.</summary>
        TargetHidden
    }
}
