using System;

namespace MergeGame.Presentation.Navigation
{
    public sealed class GameNavigationPresenter : IPresenter
    {
        private readonly IGameMenuView view;
        private readonly IAppNavigator navigator;

        public GameNavigationPresenter(IGameMenuView view, IAppNavigator navigator)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        public void Initialize() => view.BackToMenuRequested += OnBackToMenuRequested;

        public void Dispose() => view.BackToMenuRequested -= OnBackToMenuRequested;

        private void OnBackToMenuRequested() => navigator.OpenMainMenu();
    }
}
