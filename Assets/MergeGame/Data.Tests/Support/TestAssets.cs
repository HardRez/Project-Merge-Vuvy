using System;
using System.Collections.Generic;
using System.Linq;
using MergeGame.Data.Authoring;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MergeGame.Data.Tests.Support
{
    /// <summary>Builds in-memory authoring assets with healthy defaults and destroys everything it made on dispose.</summary>
    internal sealed class TestAssets : IDisposable
    {
        public const string Wood = "wood";
        public const string Stone = "stone";
        public const int ChainLength = 5;

        public static readonly int[] DefaultPoints = { 0, 10, 20, 40, 80 };
        public static readonly int[] DefaultRewardPoints = { 5, 15, 30, 60, 120 };

        private readonly List<Object> created = new List<Object>();

        public T Track<T>(T asset) where T : Object
        {
            created.Add(asset);
            return asset;
        }

        public T Create<T>(string name) where T : ScriptableObject
        {
            var asset = Track(ScriptableObject.CreateInstance<T>());
            asset.name = name;
            return asset;
        }

        public Sprite Sprite(string name)
        {
            var texture = Track(new Texture2D(4, 4));
            var sprite = Track(UnityEngine.Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f)));
            sprite.name = name;
            return sprite;
        }

        public ItemChainAsset Chain(string id, int maxLevel = ChainLength, int spawnWeight = 1, float scoreMultiplier = 1f)
        {
            var chain = Create<ItemChainAsset>(id);
            chain.Id = id;
            chain.DisplayName = $"{id} chain";
            chain.SpawnWeight = spawnWeight;
            chain.ScoreMultiplier = scoreMultiplier;
            chain.Levels = Enumerable.Range(1, maxLevel).Select(level => new ItemLevelEntry($"{id} {level}", Sprite($"{id}_{level:D2}"))).ToList();
            return chain;
        }

        public ScoreTableAsset ScoreTable(params int[] pointsByLevel)
        {
            var scoreTable = Create<ScoreTableAsset>("TestScoreTable");
            scoreTable.PointsByLevel = pointsByLevel.Length > 0 ? pointsByLevel : DefaultPoints;
            return scoreTable;
        }

        public OrderSettingsAsset Orders(int customerCount = 3)
        {
            var orders = Create<OrderSettingsAsset>("TestOrders");
            orders.MaxActiveOrders = 2;
            orders.MinItemsPerOrder = 1;
            orders.MaxItemsPerOrder = 2;
            orders.MinRequestedLevel = 2;
            orders.MaxRequestedLevel = 3;
            orders.RewardPointsByLevel = DefaultRewardPoints;
            orders.CustomerAvatars = Enumerable.Range(0, customerCount).Select(index => Sprite($"customer_{index}")).ToList();
            return orders;
        }

        public BoardLayoutAsset Layout(params InitialPlacementEntry[] placements)
        {
            var layout = Create<BoardLayoutAsset>("TestLayout");
            layout.Placements = placements;
            return layout;
        }

        /// <summary>A healthy 5x5 config with two chains and a score table, but no orders or layout.</summary>
        public GameConfigAsset Config(params ItemChainAsset[] chains)
        {
            var config = Create<GameConfigAsset>("TestConfig");
            config.BoardWidth = 5;
            config.BoardHeight = 5;
            config.Chains = chains.Length > 0 ? chains : new[] { Chain(Wood), Chain(Stone) };
            config.ScoreTable = ScoreTable();
            return config;
        }

        /// <summary>A healthy config using every asset type.</summary>
        public GameConfigAsset FullConfig()
        {
            var config = Config();
            config.Orders = Orders();
            config.InitialLayout = Layout(
                new InitialPlacementEntry(0, 0, config.Chains[0], 2, locked: true),
                new InitialPlacementEntry(1, 0, config.Chains[1], 1, locked: false));
            return config;
        }

        public void Dispose()
        {
            foreach (var asset in created)
            {
                if (asset != null)
                    Object.DestroyImmediate(asset);
            }

            created.Clear();
        }
    }
}
