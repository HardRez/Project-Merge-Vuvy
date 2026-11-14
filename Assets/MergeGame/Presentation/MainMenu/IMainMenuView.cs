using System;

namespace MergeGame.Presentation.MainMenu
{
    public interface IMainMenuView
    {
        event Action PlayRequested;
        event Action QuitRequested;
    }
}
