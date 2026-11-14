using System.Collections.Generic;
using MergeGame.Core.Orders;

namespace MergeGame.Core.Events
{
    /// <summary>
    /// <see cref="Order"/> left slot <see cref="SlotIndex"/>; its items were removed from the board.
    /// <see cref="ConsumedItems"/>[i] is the board item that satisfied <c>Order.Requirements[i]</c>.
    /// </summary>
    public sealed class OrderDelivered : GameEvent
    {
        public OrderDelivered(Order order, int slotIndex, IReadOnlyList<ConsumedItem> consumedItems, int awardedPoints)
        {
            Order = order;
            SlotIndex = slotIndex;
            ConsumedItems = consumedItems;
            AwardedPoints = awardedPoints;
        }

        public Order Order { get; }
        public int SlotIndex { get; }
        public IReadOnlyList<ConsumedItem> ConsumedItems { get; }
        public int AwardedPoints { get; }

        public override string ToString() => $"{nameof(OrderDelivered)} {Order} from slot {SlotIndex} (+{AwardedPoints})";
    }
}
