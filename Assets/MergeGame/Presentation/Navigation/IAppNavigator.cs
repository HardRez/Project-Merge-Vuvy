namespace MergeGame.Presentation.Navigation
{
    /// <summary>Switches between the app's screens; implemented by the Unity layer (scene loading, application quit).</summary>
    public interface IAppNavigator
    {
        void OpenGame();
        void OpenMainMenu();
        void Quit();
    }
}
