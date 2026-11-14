using MergeGame.App.Lifecycle;
using MergeGame.Presentation;
using MergeGame.Presentation.MainMenu;
using MergeGame.View.MainMenu;
using UnityEngine;
using Zenject;

namespace MergeGame.App.Installers
{
    public sealed class MainMenuInstaller : MonoInstaller
    {
        [SerializeField] private MainMenuView mainMenuView;

        public override void InstallBindings()
        {
            Container.Bind<IMainMenuView>().FromInstance(mainMenuView);
            Container.Bind<MainMenuPresenter>().AsSingle();
            Container.BindInterfacesTo<PresenterLifecycle>()
                .FromMethod(context => new PresenterLifecycle(new IPresenter[] { context.Container.Resolve<MainMenuPresenter>() }))
                .AsSingle();
        }
    }
}
