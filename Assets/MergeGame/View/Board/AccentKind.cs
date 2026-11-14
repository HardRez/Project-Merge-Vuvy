namespace MergeGame.View.Board
{
    /// <summary>Why an item is drawing attention to itself.</summary>
    public enum AccentKind
    {
        None,

        /// <summary>The player has been idle and this item is half of a suggested merge.</summary>
        MergeHint,

        /// <summary>The dragged item would merge into this one if dropped now.</summary>
        MergeTarget
    }
}
