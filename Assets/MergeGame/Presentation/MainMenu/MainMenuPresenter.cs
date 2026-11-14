using System;
using MergeGame.Presentation.Navigation;

namespace MergeGame.Presentation.MainMenu
{
    public sealed class MainMenuPresenter : IPresenter
    {
        private readonly IMainMenuView view;
        private readonly IAppNavigator navigator;

        public MainMenuPresenter(IMainMenuView view, IAppNavigator navigator)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        public void Initialize()
        {
            view.PlayRequested += OnPlayRequested;
            view.QuitRequested += OnQuitRequested;
        }

        public void Dispose()
        {
            view.PlayRequested -= OnPlayRequested;
            view.QuitRequested -= OnQuitRequested;
        }

        private void OnPlayRequested() => navigator.OpenGame();

        private void OnQuitRequested() => navigator.Quit();
    }
}
