using System;
using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Session;
using MergeGame.Presentation.Events;
using MergeGame.Presentation.Notices;

namespace MergeGame.Presentation.Selection
{
    /// <summary>
    /// Keeps one optional selected cell that always holds an item: a moved item stays selected, a merge selects its
    /// result and a discarded or delivered item clears the selection. The info bar describes the selected item.
    /// </summary>
    public sealed class SelectionPresenter : IPresenter
    {
        private readonly MergeGameSession session;
        private readonly SessionEventStream events;
        private readonly IBoardSelectionView selectionView;
        private readonly IItemInfoView infoView;
        private readonly INoticeView notices;
        private readonly SubscriptionBag subscriptions = new SubscriptionBag();

        public SelectionPresenter(
            MergeGameSession session,
            SessionEventStream events,
            IBoardSelectionView selectionView,
            IItemInfoView infoView,
            INoticeView notices)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.selectionView = selectionView ?? throw new ArgumentNullException(nameof(selectionView));
            this.infoView = infoView ?? throw new ArgumentNullException(nameof(infoView));
            this.notices = notices ?? throw new ArgumentNullException(nameof(notices));
        }

        public GridPosition? SelectedPosition { get; private set; }

        public void Initialize()
        {
            infoView.ShowHint();

            selectionView.CellTapped += OnCellTapped;
            infoView.DiscardRequested += OnDiscardRequested;

            subscriptions.Add(events.Subscribe<ItemMoved>(OnItemMoved));
            subscriptions.Add(events.Subscribe<ItemsMerged>(OnItemsMerged));
            subscriptions.Add(events.Subscribe<ItemDiscarded>(OnItemDiscarded));
            subscriptions.Add(events.Subscribe<OrderDelivered>(OnOrderDelivered));
        }

        public void Dispose()
        {
            selectionView.CellTapped -= OnCellTapped;
            infoView.DiscardRequested -= OnDiscardRequested;
            subscriptions.Dispose();
        }

        private void OnCellTapped(GridPosition position)
        {
            if (session.Board.IsInside(position) && !session.Board.IsEmpty(position))
                Select(position);
            else
                ClearSelection();
        }

        private void OnDiscardRequested()
        {
            if (!SelectedPosition.HasValue)
                return;

            var result = session.Discard(SelectedPosition.Value);
            if (!result.Succeeded && result.FailureReason == DiscardFailureReason.ItemLocked)
                notices.ShowNotice(Notice.LockedItemCannotBeDiscarded);
        }

        private void OnItemMoved(ItemMoved moved)
        {
            if (SelectedPosition == moved.From)
                Select(moved.To);
        }

        private void OnItemsMerged(ItemsMerged merged) => Select(merged.To);

        private void OnItemDiscarded(ItemDiscarded discarded)
        {
            if (SelectedPosition == discarded.Position)
                ClearSelection();
        }

        private void OnOrderDelivered(OrderDelivered delivered)
        {
            foreach (var consumed in delivered.ConsumedItems)
            {
                if (SelectedPosition == consumed.Position)
                {
                    ClearSelection();
                    return;
                }
            }
        }

        private void Select(GridPosition position)
        {
            SelectedPosition = position;
            selectionView.ShowSelectionMarker(position);
            infoView.ShowItem(DescribeItemAt(position));
        }

        private void ClearSelection()
        {
            if (!SelectedPosition.HasValue)
                return;

            SelectedPosition = null;
            selectionView.HideSelectionMarker();
            infoView.ShowHint();
        }

        private ItemInfo DescribeItemAt(GridPosition position)
        {
            var catalog = session.Catalog;
            var item = session.Board.GetItem(position);
            var itemId = item.ItemId;
            return new ItemInfo(
                position,
                itemId,
                catalog.GetLevel(itemId).DisplayName,
                catalog.GetChain(itemId.ChainId).MaxLevel,
                item.IsLocked,
                catalog.TryGetNext(itemId, out var next) ? next : (ItemId?)null,
                session.CanDiscard(position));
        }
    }
}
