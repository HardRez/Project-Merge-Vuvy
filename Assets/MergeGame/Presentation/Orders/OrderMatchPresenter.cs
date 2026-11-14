using System;
using System.Collections.Generic;
using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Session;
using MergeGame.Presentation.Events;

namespace MergeGame.Presentation.Orders
{
    /// <summary>
    /// Tells the board which cells hold an item an active order would take, using the session's delivery matching.
    /// A cell matched by several orders is reported once. The set is re-sent only when it changed.
    /// </summary>
    public sealed class OrderMatchPresenter : IPresenter
    {
        private readonly MergeGameSession session;
        private readonly SessionEventStream events;
        private readonly IOrderMatchView view;
        private readonly SubscriptionBag subscriptions = new SubscriptionBag();
        private readonly HashSet<GridPosition> matchedCells = new HashSet<GridPosition>();
        private List<GridPosition> shownCells = new List<GridPosition>();

        public OrderMatchPresenter(MergeGameSession session, SessionEventStream events, IOrderMatchView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Initialize()
        {
            shownCells = FindMatchedCells();
            view.ShowOrderMatches(shownCells.AsReadOnly());
            if (session.ActiveOrders.Count == 0)
                return;

            subscriptions.Add(events.Subscribe<ItemSpawned>(_ => Refresh()));
            subscriptions.Add(events.Subscribe<ItemMoved>(_ => Refresh()));
            subscriptions.Add(events.Subscribe<ItemsMerged>(_ => Refresh()));
            subscriptions.Add(events.Subscribe<ItemDiscarded>(_ => Refresh()));
            subscriptions.Add(events.Subscribe<OrderDelivered>(_ => Refresh()));
            subscriptions.Add(events.Subscribe<OrderAdded>(_ => Refresh()));
        }

        public void Dispose() => subscriptions.Dispose();

        private void Refresh()
        {
            var cells = FindMatchedCells();
            if (AreEqual(cells, shownCells))
                return;

            shownCells = cells;
            view.ShowOrderMatches(cells.AsReadOnly());
        }

        private List<GridPosition> FindMatchedCells()
        {
            matchedCells.Clear();
            foreach (var order in session.ActiveOrders)
            {
                foreach (var cell in session.GetRequirementMatches(order.Id))
                {
                    if (cell.HasValue)
                        matchedCells.Add(cell.Value);
                }
            }

            var inRowMajorOrder = new List<GridPosition>(matchedCells.Count);
            foreach (var position in session.Board.Positions)
            {
                if (matchedCells.Contains(position))
                    inRowMajorOrder.Add(position);
            }

            return inRowMajorOrder;
        }

        private static bool AreEqual(List<GridPosition> left, List<GridPosition> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }
    }
}
