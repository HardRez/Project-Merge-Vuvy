using System;
using MergeGame.Core.Events;
using MergeGame.Core.Orders;
using MergeGame.Core.Session;
using MergeGame.Presentation.Events;
using MergeGame.Presentation.Notices;

namespace MergeGame.Presentation.Orders
{
    /// <summary>
    /// One card per order slot. After every board change it re-checks which requirements the board can satisfy and
    /// refreshes only the cards whose content changed.
    /// </summary>
    public sealed class OrdersPresenter : IPresenter
    {
        private readonly MergeGameSession session;
        private readonly SessionEventStream events;
        private readonly IOrdersView view;
        private readonly INoticeView notices;
        private readonly SubscriptionBag subscriptions = new SubscriptionBag();

        /// <summary>What each slot's card shows; null between a delivery and the replacement order's card.</summary>
        private OrderCard[] shownCards = Array.Empty<OrderCard>();

        public OrdersPresenter(MergeGameSession session, SessionEventStream events, IOrdersView view, INoticeView notices)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.notices = notices ?? throw new ArgumentNullException(nameof(notices));
        }

        public void Initialize()
        {
            var orders = session.ActiveOrders;
            if (orders.Count == 0)
            {
                view.Hide();
                return;
            }

            shownCards = new OrderCard[orders.Count];
            for (var slot = 0; slot < orders.Count; slot++)
                shownCards[slot] = CreateCard(slot, orders[slot]);
            view.ShowCards(Array.AsReadOnly((OrderCard[])shownCards.Clone()));

            view.DeliverRequested += OnDeliverRequested;
            subscriptions.Add(events.Subscribe<ItemSpawned>(_ => RefreshCards()));
            subscriptions.Add(events.Subscribe<ItemMoved>(_ => RefreshCards()));
            subscriptions.Add(events.Subscribe<ItemsMerged>(_ => RefreshCards()));
            subscriptions.Add(events.Subscribe<ItemDiscarded>(_ => RefreshCards()));
            subscriptions.Add(events.Subscribe<OrderDelivered>(OnOrderDelivered));
            subscriptions.Add(events.Subscribe<OrderAdded>(OnOrderAdded));
        }

        public void Dispose()
        {
            view.DeliverRequested -= OnDeliverRequested;
            subscriptions.Dispose();
        }

        private void OnDeliverRequested(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= shownCards.Length || shownCards[slotIndex] == null)
                return;

            var result = session.DeliverOrder(session.ActiveOrders[slotIndex].Id);
            if (!result.Succeeded && result.FailureReason == DeliveryFailureReason.MissingItems)
                notices.ShowNotice(Notice.OrderNotReady);
        }

        private void OnOrderDelivered(OrderDelivered delivered)
        {
            shownCards[delivered.SlotIndex] = null;
            view.CompleteOrder(delivered.SlotIndex, delivered.ConsumedItems, delivered.AwardedPoints);
            RefreshCards();
        }

        private void OnOrderAdded(OrderAdded added)
        {
            var card = CreateCard(added.SlotIndex, added.Order);
            shownCards[added.SlotIndex] = card;
            view.AddCard(card);
        }

        private void RefreshCards()
        {
            var orders = session.ActiveOrders;
            for (var slot = 0; slot < shownCards.Length; slot++)
            {
                if (shownCards[slot] == null)
                    continue;

                var card = CreateCard(slot, orders[slot]);
                if (card.Equals(shownCards[slot]))
                    continue;

                shownCards[slot] = card;
                view.RefreshCard(card);
            }
        }

        private OrderCard CreateCard(int slotIndex, Order order)
        {
            var fulfilment = session.GetRequirementFulfilment(order.Id);
            var requirements = new RequirementState[order.Requirements.Count];
            for (var i = 0; i < requirements.Length; i++)
                requirements[i] = new RequirementState(order.Requirements[i], fulfilment[i]);

            return new OrderCard(slotIndex, order.CustomerIndex, requirements, order.RewardPoints, session.CanDeliver(order.Id));
        }
    }
}
