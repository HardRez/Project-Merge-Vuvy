using System;
using UnityEngine;

namespace MergeGame.App.Navigation
{
    /// <summary>The scenes of the app's screens, as registered in the build settings.</summary>
    [Serializable]
    public sealed class SceneNames
    {
        [SerializeField] private string mainMenu = "MainMenu";
        [SerializeField] private string game = "Game";

        public string MainMenu => mainMenu;
        public string Game => game;
    }
}
