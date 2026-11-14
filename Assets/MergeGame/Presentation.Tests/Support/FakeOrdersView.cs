using System;
using System.Collections.Generic;
using MergeGame.Core.Orders;
using MergeGame.Presentation.Orders;

namespace MergeGame.Presentation.Tests.Support
{
    internal sealed class FakeOrdersView : IOrdersView
    {
        private readonly ViewLog log;

        public FakeOrdersView(ViewLog log)
        {
            this.log = log;
        }

        public event Action<int> DeliverRequested;

        public bool IsHidden { get; private set; }
        public Dictionary<int, OrderCard> Cards { get; } = new Dictionary<int, OrderCard>();
        public List<OrderCard> Refreshes { get; } = new List<OrderCard>();

        public List<(int slot, IReadOnlyList<ConsumedItem> items, int points)> Completions { get; } =
            new List<(int, IReadOnlyList<ConsumedItem>, int)>();

        public bool HasInputListeners => DeliverRequested != null;

        public void RequestDelivery(int slotIndex) => DeliverRequested?.Invoke(slotIndex);

        public void ShowCards(IReadOnlyList<OrderCard> cards)
        {
            for (var slot = 0; slot < cards.Count; slot++)
                Cards[slot] = cards[slot];
            log.Add($"Orders.ShowCards {cards.Count}");
        }

        public void Hide()
        {
            IsHidden = true;
            log.Add("Orders.Hide");
        }

        public void RefreshCard(OrderCard card)
        {
            Cards[card.SlotIndex] = card;
            Refreshes.Add(card);
            log.Add($"Orders.RefreshCard {card}");
        }

        public void CompleteOrder(int slotIndex, IReadOnlyList<ConsumedItem> consumedItems, int awardedPoints)
        {
            Cards.Remove(slotIndex);
            Completions.Add((slotIndex, consumedItems, awardedPoints));
            log.Add($"Orders.CompleteOrder slot {slotIndex} +{awardedPoints}");
        }

        public void AddCard(OrderCard card)
        {
            Cards[card.SlotIndex] = card;
            log.Add($"Orders.AddCard {card}");
        }
    }
}
