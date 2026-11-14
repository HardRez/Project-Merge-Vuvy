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
    /// Pulses a merge suggestion while the player is idle. The first check happens
    /// <see cref="MergeHintSettings.IdleSeconds"/> after the last activity, every later one
    /// <see cref="MergeHintSettings.RepeatSeconds"/> after the previous check, whether or not that check found a pair.
    /// Each check queries the board afresh and plays the hint once if a merge exists, otherwise shows nothing.
    /// Any activity (a session event, a drag start, a tap) cancels a hint played since the last activity and restarts
    /// the wait toward <see cref="MergeHintSettings.IdleSeconds"/>. Time does not run during a drag.
    /// One <see cref="Advance"/> call runs at most one check; time beyond that check is dropped, so the next check
    /// comes a full <see cref="MergeHintSettings.RepeatSeconds"/> later instead of catching up after a long frame.
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
        private float secondsUntilCheck;
        private GridPosition? dragOrigin;
        private bool isDragging;
        private bool hasShownSinceActivity;

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
            secondsUntilCheck = settings.IdleSeconds;
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
            if (!isActive || isDragging || !(deltaSeconds > 0f))
                return;

            secondsUntilCheck -= deltaSeconds;
            if (secondsUntilCheck > 0f)
                return;

            secondsUntilCheck = settings.RepeatSeconds;
            if (!finder.TryFind(out var hint))
                return;

            hasShownSinceActivity = true;
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
            secondsUntilCheck = settings.IdleSeconds;
            if (!hasShownSinceActivity)
                return;

            hasShownSinceActivity = false;
            view.HideMergeHint();
        }
    }
}
