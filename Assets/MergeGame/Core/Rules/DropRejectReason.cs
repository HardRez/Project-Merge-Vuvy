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
        SourceLocked
    }
}
