using System;
using System.Collections.Generic;
using System.Linq;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Orders;
using MergeGame.Core.Scoring;
using MergeGame.Core.Session;
using MergeGame.Data.Authoring;

namespace MergeGame.Data.Definitions
{
    /// <summary>
    /// Maps authoring assets to Core definitions. Core validates the content itself; this class only adds the asset
    /// context (which config, chain or layout) and rejects what Core cannot see, such as empty asset slots.
    /// </summary>
    public sealed class GameDefinitionFactory
    {
        /// <exception cref="GameConfigException">The config cannot describe a playable game; the message says why.</exception>
        public GameDefinition Create(GameConfigAsset config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            try
            {
                return new GameDefinition(
                    new ItemCatalog(CreateChains(config)),
                    new BoardSize(config.BoardWidth, config.BoardHeight),
                    CreateScoreTable(config),
                    initialPlacements: CreatePlacements(config),
                    orderSettings: CreateOrderSettings(config));
            }
            catch (ArgumentException exception)
            {
                throw Problem(config, exception.Message, exception);
            }
        }

        private static List<ItemChainDefinition> CreateChains(GameConfigAsset config)
        {
            var chains = new List<ItemChainDefinition>(config.Chains.Count);
            for (var i = 0; i < config.Chains.Count; i++)
            {
                var chain = config.Chains[i];
                if (chain == null)
                    throw Problem(config, $"chain slot {i} is empty.");
                chains.Add(CreateChain(config, chain));
            }

            return chains;
        }

        private static ItemChainDefinition CreateChain(GameConfigAsset config, ItemChainAsset chain)
        {
            var levels = new List<ItemLevelDefinition>(chain.Levels.Count);
            try
            {
                for (var i = 0; i < chain.Levels.Count; i++)
                {
                    var level = i + 1;
                    var entry = chain.Levels[i] ?? throw Problem(config, $"{AssetLabels.Chain(chain)} level {level} is empty.");
                    levels.Add(new ItemLevelDefinition(level, entry.DisplayName));
                }

                return new ItemChainDefinition(chain.Id, levels, chain.SpawnWeight, chain.ScoreMultiplier);
            }
            catch (ArgumentException exception)
            {
                throw Problem(config, $"{AssetLabels.Chain(chain)}: {exception.Message}", exception);
            }
        }

        private static ScoreTable CreateScoreTable(GameConfigAsset config)
        {
            if (config.ScoreTable == null)
                throw Problem(config, "no score table is assigned.");
            return new ScoreTable(config.ScoreTable.PointsByLevel);
        }

        private static OrderSettings CreateOrderSettings(GameConfigAsset config)
        {
            var orders = config.Orders;
            if (orders == null)
                return null;

            try
            {
                return new OrderSettings(
                    orders.MaxActiveOrders,
                    orders.MinItemsPerOrder,
                    orders.MaxItemsPerOrder,
                    orders.MinRequestedLevel,
                    orders.MaxRequestedLevel,
                    orders.CustomerCount,
                    new ScoreTable(orders.RewardPointsByLevel));
            }
            catch (ArgumentException exception)
            {
                throw Problem(config, $"order settings '{orders.name}': {exception.Message}", exception);
            }
        }

        private static List<InitialPlacement> CreatePlacements(GameConfigAsset config)
        {
            var layout = config.InitialLayout;
            if (layout == null)
                return null;

            var placements = new List<InitialPlacement>(layout.Placements.Count);
            for (var i = 0; i < layout.Placements.Count; i++)
            {
                var entry = layout.Placements[i];
                var label = $"initial layout '{layout.name}' placement {i}";
                if (entry == null)
                    throw Problem(config, $"{label} is empty.");
                if (entry.Chain == null)
                    throw Problem(config, $"{label} at {AssetLabels.Cell(entry)} has no chain.");
                if (!config.Chains.Contains(entry.Chain))
                    throw Problem(config, $"{label} at {AssetLabels.Cell(entry)} uses {AssetLabels.Chain(entry.Chain)}, which is not in the config's chain list.");

                try
                {
                    placements.Add(new InitialPlacement(new GridPosition(entry.X, entry.Y), new ItemId(entry.Chain.Id, entry.Level), entry.Locked));
                }
                catch (ArgumentException exception)
                {
                    throw Problem(config, $"{label} at {AssetLabels.Cell(entry)}: {exception.Message}", exception);
                }
            }

            return placements;
        }

        private static GameConfigException Problem(GameConfigAsset config, string message, Exception innerException = null) =>
            new GameConfigException($"Game config '{config.name}': {message}", innerException);
    }
}
