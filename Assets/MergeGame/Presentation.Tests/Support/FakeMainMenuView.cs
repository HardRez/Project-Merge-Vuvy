using System;
using MergeGame.Presentation.MainMenu;

namespace MergeGame.Presentation.Tests.Support
{
    internal sealed class FakeMainMenuView : IMainMenuView
    {
        public event Action PlayRequested;
        public event Action QuitRequested;

        public bool HasInputListeners => PlayRequested != null || QuitRequested != null;

        public void PressPlay() => PlayRequested?.Invoke();
        public void PressQuit() => QuitRequested?.Invoke();
    }
}
