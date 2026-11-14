namespace MergeGame.Core.Orders
{
    public enum DeliveryFailureReason
    {
        None,

        /// <summary>No active order has this id; always the case in a game without orders.</summary>
        UnknownOrder,

        /// <summary>The board does not hold enough distinct unlocked items for every requirement.</summary>
        MissingItems
    }
}
