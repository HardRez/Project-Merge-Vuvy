using System;
using System.Collections.Generic;
using MergeGame.Core.Orders;
using MergeGame.Data.Visuals;
using MergeGame.Presentation.Orders;
using MergeGame.View.Audio;
using MergeGame.View.Board;
using UnityEngine;
using Zenject;

namespace MergeGame.View.Orders
{
    /// <summary>The strip of order cards, one per slot, sharing the width equally whatever the number of slots.</summary>
    public sealed class OrdersView : MonoBehaviour, IOrdersView, IOrderSlotTargets
    {
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private OrderCardView cardPrefab;

        [Tooltip("Optional; without it completed orders are silent.")]
        [SerializeField] private SoundPlayer sounds;

        private readonly List<OrderCardView> cards = new List<OrderCardView>();
        private IItemVisuals visuals;

        public event Action<int> DeliverRequested;

        public int CardCount => cards.Count;

        [Inject]
        public void Construct(IItemVisuals itemVisuals) => visuals = itemVisuals;

        public OrderCardView GetCard(int slotIndex) => cards[slotIndex];

        public void ShowCards(IReadOnlyList<OrderCard> orderCards)
        {
            foreach (var card in cards)
                Destroy(card.gameObject);
            cards.Clear();

            foreach (var orderCard in orderCards)
            {
                var card = Instantiate(cardPrefab, cardContainer);
                card.name = $"Order Card {orderCard.SlotIndex}";
                card.Initialize(visuals);
                var slotIndex = orderCard.SlotIndex;
                card.DeliverClicked += () => DeliverRequested?.Invoke(slotIndex);
                card.Show(orderCard);
                cards.Add(card);
            }

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        public void RefreshCard(OrderCard card) => FindCard(card.SlotIndex)?.Show(card);

        public void CompleteOrder(int slotIndex, IReadOnlyList<ConsumedItem> consumedItems, int awardedPoints)
        {
            FindCard(slotIndex)?.Celebrate(awardedPoints);
            sounds.TryPlay(SoundCue.OrderComplete);
        }

        public void AddCard(OrderCard card) => FindCard(card.SlotIndex)?.Show(card);

        public bool TryGetDeliveryTarget(int slotIndex, out Vector3 worldPosition)
        {
            var card = FindCard(slotIndex);
            worldPosition = card != null ? card.DeliveryTarget : default;
            return card != null && isActiveAndEnabled;
        }

        private OrderCardView FindCard(int slotIndex) => slotIndex >= 0 && slotIndex < cards.Count ? cards[slotIndex] : null;
    }
}
