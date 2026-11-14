using System;

namespace MergeGame.Presentation.Navigation
{
    /// <summary>The game screen's navigation controls.</summary>
    public interface IGameMenuView
    {
        event Action BackToMenuRequested;
    }
}
