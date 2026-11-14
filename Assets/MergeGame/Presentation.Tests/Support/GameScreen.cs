using System;
using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Session;
using MergeGame.Presentation.Board;
using MergeGame.Presentation.Events;
using MergeGame.Presentation.Hints;
using MergeGame.Presentation.Navigation;
using MergeGame.Presentation.Orders;
using MergeGame.Presentation.Score;
using MergeGame.Presentation.Selection;
using MergeGame.Presentation.Spawning;

namespace MergeGame.Presentation.Tests.Support
{
    /// <summary>
    /// The whole game screen wired the way the composition root wires it, with fake views sharing one log.
    /// </summary>
    internal sealed class GameScreen : IDisposable
    {
        private readonly List<IPresenter> presenters = new List<IPresenter>();

        public GameScreen(MergeGameSession session)
        {
            Session = session;
            Events = new SessionEventStream(session);
            Board = new FakeBoardView(Log);
            Selection = new FakeSelectionView(Log);
            Info = new FakeItemInfoView(Log);
            Orders = new FakeOrdersView(Log);
            Score = new FakeScoreView(Log);
            Spawn = new FakeSpawnView(Log);
            Notices = new FakeNoticeView(Log);
            Hint = new FakeMergeHintView(Log);

            SelectionPresenter = new SelectionPresenter(session, Events, Selection, Info, Notices);
            presenters.Add(new BoardPresenter(session, Events, Board, Notices));
            presenters.Add(SelectionPresenter);
            presenters.Add(new OrdersPresenter(session, Events, Orders, Notices));
            presenters.Add(new ScorePresenter(session, Events, Score));
            presenters.Add(new SpawnPresenter(session, Events, Spawn, Notices));
            presenters.Add(new GameNavigationPresenter(Menu, Navigator));
            HintPresenter = new MergeHintPresenter(session, Events, Board, Selection, Hint, new MergeHintSettings(HintDelaySeconds));
            presenters.Add(HintPresenter);

            foreach (var presenter in presenters)
                presenter.Initialize();
        }

        public const float HintDelaySeconds = 5f;

        public ViewLog Log { get; } = new ViewLog();
        public MergeGameSession Session { get; }
        public SessionEventStream Events { get; }
        public FakeBoardView Board { get; }
        public FakeSelectionView Selection { get; }
        public FakeItemInfoView Info { get; }
        public FakeOrdersView Orders { get; }
        public FakeScoreView Score { get; }
        public FakeSpawnView Spawn { get; }
        public FakeNoticeView Notices { get; }
        public FakeGameMenuView Menu { get; } = new FakeGameMenuView();
        public FakeAppNavigator Navigator { get; } = new FakeAppNavigator();
        public FakeMergeHintView Hint { get; }
        public SelectionPresenter SelectionPresenter { get; }
        public MergeHintPresenter HintPresenter { get; }

        /// <summary>Lets time pass the way the composition root's per-frame tick does.</summary>
        public void Advance(float deltaSeconds) => HintPresenter.Advance(deltaSeconds);

        public static GameScreen For(GameBuilder builder) => new GameScreen(builder.Build());

        /// <summary>What the session's board holds, in the shape the fake board view keeps.</summary>
        public Dictionary<GridPosition, ShownItem> SessionItems()
        {
            var items = new Dictionary<GridPosition, ShownItem>();
            foreach (var position in Session.Board.Positions)
            {
                if (Session.Board.TryGetItem(position, out var item))
                    items.Add(position, item.IsHidden ? ShownItem.Box : new ShownItem(item.ItemId, item.IsLocked));
            }

            return items;
        }

        public void Dispose()
        {
            for (var i = presenters.Count - 1; i >= 0; i--)
                presenters[i].Dispose();
            Events.Dispose();
        }
    }
}
