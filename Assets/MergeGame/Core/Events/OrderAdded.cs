using MergeGame.Core.Orders;

namespace MergeGame.Core.Events
{
    /// <summary>A new order now occupies <see cref="SlotIndex"/> of the session's active orders.</summary>
    public sealed class OrderAdded : GameEvent
    {
        public OrderAdded(Order order, int slotIndex)
        {
            Order = order;
            SlotIndex = slotIndex;
        }

        public Order Order { get; }
        public int SlotIndex { get; }

        public override string ToString() => $"{nameof(OrderAdded)} {Order} in slot {SlotIndex}";
    }
}
