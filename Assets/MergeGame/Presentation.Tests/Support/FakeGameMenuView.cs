using System;
using MergeGame.Presentation.Navigation;

namespace MergeGame.Presentation.Tests.Support
{
    internal sealed class FakeGameMenuView : IGameMenuView
    {
        public event Action BackToMenuRequested;

        public bool HasInputListeners => BackToMenuRequested != null;

        public void PressBackToMenu() => BackToMenuRequested?.Invoke();
    }
}
