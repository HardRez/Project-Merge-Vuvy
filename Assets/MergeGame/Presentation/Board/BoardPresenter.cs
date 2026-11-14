using System;
using System.Collections.Generic;
using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Rules;
using MergeGame.Core.Session;
using MergeGame.Presentation.Events;
using MergeGame.Presentation.Notices;

namespace MergeGame.Presentation.Board
{
    /// <summary>
    /// Drag and drop on the grid: decides what can be picked up, previews the hovered cell, executes the drop and mirrors
    /// every board change of the session on the view.
    /// </summary>
    public sealed class BoardPresenter : IPresenter
    {
        private readonly MergeGameSession session;
        private readonly SessionEventStream events;
        private readonly IBoardView view;
        private readonly INoticeView notices;
        private readonly SubscriptionBag subscriptions = new SubscriptionBag();

        private GridPosition? dragOrigin;
        private GridPosition? hoveredCell;
        private GridPosition? highlightedCell;
        private DropHighlight shownHighlight;

        public BoardPresenter(MergeGameSession session, SessionEventStream events, IBoardView view, INoticeView notices)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.notices = notices ?? throw new ArgumentNullException(nameof(notices));
        }

        private IReadOnlyBoard Board => session.Board;

        public void Initialize()
        {
            view.CreateGrid(Board.Size);
            foreach (var position in Board.Positions)
            {
                if (Board.TryGetItem(position, out var item))
                    view.PlaceItem(position, item.ItemId, item.IsLocked);
            }

            view.DragStarted += OnDragStarted;
            view.DragMoved += OnDragMoved;
            view.DragReleased += OnDragReleased;

            subscriptions.Add(events.Subscribe<ItemSpawned>(OnItemSpawned));
            subscriptions.Add(events.Subscribe<ItemMoved>(OnItemMoved));
            subscriptions.Add(events.Subscribe<ItemsMerged>(OnItemsMerged));
            subscriptions.Add(events.Subscribe<DropRejected>(OnDropRejected));
            subscriptions.Add(events.Subscribe<ItemDiscarded>(OnItemDiscarded));
            subscriptions.Add(events.Subscribe<OrderDelivered>(OnOrderDelivered));
        }

        public void Dispose()
        {
            view.DragStarted -= OnDragStarted;
            view.DragMoved -= OnDragMoved;
            view.DragReleased -= OnDragReleased;
            subscriptions.Dispose();
        }

        private void OnDragStarted(GridPosition origin)
        {
            if (dragOrigin.HasValue || !Board.IsInside(origin) || !Board.TryGetItem(origin, out var item))
                return;

            if (item.IsLocked)
            {
                view.RefuseDrag(origin);
                notices.ShowNotice(Notice.ItemIsLocked);
                return;
            }

            dragOrigin = origin;
            view.BeginDrag(origin);
        }

        private void OnDragMoved(GridPosition? cell)
        {
            if (!dragOrigin.HasValue)
                return;

            hoveredCell = cell;
            UpdateHighlight();
        }

        private void OnDragReleased(GridPosition? cell)
        {
            if (!dragOrigin.HasValue)
                return;

            var origin = dragOrigin.Value;
            EndDrag();

            if (IsDropTarget(cell, origin))
                session.Drop(origin, cell.Value);
            else
                view.ReturnItem(origin);
        }

        private void OnItemSpawned(ItemSpawned spawned)
        {
            view.SpawnItem(spawned.Position, spawned.ItemId);
            UpdateHighlight();
        }

        private void OnItemMoved(ItemMoved moved)
        {
            view.MoveItem(moved.From, moved.To);
            UpdateHighlight();
        }

        private void OnItemsMerged(ItemsMerged merged)
        {
            view.MergeItems(merged.From, merged.To, merged.ResultItemId, merged.TargetWasLocked);
            if (merged.AwardedPoints > 0)
                view.ShowPointsPopup(merged.To, merged.AwardedPoints);
            UpdateHighlight();
        }

        private void OnDropRejected(DropRejected rejected)
        {
            if (rejected.Reason != DropRejectReason.SourceEmpty)
                view.ReturnItem(rejected.From);
            if (TryGetNotice(rejected.Reason, out var notice))
                notices.ShowNotice(notice);
        }

        private void OnItemDiscarded(ItemDiscarded discarded)
        {
            if (dragOrigin == discarded.Position)
                EndDrag();

            view.DiscardItem(discarded.Position);
            UpdateHighlight();
        }

        private void OnOrderDelivered(OrderDelivered delivered)
        {
            var positions = new List<GridPosition>(delivered.ConsumedItems.Count);
            foreach (var consumed in delivered.ConsumedItems)
                positions.Add(consumed.Position);

            if (dragOrigin.HasValue && positions.Contains(dragOrigin.Value))
                EndDrag();

            view.DeliverItems(delivered.SlotIndex, positions.AsReadOnly());
            UpdateHighlight();
        }

        /// <summary>Shows what a drop on the hovered cell would do; also re-run after board changes during a drag.</summary>
        private void UpdateHighlight()
        {
            if (!dragOrigin.HasValue || !IsDropTarget(hoveredCell, dragOrigin.Value))
            {
                ClearHighlight();
                return;
            }

            var cell = hoveredCell.Value;
            var highlight = ToHighlight(session.PreviewDrop(dragOrigin.Value, cell));
            if (highlightedCell == cell && shownHighlight == highlight)
                return;

            highlightedCell = cell;
            shownHighlight = highlight;
            view.ShowDropHighlight(cell, highlight);
        }

        private void ClearHighlight()
        {
            if (!highlightedCell.HasValue)
                return;

            highlightedCell = null;
            view.ClearDropHighlight();
        }

        private void EndDrag()
        {
            dragOrigin = null;
            hoveredCell = null;
            ClearHighlight();
        }

        private bool IsDropTarget(GridPosition? cell, GridPosition origin) =>
            cell.HasValue && cell.Value != origin && Board.IsInside(cell.Value);

        private static DropHighlight ToHighlight(DropOutcome outcome)
        {
            switch (outcome.Kind)
            {
                case DropOutcomeKind.Move:
                    return DropHighlight.MoveToEmptyCell;
                case DropOutcomeKind.Merge:
                    return DropHighlight.Merge;
                default:
                    return outcome.RejectReason == DropRejectReason.MaxLevel ? DropHighlight.MaxLevel : DropHighlight.Invalid;
            }
        }

        private static bool TryGetNotice(DropRejectReason reason, out Notice notice)
        {
            switch (reason)
            {
                case DropRejectReason.MaxLevel:
                    notice = Notice.MaxLevelReached;
                    return true;
                case DropRejectReason.DifferentChain:
                    notice = Notice.DifferentChain;
                    return true;
                case DropRejectReason.DifferentLevel:
                    notice = Notice.DifferentLevel;
                    return true;
                case DropRejectReason.SourceLocked:
                    notice = Notice.ItemIsLocked;
                    return true;
                default:
                    notice = default;
                    return false;
            }
        }
    }
}
