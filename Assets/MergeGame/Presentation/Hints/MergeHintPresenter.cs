using System;
using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Hints;
using MergeGame.Core.Session;
using MergeGame.Presentation.Board;
using MergeGame.Presentation.Events;
using MergeGame.Presentation.Selection;

namespace MergeGame.Presentation.Hints
{
    /// <summary>
    /// Suggests a merge once the player has been idle for <see cref="MergeHintSettings.IdleSeconds"/>. Any activity
    /// (a session event, a drag start, a tap) hides a shown hint and restarts the idle time, so a hint never points at
    /// cells that changed since. The idle time does not run during a drag. When an idle period ends without any
    /// possible merge, nothing is shown and the board is checked again only after the next full idle period.
    /// </summary>
    public sealed class MergeHintPresenter : IPresenter, ITimeDriven
    {
        private readonly SessionEventStream events;
        private readonly IBoardView boardView;
        private readonly IBoardSelectionView selectionView;
        private readonly IMergeHintView view;
        private readonly MergeHintSettings settings;
        private readonly MergeHintFinder finder;
        private readonly SubscriptionBag subscriptions = new SubscriptionBag();

        private bool isActive;
        private float idleSeconds;
        private GridPosition? dragOrigin;
        private bool isDragging;
        private bool isHintShown;

        public MergeHintPresenter(
            MergeGameSession session,
            SessionEventStream events,
            IBoardView boardView,
            IBoardSelectionView selectionView,
            IMergeHintView view,
            MergeHintSettings settings)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.boardView = boardView ?? throw new ArgumentNullException(nameof(boardView));
            this.selectionView = selectionView ?? throw new ArgumentNullException(nameof(selectionView));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            finder = new MergeHintFinder(session);
        }

        public void Initialize()
        {
            boardView.DragStarted += OnDragStarted;
            boardView.DragReleased += OnDragReleased;
            selectionView.CellTapped += OnCellTapped;
            subscriptions.Add(events.Subscribe<GameEvent>(OnGameEvent));
            isActive = true;
        }

        public void Dispose()
        {
            isActive = false;
            boardView.DragStarted -= OnDragStarted;
            boardView.DragReleased -= OnDragReleased;
            selectionView.CellTapped -= OnCellTapped;
            subscriptions.Dispose();
        }

        public void Advance(float deltaSeconds)
        {
            if (!isActive || isDragging || isHintShown || !(deltaSeconds > 0f))
                return;

            idleSeconds += deltaSeconds;
            if (idleSeconds < settings.IdleSeconds)
                return;

            idleSeconds = 0f;
            if (!finder.TryFind(out var hint))
                return;

            isHintShown = true;
            view.ShowMergeHint(hint.From, hint.To);
        }

        private void OnDragStarted(GridPosition origin)
        {
            isDragging = true;
            dragOrigin = origin;
            RestartIdleTime();
        }

        private void OnDragReleased(GridPosition? cell) => EndDrag();

        private void OnCellTapped(GridPosition position) => RestartIdleTime();

        private void OnGameEvent(GameEvent gameEvent)
        {
            if (RemovesDraggedItem(gameEvent))
                EndDrag();
            else
                RestartIdleTime();
        }

        private bool RemovesDraggedItem(GameEvent gameEvent)
        {
            if (!dragOrigin.HasValue)
                return false;

            switch (gameEvent)
            {
                case ItemDiscarded discarded:
                    return discarded.Position == dragOrigin.Value;
                case OrderDelivered delivered:
                    foreach (var consumed in delivered.ConsumedItems)
                    {
                        if (consumed.Position == dragOrigin.Value)
                            return true;
                    }

                    return false;
                default:
                    return false;
            }
        }

        private void EndDrag()
        {
            isDragging = false;
            dragOrigin = null;
            RestartIdleTime();
        }

        private void RestartIdleTime()
        {
            idleSeconds = 0f;
            if (!isHintShown)
                return;

            isHintShown = false;
            view.HideMergeHint();
        }
    }
}
