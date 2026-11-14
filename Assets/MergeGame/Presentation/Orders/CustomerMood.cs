namespace MergeGame.Presentation.Orders
{
    /// <summary>How the customer of an order card feels about what the board holds for it.</summary>
    public enum CustomerMood
    {
        /// <summary>Nothing it asks for is on the board.</summary>
        Waiting,

        /// <summary>Some of what it asks for is on the board.</summary>
        Hopeful,

        /// <summary>The order can be delivered.</summary>
        Ready
    }
}
