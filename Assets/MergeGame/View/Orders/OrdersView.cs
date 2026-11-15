using System;
using System.Collections.Generic;
using MergeGame.Core.Orders;
using MergeGame.Data.Visuals;
using MergeGame.Presentation.Orders;
using MergeGame.View.Audio;
using MergeGame.View.Board;
using MergeGame.View.Feedback;
using MergeGame.View.Score;
using UnityEngine;
using Zenject;

namespace MergeGame.View.Orders
{
    /// <summary>
    /// The shop counter with one order per slot, the slots sharing the width equally whatever their number. Each slot has
    /// two parts in two rows: its customer, standing behind the counter, and its card in front of it (the tray of
    /// requested items, the reward and the deliver button). The counter is drawn between the rows, so it hides the
    /// customers' lower bodies; both rows lay out alike, so a customer stays above its own card.
    /// </summary>
    public sealed class OrdersView : MonoBehaviour, IOrdersView, IOrderSlotTargets
    {
        [SerializeField] private RectTransform customerContainer;
        [SerializeField] private CustomerPortraitView customerPrefab;
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private OrderCardView cardPrefab;

        [Tooltip("Optional; without it completed orders are silent.")]
        [SerializeField] private SoundPlayer sounds;

        [Tooltip("Optional; without it completed orders do not buzz the device.")]
        [SerializeField] private Haptics haptics;

        [Tooltip("Optional; without it the money of a completed order goes straight into the score pill.")]
        [SerializeField] private CashFlightView cashFlight;

        private readonly List<OrderCardView> cards = new List<OrderCardView>();
        private readonly List<CustomerPortraitView> customers = new List<CustomerPortraitView>();
        private IItemVisuals visuals;

        public event Action<int> DeliverRequested;

        public int CardCount => cards.Count;

        [Inject]
        public void Construct(IItemVisuals itemVisuals) => visuals = itemVisuals;

        public OrderCardView GetCard(int slotIndex) => cards[slotIndex];

        public void ShowCards(IReadOnlyList<OrderCard> orderCards)
        {
            // The cards that owed money are going away, so they will never pay it out.
            if (cashFlight != null)
                cashFlight.SettleOwed();

            foreach (var card in cards)
                Destroy(card.gameObject);
            foreach (var customer in customers)
                Destroy(customer.gameObject);
            cards.Clear();
            customers.Clear();

            foreach (var orderCard in orderCards)
            {
                var customer = Instantiate(customerPrefab, customerContainer);
                customer.name = $"Customer {orderCard.SlotIndex}";
                customers.Add(customer);
                var card = Instantiate(cardPrefab, cardContainer);
                card.name = $"Order Card {orderCard.SlotIndex}";
                card.Initialize(visuals, customer);
                var slotIndex = orderCard.SlotIndex;
                card.DeliverClicked += () => DeliverRequested?.Invoke(slotIndex);
                card.PaidOut += origin => PayOut(slotIndex, origin);
                // Customers coming in together (the opening counter) pop once: the player plays a clip once per frame.
                card.CustomerArrived += () => sounds.TryPlay(SoundCue.CustomerAppear);
                card.Show(orderCard);
                cards.Add(card);
            }

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        public void RefreshCard(OrderCard card) => FindCard(card.SlotIndex)?.Show(card);

        public void CompleteOrder(int slotIndex, IReadOnlyList<ConsumedItem> consumedItems, int awardedPoints)
        {
            var card = FindCard(slotIndex);
            // Announced now, before the score changes, so the pill waits for the money; it flies when the customer pays.
            if (cashFlight != null && card != null && card.isActiveAndEnabled)
                cashFlight.Expect(slotIndex, awardedPoints);
            card?.Celebrate(awardedPoints);
            sounds.TryPlay(SoundCue.OrderComplete);
            haptics.TryPulse(HapticPulse.Strong);
        }

        public void AddCard(OrderCard card) => FindCard(card.SlotIndex)?.Show(card);

        public bool TryGetDeliveryTarget(int slotIndex, out Vector3 worldPosition)
        {
            var card = FindCard(slotIndex);
            worldPosition = card != null ? card.DeliveryTarget : default;
            return card != null && isActiveAndEnabled;
        }

        private void OnDisable()
        {
            // A hidden counter's cards stop celebrating, so they would never pay what they owe.
            if (cashFlight != null)
                cashFlight.SettleOwed();
        }

        private void PayOut(int slotIndex, Vector3 origin)
        {
            if (cashFlight != null)
                cashFlight.PayOut(slotIndex, origin);
        }

        private OrderCardView FindCard(int slotIndex) => slotIndex >= 0 && slotIndex < cards.Count ? cards[slotIndex] : null;
    }
}
