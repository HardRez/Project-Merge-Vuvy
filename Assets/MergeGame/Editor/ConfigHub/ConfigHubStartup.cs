using UnityEditor;
using UnityEngine;

namespace MergeGame.Editor.ConfigHub
{
    /// <summary>
    /// Opens the Config Hub once when the editor starts (not on every script reload), unless it was switched off with
    /// the "Show on startup" toggle in the hub's footer.
    /// </summary>
    [InitializeOnLoad]
    internal static class ConfigHubStartup
    {
        private const string ShowOnStartupKey = "MergeGame.ConfigHub.ShowOnStartup";

        // SessionState survives domain reloads but not an editor restart, which is exactly "once per launch".
        private const string HandledThisLaunchKey = "MergeGame.ConfigHub.StartupHandled";

        static ConfigHubStartup()
        {
            if (Application.isBatchMode || SessionState.GetBool(HandledThisLaunchKey, false))
                return;

            SessionState.SetBool(HandledThisLaunchKey, true);
            if (ShowOnStartup)
                EditorApplication.delayCall += ConfigHubWindow.Open;
        }

        public static bool ShowOnStartup
        {
            get => EditorPrefs.GetBool(ShowOnStartupKey, true);
            set => EditorPrefs.SetBool(ShowOnStartupKey, value);
        }
    }
}
