using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Spawning;

namespace MergeGame.Core.Orders
{
    /// <summary>
    /// The session's active orders in fixed slots: picks customers, generates, validates and prices orders and matches
    /// them against the board. It never touches the board or score; the session applies a delivery and reports it.
    /// Creating an order rolls the customer first, then whatever the <see cref="IOrderGenerator"/> rolls.
    /// </summary>
    internal sealed class OrderBook
    {
        private readonly OrderSettings settings;
        private readonly IOrderGenerator generator;
        private readonly ItemCatalog catalog;
        private readonly IRandom random;
        private readonly Order[] slots;
        private readonly ReadOnlyCollection<Order> activeOrders;
        private readonly List<ConsumedItem> matchBuffer = new List<ConsumedItem>();
        private readonly List<int> customerBuffer = new List<int>();
        private int nextOrderId = 1;

        public OrderBook(OrderSettings settings, IOrderGenerator generator, ItemCatalog catalog, IRandom random)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.random = random ?? throw new ArgumentNullException(nameof(random));

            slots = new Order[settings.MaxActiveOrders];
            for (var i = 0; i < slots.Length; i++)
                slots[i] = CreateOrderFor(i);
            activeOrders = Array.AsReadOnly(slots);
        }

        public IReadOnlyList<Order> ActiveOrders => activeOrders;

        public bool TryGetSlot(int orderId, out int slotIndex)
        {
            for (slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                if (slots[slotIndex].Id == orderId)
                    return true;
            }

            slotIndex = -1;
            return false;
        }

        public bool CanFulfil(Order order, IReadOnlyBoard board) => TryMatch(order, board, matchBuffer);

        /// <summary><paramref name="result"/>[i] satisfies <c>order.Requirements[i]</c> when this returns true.</summary>
        public bool TryMatch(Order order, IReadOnlyBoard board, List<ConsumedItem> result) => Match(order, board, result, null);

        /// <summary>Entry i tells whether requirement i is matched; all true exactly when <see cref="CanFulfil"/> is.</summary>
        public bool[] GetFulfilment(Order order, IReadOnlyBoard board)
        {
            var fulfilment = new bool[order.Requirements.Count];
            Match(order, board, matchBuffer, fulfilment);
            return fulfilment;
        }

        /// <summary>
        /// Generates and prices the order that will take <paramref name="slotIndex"/> without placing it, so a failing
        /// generator leaves the book unchanged.
        /// </summary>
        public Order CreateOrderFor(int slotIndex)
        {
            var customerIndex = ChooseCustomer(slotIndex);
            var requirements = generator.Generate(random)
                               ?? throw new InvalidOperationException($"Order generator {generator.GetType().Name} returned no requirements.");
            settings.EnsureAllows(requirements, catalog);

            return new Order(nextOrderId++, customerIndex, requirements, settings.RewardFor(requirements, catalog));
        }

        public void Replace(int slotIndex, Order order) => slots[slotIndex] = order ?? throw new ArgumentNullException(nameof(order));

        /// <summary>Uniform among customers no other slot shows; any customer once all of them are taken.</summary>
        private int ChooseCustomer(int slotIndex)
        {
            customerBuffer.Clear();
            for (var customer = 0; customer < settings.CustomerCount; customer++)
            {
                if (!IsShownByOtherSlot(customer, slotIndex))
                    customerBuffer.Add(customer);
            }

            if (customerBuffer.Count == 0)
                return random.Next(settings.CustomerCount);
            return customerBuffer[random.Next(customerBuffer.Count)];
        }

        private bool IsShownByOtherSlot(int customer, int slotIndex)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (i != slotIndex && slots[i] != null && slots[i].CustomerIndex == customer)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Greedy row-major matching: each requirement in order takes the first unlocked matching item no earlier
        /// requirement took. A missing requirement takes nothing, so later picks do not depend on whether matching
        /// stops there (<paramref name="fulfilment"/> null) or continues (<paramref name="fulfilment"/> given).
        /// </summary>
        private static bool Match(Order order, IReadOnlyBoard board, List<ConsumedItem> matched, bool[] fulfilment)
        {
            matched.Clear();
            var allFound = true;
            for (var i = 0; i < order.Requirements.Count; i++)
            {
                var required = order.Requirements[i];
                if (TryFindFreeItem(required, board, matched, out var position))
                {
                    matched.Add(new ConsumedItem(position, required));
                    if (fulfilment != null)
                        fulfilment[i] = true;
                }
                else
                {
                    allFound = false;
                    if (fulfilment == null)
                        return false;
                }
            }

            return allFound;
        }

        private static bool TryFindFreeItem(ItemId wanted, IReadOnlyBoard board, List<ConsumedItem> taken, out GridPosition position)
        {
            foreach (var candidate in board.Positions)
            {
                if (board.TryGetItem(candidate, out var item) && !item.IsLocked && item.ItemId == wanted && !IsTaken(taken, candidate))
                {
                    position = candidate;
                    return true;
                }
            }

            position = default;
            return false;
        }

        private static bool IsTaken(List<ConsumedItem> taken, GridPosition position)
        {
            foreach (var item in taken)
            {
                if (item.Position == position)
                    return true;
            }

            return false;
        }
    }
}
