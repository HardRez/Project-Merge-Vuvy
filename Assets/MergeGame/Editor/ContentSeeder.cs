using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MergeGame.Data.Authoring;
using MergeGame.Data.Validation;
using MergeGame.Editor.Seed;
using UnityEditor;
using UnityEngine;

namespace MergeGame.Editor
{
    /// <summary>
    /// Generates the shipped config assets from <c>seed_content.json</c>. Idempotent: existing assets are updated in
    /// place so their GUIDs (referenced by scenes and prefabs) never change, and chains no longer in the seed are deleted.
    /// Sprites are found by convention, so a new chain only needs its JSON entry and its PNGs.
    /// </summary>
    public static class ContentSeeder
    {
        public const string SeedPath = "Assets/MergeGame/Editor/Seed/seed_content.json";
        public const string ConfigFolder = "Assets/MergeGame/Content/Config";
        public const string ChainsFolder = ConfigFolder + "/Chains";
        public const string GameConfigPath = ConfigFolder + "/GameConfig.asset";
        public const string ScoreTablePath = ConfigFolder + "/ScoreTable.asset";
        public const string OrderSettingsPath = ConfigFolder + "/OrderSettings.asset";
        public const string BoardLayoutPath = ConfigFolder + "/BoardLayout.asset";
        public const string ItemSpritesFolder = "Assets/MergeGame/Content/Sprites/Items";
        public const string IconSpritesFolder = "Assets/MergeGame/Content/Sprites/Icons";

        private const string LogPrefix = "[ContentSeeder]";

        // Chain ids become file and folder names.
        private static readonly Regex ChainIdPattern = new Regex("^[a-z0-9_]+$");

        [MenuItem("Merge Game/Seed Content")]
        private static void SeedFromMenu()
        {
            try
            {
                var config = Seed();
                Selection.activeObject = config;
                EditorGUIUtility.PingObject(config);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Seed Content failed", exception.Message, "OK");
            }
        }

        /// <summary>Entry point for <c>-executeMethod</c>; exits the editor with code 1 on any failure so CI notices.</summary>
        public static void SeedFromCommandLine()
        {
            try
            {
                Seed();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Normalises sprite imports, writes every config asset and validates the result.</summary>
        /// <exception cref="InvalidOperationException">The seed is invalid, a sprite is missing or the result does not validate.</exception>
        public static GameConfigAsset Seed()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var reimported = SpriteImportSettings.Apply(ItemSpritesFolder, IconSpritesFolder);

            var seed = ReadSeed();
            var missingSprites = new List<string>();
            var chainIcons = seed.chains.ToDictionary(chain => chain.id, chain => LoadChainIcons(chain, missingSprites));
            var avatars = LoadCustomerAvatars(seed.orders, missingSprites);
            if (missingSprites.Count > 0)
                throw new InvalidOperationException($"{LogPrefix} Missing sprites (expected by naming convention):\n  " + string.Join("\n  ", missingSprites));

            EnsureFolder(ChainsFolder);
            var chains = seed.chains.Select(chain => WriteChain(chain, chainIcons[chain.id])).ToList();
            var config = LoadOrCreate<GameConfigAsset>(GameConfigPath);
            config.BoardWidth = seed.board.width;
            config.BoardHeight = seed.board.height;
            config.Chains = chains;
            config.ScoreTable = WriteScoreTable(seed.scoreTable);
            config.Orders = WriteOrderSettings(seed.orders, avatars);
            config.InitialLayout = WriteBoardLayout(seed.initialLayout, chains);
            EditorUtility.SetDirty(config);
            var deleted = DeleteStaleChains(chains);
            AssetDatabase.SaveAssets();

            var problems = GameConfigValidator.Validate(config);
            if (problems.Count > 0)
                throw new InvalidOperationException($"{LogPrefix} {GameConfigPath} has problems:\n  " + string.Join("\n  ", problems));

            Debug.Log($"{LogPrefix} Seeded {GameConfigPath}: {chains.Count} chains ({string.Join(", ", chains.Select(chain => chain.Id))}), " +
                      $"{config.BoardWidth}x{config.BoardHeight} board, {config.InitialLayout.Placements.Count} initial placements, " +
                      $"{avatars.Count} customers; re-imported {reimported.Count} sprites; deleted {deleted.Count} stale chain assets" +
                      (deleted.Count > 0 ? $" ({string.Join(", ", deleted)})." : "."));
            return config;
        }

        private static SeedContent ReadSeed()
        {
            if (!File.Exists(SeedPath))
                throw new InvalidOperationException($"{LogPrefix} Seed file not found at {SeedPath}.");

            var seed = JsonUtility.FromJson<SeedContent>(File.ReadAllText(SeedPath, Encoding.UTF8));
            if (seed?.board == null || seed.scoreTable == null || seed.orders == null || seed.chains == null || seed.chains.Length == 0)
                throw new InvalidOperationException($"{LogPrefix} {SeedPath} needs 'board', 'scoreTable', 'orders' and a non-empty 'chains' section.");

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var chain in seed.chains)
            {
                if (chain?.id == null || !ChainIdPattern.IsMatch(chain.id))
                    throw new InvalidOperationException($"{LogPrefix} Chain id '{chain?.id}' must use only lowercase letters, digits and underscores.");
                if (!ids.Add(chain.id))
                    throw new InvalidOperationException($"{LogPrefix} Chain id '{chain.id}' appears more than once.");
                if (chain.levels == null || chain.levels.Length == 0)
                    throw new InvalidOperationException($"{LogPrefix} Chain '{chain.id}' has no levels.");
            }

            var layout = seed.initialLayout ?? Array.Empty<SeedPlacement>();
            for (var i = 0; i < layout.Length; i++)
            {
                if (layout[i] == null || !ids.Contains(layout[i].chain ?? string.Empty))
                    throw new InvalidOperationException($"{LogPrefix} initialLayout[{i}] names chain '{layout[i]?.chain}', which is not in 'chains'.");
            }

            seed.initialLayout = layout;
            return seed;
        }

