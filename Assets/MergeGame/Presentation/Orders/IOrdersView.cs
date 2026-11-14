using System;
using System.Collections.Generic;
using MergeGame.Core.Orders;

namespace MergeGame.Presentation.Orders
{
    /// <summary>The strip of customer order cards, one per slot.</summary>
    public interface IOrdersView
    {
        /// <summary>The player asked to deliver the order of the slot.</summary>
        event Action<int> DeliverRequested;

        /// <summary>Called once at start; <c>cards[i]</c> belongs to slot i.</summary>
        void ShowCards(IReadOnlyList<OrderCard> cards);

        /// <summary>The game has no orders.</summary>
        void Hide();

        /// <summary>The card's order is unchanged but what it shows (fulfilment, deliverability) changed.</summary>
        void RefreshCard(OrderCard card);

        /// <summary>
        /// The slot's order was delivered. <paramref name="consumedItems"/> came from these board cells (the board view
        /// removes them at the same time), so they can fly to the customer.
        /// </summary>
        void CompleteOrder(int slotIndex, IReadOnlyList<ConsumedItem> consumedItems, int awardedPoints);

        /// <summary>A new order arrived in the slot of a completed one.</summary>
        void AddCard(OrderCard card);
    }
}
