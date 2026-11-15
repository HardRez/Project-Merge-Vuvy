using System;
using MergeGame.Presentation.Navigation;

namespace MergeGame.Presentation.Cheats
{
    /// <summary>Carries out the cheats: resetting the save forgets the progress and opens a new game straight away.</summary>
    public sealed class CheatsPresenter : IPresenter
    {
        private readonly ICheatsView view;
        private readonly IGameProgress progress;
        private readonly IAppNavigator navigator;

        public CheatsPresenter(ICheatsView view, IGameProgress progress, IAppNavigator navigator)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        public void Initialize() => view.ResetSaveRequested += OnResetSaveRequested;

        public void Dispose() => view.ResetSaveRequested -= OnResetSaveRequested;

        private void OnResetSaveRequested()
        {
            progress.ResetProgress();
            navigator.OpenGame();
        }
    }
}
