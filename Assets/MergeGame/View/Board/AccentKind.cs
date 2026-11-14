namespace MergeGame.View.Board
{
    /// <summary>Why an item is drawing attention to itself.</summary>
    /// <remarks>Ordered by priority: a higher kind replaces a lower one in progress, never the other way round.</remarks>
    public enum AccentKind
    {
        None,

        /// <summary>A one-shot shine right after the item was spawned.</summary>
        SpawnShine,

        /// <summary>The player has been idle and this item is half of a suggested merge.</summary>
        MergeHint,

        /// <summary>The dragged item would merge into this one if dropped now.</summary>
        MergeTarget
    }
}
