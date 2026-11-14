namespace MergeGame.Presentation.Notices
{
    /// <summary>Short messages that tell the player why an action did nothing. The view owns the wording.</summary>
    public enum Notice
    {
        /// <summary>A locked item was picked up; it cannot be moved until a matching item is merged onto it.</summary>
        ItemIsLocked,

        /// <summary>Two equal items at the last level of their chain were dropped onto each other.</summary>
        MaxLevelReached,

        DifferentChain,
        DifferentLevel,

        /// <summary>Spawn was requested with no empty cell.</summary>
        BoardFull,

        LockedItemCannotBeDiscarded,

        /// <summary>Delivery was requested while the board lacks some of the order's items.</summary>
        OrderNotReady,

        /// <summary>A closed box was picked up or something was dropped onto one; boxes open when a neighbouring locked item is freed.</summary>
        ItemIsHidden
    }
}
