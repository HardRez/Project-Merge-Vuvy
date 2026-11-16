using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MergeGame.App.Installers;
using MergeGame.Data.Authoring;
using MergeGame.Editor.Building;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MergeGame.Editor.ConfigHub
{
    /// <summary>Finds the game's config assets, what they reference and what references them, and edits those links.</summary>
    internal static class HubAssets
    {
        private static readonly string[] SearchFolders = { "Assets" };

        // Asset path -> paths of the assets, prefabs and scenes that reference it directly. Built lazily, dropped on change.
        private static Dictionary<string, List<string>> usedBy;

        public static List<T> FindAll<T>() where T : Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name, SearchFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .Select(path => AssetDatabase.LoadAssetAtPath<T>(path))
                .Where(asset => asset != null)
                .ToList();

        public static GameObject ProjectContextPrefab => AssetDatabase.LoadAssetAtPath<GameObject>(BuildPaths.ProjectContextPrefab);

        public static ProjectInstaller ProjectInstaller
        {
            get
            {
                var prefab = ProjectContextPrefab;
                return prefab != null ? prefab.GetComponent<ProjectInstaller>() : null;
            }
        }

        /// <summary>The config the game actually plays: the one the project context installs.</summary>
        public static GameConfigAsset InstalledConfig
        {
            get
            {
                var installer = ProjectInstaller;
                if (installer == null)
                    return null;
                using var serialized = new SerializedObject(installer);
                return serialized.FindProperty("gameConfig")?.objectReferenceValue as GameConfigAsset;
            }
        }

        public static bool Install(GameConfigAsset config)
        {
            var installer = ProjectInstaller;
            if (installer == null)
                return false;
            SetReference(installer, "gameConfig", config);
            return true;
        }

        /// <summary>Points a serialized field at <paramref name="value"/> with undo, as the Inspector would.</summary>
        public static void SetReference(Object owner, string field, Object value)
        {
            using var serialized = new SerializedObject(owner);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
            Changed();
        }

        public static void AddChain(GameConfigAsset config, ItemChainAsset chain)
        {
            using var serialized = new SerializedObject(config);
            var chains = serialized.FindProperty("chains");
            chains.arraySize++;
            chains.GetArrayElementAtIndex(chains.arraySize - 1).objectReferenceValue = chain;
            serialized.ApplyModifiedProperties();
            Changed();
        }

        public static void RemoveChainAt(GameConfigAsset config, int index)
        {
            using var serialized = new SerializedObject(config);
            var chains = serialized.FindProperty("chains");
            chains.GetArrayElementAtIndex(index).objectReferenceValue = null;
            var before = chains.arraySize;
            chains.DeleteArrayElementAtIndex(index);
            // Older serialization only clears a reference element on the first delete.
            if (chains.arraySize == before)
                chains.DeleteArrayElementAtIndex(index);
            serialized.ApplyModifiedProperties();
            Changed();
        }

        public static void MoveChain(GameConfigAsset config, int from, int to)
        {
            using var serialized = new SerializedObject(config);
            var chains = serialized.FindProperty("chains");
            if (to < 0 || to >= chains.arraySize)
                return;
            chains.MoveArrayElement(from, to);
            serialized.ApplyModifiedProperties();
            Changed();
        }

        /// <summary>Creates and saves a new asset in <paramref name="folder"/> under a free name.</summary>
        public static T Create<T>(string folder, string fileName, Action<T> fill = null) where T : ScriptableObject
        {
            AssetFolders.Ensure(folder);
            var asset = ScriptableObject.CreateInstance<T>();
            fill?.Invoke(asset);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{fileName}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Changed();
            return asset;
        }

        public static ItemChainAsset CreateChain()
        {
            var taken = new HashSet<string>(FindAll<ItemChainAsset>().Select(chain => chain.Id));
            var id = "new_chain";
            for (var i = 2; taken.Contains(id); i++)
                id = "new_chain_" + i;

            return Create<ItemChainAsset>(ContentSeeder.ChainsFolder, id, chain =>
            {
                chain.Id = id;
                chain.DisplayName = "New Chain";
            });
        }

        /// <summary>The assets, prefabs and scenes <paramref name="asset"/> references directly.</summary>
        public static List<Object> Uses(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                return new List<Object>();

            return AssetDatabase.GetDependencies(path, false)
                .Where(dependency => dependency != path && IsHolder(dependency))
                .OrderBy(dependency => dependency, StringComparer.OrdinalIgnoreCase)
                .Select(AssetDatabase.LoadMainAssetAtPath)
                .Where(dependency => dependency != null)
                .ToList();
        }

        /// <summary>The assets, prefabs and scenes that reference <paramref name="asset"/> directly.</summary>
        public static List<Object> UsedBy(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                return new List<Object>();

            EnsureUsedBy();
            return usedBy.TryGetValue(path, out var holders)
                ? holders.Select(AssetDatabase.LoadMainAssetAtPath).Where(holder => holder != null).ToList()
                : new List<Object>();
        }

        /// <summary>Forgets cached reference lookups; call after assets change.</summary>
        public static void Changed() => usedBy = null;

        private static void EnsureUsedBy()
        {
            if (usedBy != null)
                return;

            usedBy = new Dictionary<string, List<string>>();
            var holders = new[] { "t:ScriptableObject", "t:Prefab", "t:Scene" }
                .SelectMany(filter => AssetDatabase.FindAssets(filter, SearchFolders))
                .Select(AssetDatabase.GUIDToAssetPath)
                .Distinct()
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

            foreach (var holder in holders)
            {
                foreach (var dependency in AssetDatabase.GetDependencies(holder, false))
                {
                    if (dependency == holder)
                        continue;
                    if (!usedBy.TryGetValue(dependency, out var list))
                        usedBy.Add(dependency, list = new List<string>());
                    list.Add(holder);
                }
            }
        }

        private static bool IsHolder(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            return extension == ".asset" || extension == ".prefab" || extension == ".unity";
        }
    }
}
