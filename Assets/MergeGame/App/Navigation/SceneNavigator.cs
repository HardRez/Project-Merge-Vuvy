using System;
using MergeGame.Presentation.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MergeGame.App.Navigation
{
    /// <summary>One scene per screen; every switch loads the target scene from scratch, so the game screen reopens the saved game.</summary>
    public sealed class SceneNavigator : IAppNavigator
    {
        private readonly SceneNames scenes;

        public SceneNavigator(SceneNames scenes)
        {
            this.scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
        }

        public void OpenGame() => SceneManager.LoadScene(scenes.Game);

        public void OpenMainMenu() => SceneManager.LoadScene(scenes.MainMenu);

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