        private static List<Sprite> LoadChainIcons(SeedChain chain, List<string> missing)
        {
            var icons = new List<Sprite>(chain.levels.Length);
            for (var level = 1; level <= chain.levels.Length; level++)
                icons.Add(LoadSprite($"{ItemSpritesFolder}/{chain.id}/{chain.id}_{level:D2}.png", missing));
            return icons;
        }

        private static List<Sprite> LoadCustomerAvatars(SeedOrders orders, List<string> missing)
        {
            var avatars = new List<Sprite>(Math.Max(orders.customerCount, 0));
            for (var customer = 1; customer <= orders.customerCount; customer++)
                avatars.Add(LoadSprite($"{IconSpritesFolder}/customer_{customer:D2}.png", missing));
            return avatars;
        }

        private static Sprite LoadSprite(string path, List<string> missing)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                missing.Add(File.Exists(path) ? $"{path} (exists but does not import as a Sprite)" : path);
            return sprite;
        }

        private static ItemChainAsset WriteChain(SeedChain seed, IReadOnlyList<Sprite> icons)
        {
            var chain = LoadOrCreate<ItemChainAsset>($"{ChainsFolder}/{seed.id}.asset");
            chain.Id = seed.id;
            chain.DisplayName = seed.displayName;
            chain.SpawnWeight = seed.spawnWeight;
            chain.ScoreMultiplier = seed.scoreMultiplier;
            chain.Levels = seed.levels.Select((level, index) => new ItemLevelEntry(level.name, icons[index])).ToList();
            EditorUtility.SetDirty(chain);
            return chain;
        }

        private static ScoreTableAsset WriteScoreTable(int[] pointsByLevel)
        {
            var scoreTable = LoadOrCreate<ScoreTableAsset>(ScoreTablePath);
            scoreTable.PointsByLevel = pointsByLevel;
            EditorUtility.SetDirty(scoreTable);
            return scoreTable;
        }

        private static OrderSettingsAsset WriteOrderSettings(SeedOrders seed, IReadOnlyList<Sprite> avatars)
        {
            var orders = LoadOrCreate<OrderSettingsAsset>(OrderSettingsPath);
            orders.MaxActiveOrders = seed.maxActiveOrders;
            orders.MinItemsPerOrder = seed.minItemsPerOrder;
            orders.MaxItemsPerOrder = seed.maxItemsPerOrder;
            orders.MinRequestedLevel = seed.minRequestedLevel;
            orders.MaxRequestedLevel = seed.maxRequestedLevel;
            orders.RewardPointsByLevel = seed.rewardPointsByLevel ?? Array.Empty<int>();
            orders.CustomerAvatars = avatars;
            EditorUtility.SetDirty(orders);
            return orders;
        }

        private static BoardLayoutAsset WriteBoardLayout(SeedPlacement[] seed, IReadOnlyList<ItemChainAsset> chains)
        {
            var layout = LoadOrCreate<BoardLayoutAsset>(BoardLayoutPath);
            layout.Placements = seed
                .Select(placement => new InitialPlacementEntry(
                    placement.x, placement.y, chains.First(chain => chain.Id == placement.chain), placement.level, placement.locked))
                .ToList();
            EditorUtility.SetDirty(layout);
            return layout;
        }

        private static List<string> DeleteStaleChains(IReadOnlyList<ItemChainAsset> current)
        {
            var keep = new HashSet<string>(current.Select(chain => AssetDatabase.GetAssetPath(chain)));
            var deleted = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(ItemChainAsset)}", new[] { ChainsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (keep.Contains(path))
                    continue;
                if (!AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException($"{LogPrefix} Could not delete the stale chain asset {path}.");
                deleted.Add(path);
            }

            return deleted;
        }

        /// <summary>Loads the asset at <paramref name="path"/> so it keeps its GUID, or creates it the first time.</summary>
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException($"{LogPrefix} {path} exists but is not a {typeof(T).Name}; move or delete it and seed again.");

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
