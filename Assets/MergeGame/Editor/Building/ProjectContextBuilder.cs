using System;
using System.IO;
using MergeGame.App.Installers;
using MergeGame.Data.Authoring;
using MergeGame.View.Audio;
using UnityEditor;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace MergeGame.Editor.Building
{
    /// <summary>Generates the Zenject project context prefab with the app-wide installer and the background music.</summary>
    internal static class ProjectContextBuilder
    {
        public static void Build(GameConfigAsset config)
        {
            var root = new GameObject("ProjectContext");
            try
            {
                var context = root.AddComponent<ProjectContext>();
                var installer = root.AddComponent<ProjectInstaller>().Wire("gameConfig", config);
                context.Installers = new MonoInstaller[] { installer };

                // The project context outlives scenes, so the music plays on across screen changes.
                root.AddComponent<MusicPlayer>().Wire("library", UiDataAssets.LoadOrCreateSoundLibrary());
                var music = root.GetComponent<AudioSource>();
                music.playOnAwake = false;
                music.loop = true;
                music.spatialBlend = 0f;

                AssetFolders.Ensure(Path.GetDirectoryName(BuildPaths.ProjectContextPrefab));
                PrefabUtility.SaveAsPrefabAsset(root, BuildPaths.ProjectContextPrefab, out var saved);
                if (!saved)
                    throw new InvalidOperationException($"Could not save {BuildPaths.ProjectContextPrefab}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
