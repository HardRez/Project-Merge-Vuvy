using MergeGame.App.Lifecycle;
using MergeGame.Core.Items;
using MergeGame.Core.Session;
using MergeGame.Core.Spawning;
using MergeGame.Data.Authoring;
using MergeGame.Data.Definitions;
using MergeGame.Data.Visuals;
using MergeGame.Presentation;
using MergeGame.Presentation.Board;
using MergeGame.Presentation.Events;
using MergeGame.Presentation.Hints;
using MergeGame.Presentation.Navigation;
using MergeGame.Presentation.Notices;
using MergeGame.Presentation.Orders;
using MergeGame.Presentation.Score;
using MergeGame.Presentation.Selection;
using MergeGame.Presentation.Settings;
using MergeGame.Presentation.Spawning;
using MergeGame.View.Board;
using MergeGame.View.Hints;
using MergeGame.View.Navigation;
using MergeGame.View.Notices;
using MergeGame.View.Orders;
using MergeGame.View.Score;
using MergeGame.View.Selection;
using MergeGame.View.Settings;
using MergeGame.View.Spawning;
using UnityEngine;
using Zenject;

namespace MergeGame.App.Installers
{
    /// <summary>
    /// Composition root of the game screen. The scene container lives exactly as long as the scene, so every load of
    /// the Game scene starts a new session.
    /// </summary>
    public sealed class GameInstaller : MonoInstaller
    {
        [Header("Views")]
        [SerializeField] private BoardView boardView;
        [SerializeField] private ItemInfoView itemInfoView;
        [SerializeField] private OrdersView ordersView;
        [SerializeField] private ScoreView scoreView;
        [SerializeField] private SpawnView spawnView;
        [SerializeField] private NoticeView noticeView;
        [SerializeField] private GameMenuView gameMenuView;
        [SerializeField] private MergeHintView mergeHintView;
        [SerializeField] private OrderMatchView orderMatchView;
        [SerializeField] private SettingsView settingsView;

        [Header("Tuning")]
        [Tooltip("Seconds without any player action before a possible merge is first pointed out (one short pulse).")]
        [Min(0.1f)]
        [SerializeField] private float mergeHintIdleSeconds = 5f;

        [Tooltip("While the player stays idle, seconds between one hint check and the next; each check pulses the hint " +
                 "once if a merge exists. Keep it longer than the pulse itself.")]
        [Min(0.1f)]
        [SerializeField] private float mergeHintRepeatSeconds = 4f;

        [Header("Pools")]
        [SerializeField] private ItemView itemPrefab;
        [SerializeField] private PointsPopup pointsPopupPrefab;
        [SerializeField] private Transform poolRoot;

        [Tooltip("Board items created up front; the pool grows on demand beyond this.")]
        [SerializeField] private int initialItemPoolSize = 30;

        public override void InstallBindings()
        {
            BindGame();
            BindViews();
            BindPools();
            BindPresenters();
        }

        private void BindGame()
        {
            Container.Bind<GameDefinition>()
                .FromMethod(context => new GameDefinitionFactory().Create(context.Container.Resolve<GameConfigAsset>()))
                .AsSingle();
            Container.Bind<ItemCatalog>().FromResolveGetter<GameDefinition>(definition => definition.Catalog).AsSingle();
            Container.Bind<MergeGameSession>()
                .FromMethod(context => new MergeGameSession(context.Container.Resolve<GameDefinition>(), context.Container.Resolve<IRandom>()))
                .AsSingle();
            Container.Bind<SessionEventStream>().AsSingle();
            Container.Bind<IItemVisuals>().To<ItemVisualLibrary>().AsSingle();
        }

        private void BindViews()
        {
            Container.Bind(typeof(IBoardView), typeof(IBoardSelectionView)).FromInstance(boardView);
            Container.Bind<IItemInfoView>().FromInstance(itemInfoView);
            Container.Bind(typeof(IOrdersView), typeof(IOrderSlotTargets)).FromInstance(ordersView);
            Container.Bind<IScoreView>().FromInstance(scoreView);
            Container.Bind<ISpawnView>().FromInstance(spawnView);
            Container.Bind<INoticeView>().FromInstance(noticeView);
            Container.Bind<IGameMenuView>().FromInstance(gameMenuView);
            Container.Bind<IMergeHintView>().FromInstance(mergeHintView);
            Container.Bind<IOrderMatchView>().FromInstance(orderMatchView);
            Container.Bind<ISettingsView>().FromInstance(settingsView);
        }

        private void BindPools()
        {
            Container.BindMemoryPool<ItemView, ItemView.Pool>()
                .WithInitialSize(initialItemPoolSize)
                .FromComponentInNewPrefab(itemPrefab)
                .UnderTransform(poolRoot);
            Container.BindMemoryPool<PointsPopup, PointsPopup.Pool>()
                .WithInitialSize(4)
                .FromComponentInNewPrefab(pointsPopupPrefab)
                .UnderTransform(poolRoot);
        }

        private void BindPresenters()
        {
            Container.Bind<BoardPresenter>().AsSingle();
            Container.Bind<SelectionPresenter>().AsSingle();
            Container.Bind<OrdersPresenter>().AsSingle();
            Container.Bind<OrderMatchPresenter>().AsSingle();
            Container.Bind<ScorePresenter>().AsSingle();
            Container.Bind<SpawnPresenter>().AsSingle();
            Container.Bind<GameNavigationPresenter>().AsSingle();
            Container.Bind<SettingsPresenter>().AsSingle();
            Container.Bind<MergeHintSettings>().FromInstance(new MergeHintSettings(mergeHintIdleSeconds, mergeHintRepeatSeconds));
            Container.Bind(typeof(MergeHintPresenter), typeof(ITimeDriven)).To<MergeHintPresenter>().AsSingle();
            Container.BindInterfacesTo<TimeDrivenTicker>().AsSingle();

            // The order matters: the board must exist before the selection and orders describe it, and the hint
            // presenter comes last so it sees every other presenter's reaction to an event first.
            Container.BindInterfacesTo<PresenterLifecycle>().FromMethod(CreateLifecycle).AsSingle();
        }

        private static PresenterLifecycle CreateLifecycle(InjectContext context)
        {
            var container = context.Container;
            var presenters = new IPresenter[]
            {
                container.Resolve<BoardPresenter>(),
                container.Resolve<SelectionPresenter>(),
                container.Resolve<OrdersPresenter>(),
                container.Resolve<OrderMatchPresenter>(),
                container.Resolve<ScorePresenter>(),
                container.Resolve<SpawnPresenter>(),
                container.Resolve<GameNavigationPresenter>(),
                container.Resolve<SettingsPresenter>(),
                container.Resolve<MergeHintPresenter>()
            };
            return new PresenterLifecycle(presenters, container.Resolve<SessionEventStream>());
        }
    }
}
