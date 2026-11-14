using System;
using MergeGame.Core.Events;
using MergeGame.Core.Session;
using MergeGame.Presentation.Events;

namespace MergeGame.Presentation.Score
{
    public sealed class ScorePresenter : IPresenter
    {
        private readonly MergeGameSession session;
        private readonly SessionEventStream events;
        private readonly IScoreView view;
        private readonly SubscriptionBag subscriptions = new SubscriptionBag();

        public ScorePresenter(MergeGameSession session, SessionEventStream events, IScoreView view)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Initialize()
        {
            view.SetScore(session.Score);
            subscriptions.Add(events.Subscribe<ScoreChanged>(OnScoreChanged));
        }

        public void Dispose() => subscriptions.Dispose();

        private void OnScoreChanged(ScoreChanged changed) => view.ChangeScore(changed.PreviousScore, changed.Score);
    }
}
