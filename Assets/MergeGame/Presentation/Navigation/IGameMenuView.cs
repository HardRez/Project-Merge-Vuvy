using System;

namespace MergeGame.Presentation.Navigation
{
    public interface IGameMenuView
    {
        event Action BackToMenuRequested;
    }
}
