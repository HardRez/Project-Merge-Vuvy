using System;
using MergeGame.Core.Events;
using MergeGame.Core.Session;
using MergeGame.Core.Spawning;
using MergeGame.Presentation.Events;
using MergeGame.Presentation.Notices;

namespace MergeGame.Presentation.Spawning
{
    /// <summary>Spawns on request and keeps the button available exactly while the board has an empty cell.</summary>
    public sealed class SpawnPresenter : IPresenter
    {
        private readonly MergeGameSession session;
        private readonly SessionEventStream events;
        private readonly ISpawnView view;
        private readonly INoticeView notices;
        private readonly SubscriptionBag subscriptions = new SubscriptionBag();

        public SpawnPresenter(MergeGameSession session, SessionEventStream events, ISpawnView view, INoticeView notices)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.notices = notices ?? throw new ArgumentNullException(nameof(notices));
        }

        public void Initialize()
        {
            view.SetSpawnAvailable(session.CanSpawn);

            view.SpawnRequested += OnSpawnRequested;
            subscriptions.Add(events.Subscribe<BoardFullChanged>(OnBoardFullChanged));
        }

        public void Dispose()
        {
            view.SpawnRequested -= OnSpawnRequested;
            subscriptions.Dispose();
        }

        private void OnSpawnRequested()
        {
            var result = session.Spawn();
            if (!result.Succeeded && result.FailureReason == SpawnFailureReason.BoardFull)
                notices.ShowNotice(Notice.BoardFull);
        }

        private void OnBoardFullChanged(BoardFullChanged changed) => view.SetSpawnAvailable(!changed.IsFull);
    }
}
