using System;
using MergeGame.Data.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// Generates everything the app needs to run: UI sprite import settings, the TextMesh Pro font, the UI data assets,
    /// the prefabs, the Zenject project context and both scenes, registered in the build settings with the main menu
    /// first. Idempotent: generated files are rewritten in place (keeping their GUIDs), while the theme and notice
    /// texts are only created when missing, so designers' edits to them survive a rebuild.
    /// </summary>
    public static class SceneBuilder
    {
        private const string LogPrefix = "[SceneBuilder]";

        [MenuItem("Merge Game/Build Scenes")]
        private static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            try
            {
                Build();
                EditorSceneManager.OpenScene(BuildPaths.MainMenuScene);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Build Scenes failed", exception.Message, "OK");
            }
        }

        /// <summary>Entry point for <c>-executeMethod</c>; exits the editor with code 1 on any failure so CI notices.</summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                Build();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void Build()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            SpriteImportSettings.Apply(new[] { BuildPaths.UiSpritesFolder }, UiSprites.BorderOf, UiSprites.IsReadAtRuntime);

            if (AssetDatabase.LoadAssetAtPath<GameConfigAsset>(ContentSeeder.GameConfigPath) == null)
                throw new InvalidOperationException($"{LogPrefix} {ContentSeeder.GameConfigPath} is missing; run Merge Game/Seed Content first.");

            // Opening a scene unloads assets loaded before it, so every step opens its scene first and then loads.
            SceneScaffold.NewScene();
            var theme = UiDataAssets.LoadOrCreateTheme();
            UiDataAssets.LoadOrCreateNoticeTexts(UiSprites.Load());
            UiFonts.Generate(theme);
            GamePrefabs.Generate(UiKit.Load());
            ProjectContextBuilder.Build(AssetDatabase.LoadAssetAtPath<GameConfigAsset>(ContentSeeder.GameConfigPath));

            MainMenuSceneBuilder.Build(ContentSeeder.GameConfigPath);
            GameSceneBuilder.Build();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BuildPaths.MainMenuScene, true),
                new EditorBuildSettingsScene(BuildPaths.GameScene, true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log($"{LogPrefix} Built the prefabs, the project context and the scenes {BuildPaths.MainMenuScene} and {BuildPaths.GameScene}.");
        }
    }
}
