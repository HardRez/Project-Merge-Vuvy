using System;
using System.Collections.Generic;
using MergeGame.Core.Orders;

namespace MergeGame.Core.Session
{
    /// <summary>
    /// Everything a session needs to carry on where it stopped: the items on the board, the score and the open orders.
    /// Taken with <see cref="MergeGameSession.CaptureSnapshot"/> and turned back into a session with
    /// <see cref="MergeGameSession.Resume"/>. Holds plain data only, so the app can write it to any storage.
    /// </summary>
    public sealed class GameSnapshot
    {
        /// <param name="items">Every occupied cell, each as the item put there; empty cells are left out.</param>
        /// <param name="orders">The open orders by slot; empty for a game without orders.</param>
        /// <param name="nextOrderId">The id the next generated order gets; above every id in <paramref name="orders"/>.</param>
        public GameSnapshot(int score, IReadOnlyList<InitialPlacement> items, IReadOnlyList<Order> orders, int nextOrderId)
        {
            if (score < 0)
                throw new ArgumentOutOfRangeException(nameof(score), score, "A saved score must not be negative.");
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (orders == null)
                throw new ArgumentNullException(nameof(orders));
            if (nextOrderId < 1)
                throw new ArgumentOutOfRangeException(nameof(nextOrderId), nextOrderId, "Order ids start at 1.");

            var orderCopy = new Order[orders.Count];
            for (var i = 0; i < orderCopy.Length; i++)
            {
                orderCopy[i] = orders[i] ?? throw new ArgumentException($"Saved order at slot {i} is null.", nameof(orders));
                if (orderCopy[i].Id >= nextOrderId)
                    throw new ArgumentException(
                        $"Saved order {orderCopy[i].Id} is not below the next order id {nextOrderId}.", nameof(nextOrderId));
            }

            var itemCopy = new InitialPlacement[items.Count];
            for (var i = 0; i < itemCopy.Length; i++)
                itemCopy[i] = items[i];

            Score = score;
            Items = Array.AsReadOnly(itemCopy);
            Orders = Array.AsReadOnly(orderCopy);
            NextOrderId = nextOrderId;
        }

        public int Score { get; }

        /// <summary>Every occupied cell in row-major order when captured from a session.</summary>
        public IReadOnlyList<InitialPlacement> Items { get; }

        /// <summary>One entry per order slot, in slot order.</summary>
        public IReadOnlyList<Order> Orders { get; }

        public int NextOrderId { get; }
    }
}
