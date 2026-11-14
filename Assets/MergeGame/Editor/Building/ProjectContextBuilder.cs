using System;
using System.IO;
using MergeGame.App.Installers;
using MergeGame.Data.Authoring;
using UnityEditor;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace MergeGame.Editor.Building
{
    /// <summary>Generates the Zenject project context prefab with the app-wide installer.</summary>
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
