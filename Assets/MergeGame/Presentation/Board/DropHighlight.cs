namespace MergeGame.Presentation.Board
{
    /// <summary>What dropping the dragged item on the hovered cell would do.</summary>
    public enum DropHighlight
    {
        MoveToEmptyCell,
        Merge,
        Invalid,

        /// <summary>Invalid because both items are already at the last level of their chain.</summary>
        MaxLevel
    }
}
