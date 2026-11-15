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
        private readonly BoardItem[][] claims;
        private readonly ReadOnlyCollection<Order> activeOrders;
        private readonly List<ConsumedItem> matchBuffer = new List<ConsumedItem>();
        private readonly List<int> customerBuffer = new List<int>();
        private int nextOrderId = 1;

        public OrderBook(OrderSettings settings, IOrderGenerator generator, ItemCatalog catalog, IRandom random)
            : this(settings, generator, catalog, random, null, 1)
        {
        }

        /// <summary>
        /// Opens the book with <paramref name="savedOrders"/> in its slots instead of generating new ones, as a resumed
        /// game does. Every saved order is checked against the settings and the catalog and repriced with today's
        /// rewards, so content that changed since the save either still fits or is rejected.
        /// </summary>
        /// <param name="savedOrders">One order per slot; null generates fresh orders.</param>
        /// <exception cref="ArgumentException">A saved order no longer fits the settings or the catalog.</exception>
        public OrderBook(
            OrderSettings settings, IOrderGenerator generator, ItemCatalog catalog, IRandom random, IReadOnlyList<Order> savedOrders, int nextOrderId)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.generator = generator ?? throw new ArgumentNullException(nameof(generator));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.random = random ?? throw new ArgumentNullException(nameof(random));

            slots = new Order[settings.MaxActiveOrders];
            claims = new BoardItem[slots.Length][];
            activeOrders = Array.AsReadOnly(slots);
            if (savedOrders == null)
            {
                for (var i = 0; i < slots.Length; i++)
                    slots[i] = CreateOrderFor(i);
            }
            else
            {
                Restore(savedOrders, nextOrderId);
            }
        }

        public IReadOnlyList<Order> ActiveOrders => activeOrders;

        /// <summary>The id the next generated order gets.</summary>
        public int NextOrderId => nextOrderId;

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
            var matchedCells = GetMatchedCells(order, board);
            var fulfilment = new bool[matchedCells.Length];
            for (var i = 0; i < fulfilment.Length; i++)
                fulfilment[i] = matchedCells[i].HasValue;
            return fulfilment;
        }

        /// <summary>Entry i is the cell that would be handed over for requirement i, or null when it is missing.</summary>
        public GridPosition?[] GetMatchedCells(Order order, IReadOnlyBoard board)
        {
            var matchedCells = new GridPosition?[order.Requirements.Count];
            Match(order, board, matchBuffer, matchedCells);
            return matchedCells;
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

        /// <summary>The new order starts with no remembered matches; see <see cref="RememberMatches"/>.</summary>
        public void Replace(int slotIndex, Order order)
        {
            slots[slotIndex] = order ?? throw new ArgumentNullException(nameof(order));
            claims[slotIndex] = null;
        }

        private void Restore(IReadOnlyList<Order> savedOrders, int savedNextOrderId)
        {
            if (savedOrders.Count != slots.Length)
                throw new ArgumentException(
                    $"The save holds {savedOrders.Count} orders but the game has {slots.Length} order slots.", nameof(savedOrders));

            var ids = new HashSet<int>();
            for (var i = 0; i < slots.Length; i++)
            {
                var saved = savedOrders[i] ?? throw new ArgumentException($"Saved order at slot {i} is null.", nameof(savedOrders));
                if (!ids.Add(saved.Id))
                    throw new ArgumentException($"Two saved orders share the id {saved.Id}.", nameof(savedOrders));
                if (saved.Id >= savedNextOrderId)
                    throw new ArgumentException($"Saved order {saved.Id} is not below the next order id {savedNextOrderId}.", nameof(savedOrders));
                if (saved.CustomerIndex >= settings.CustomerCount)
                    throw new ArgumentException(
                        $"Saved order {saved.Id} comes from customer {saved.CustomerIndex}, but there are only {settings.CustomerCount}.", nameof(savedOrders));
                try
                {
                    settings.EnsureAllows(saved.Requirements, catalog);
                }
                catch (InvalidOperationException exception)
                {
                    throw new ArgumentException($"Saved order {saved.Id} no longer fits the game: {exception.Message}", nameof(savedOrders), exception);
                }

                slots[i] = new Order(saved.Id, saved.CustomerIndex, saved.Requirements, settings.RewardFor(saved.Requirements, catalog));
            }

            nextOrderId = savedNextOrderId;
        }

        /// <summary>
        /// Uniform among customers no slot shows, so the customer leaving <paramref name="slotIndex"/> never comes
        /// straight back. Once every customer is shown, uniform among all but the leaving one; with a single customer,
        /// that customer.
        /// </summary>
        private int ChooseCustomer(int slotIndex)
        {
            var leaving = slots[slotIndex]?.CustomerIndex ?? -1;

            customerBuffer.Clear();
            for (var customer = 0; customer < settings.CustomerCount; customer++)
            {
                if (!IsShown(customer))
                    customerBuffer.Add(customer);
            }

            if (customerBuffer.Count == 0)
            {
                for (var customer = 0; customer < settings.CustomerCount; customer++)
                {
                    if (customer != leaving)
                        customerBuffer.Add(customer);
                }
            }

            if (customerBuffer.Count == 0)
                return leaving;
            return customerBuffer[random.Next(customerBuffer.Count)];
        }

        private bool IsShown(int customer)
        {
            foreach (var order in slots)
            {
                if (order != null && order.CustomerIndex == customer)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Records, for every active order, which board item each requirement is matched to now. Call after every
        /// change to the board: a requirement then keeps its item for as long as that same item instance stays on the
        /// board unlocked, even when an identical item appears earlier in row-major order or the item is moved.
        /// </summary>
        public void RememberMatches(IReadOnlyBoard board)
        {
            for (var slot = 0; slot < slots.Length; slot++)
            {
                var order = slots[slot];
                var cells = new GridPosition?[order.Requirements.Count];
                Match(order, board, matchBuffer, cells);

                var claim = new BoardItem[cells.Length];
                for (var i = 0; i < cells.Length; i++)
                {
                    if (cells[i].HasValue)
                        claim[i] = board.GetItem(cells[i].Value);
                }

                claims[slot] = claim;
            }
        }

        /// <summary>
        /// First every requirement keeps the item it was matched to last time (<see cref="RememberMatches"/>) if that
        /// item is still on the board, unlocked and unchanged. Then each requirement left in order takes the first
        /// unlocked matching item (row-major) no other requirement took. A missing requirement takes nothing, so
        /// later picks do not depend on whether the caller wants every cell (<paramref name="matchedCells"/> given).
        /// <paramref name="matched"/> lists the items in requirement order when every requirement is found.
        /// </summary>
        private bool Match(Order order, IReadOnlyBoard board, List<ConsumedItem> matched, GridPosition?[] matchedCells)
        {
            var count = order.Requirements.Count;
            var cells = matchedCells ?? new GridPosition?[count];
            var claim = ClaimOf(order);

            matched.Clear();
            for (var i = 0; i < count; i++)
            {
                cells[i] = null;
                var required = order.Requirements[i];
                if (claim != null && TryFindClaimedItem(claim[i], required, board, matched, out var position))
                {
                    cells[i] = position;
                    matched.Add(new ConsumedItem(position, required));
                }
            }

            var allFound = true;
            for (var i = 0; i < count; i++)
            {
                if (cells[i].HasValue)
                    continue;

                var required = order.Requirements[i];
                if (TryFindFreeItem(required, board, matched, out var position))
                {
                    cells[i] = position;
                    matched.Add(new ConsumedItem(position, required));
                }
                else
                {
                    allFound = false;
                }
            }

            matched.Clear();
            if (!allFound)
                return false;

            for (var i = 0; i < count; i++)
                matched.Add(new ConsumedItem(cells[i].Value, order.Requirements[i]));
            return true;
        }

        private BoardItem[] ClaimOf(Order order)
        {
            for (var slot = 0; slot < slots.Length; slot++)
            {
                if (ReferenceEquals(slots[slot], order))
                    return claims[slot];
            }

            return null;
        }

        private static bool TryFindClaimedItem(
            BoardItem claimed, ItemId wanted, IReadOnlyBoard board, List<ConsumedItem> taken, out GridPosition position)
        {
            position = default;
            if (claimed == null || claimed.IsLocked || claimed.ItemId != wanted)
                return false;

            foreach (var candidate in board.Positions)
            {
                if (board.TryGetItem(candidate, out var item) && ReferenceEquals(item, claimed))
                {
                    position = candidate;
                    return !IsTaken(taken, candidate);
                }
            }

            return false;
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
