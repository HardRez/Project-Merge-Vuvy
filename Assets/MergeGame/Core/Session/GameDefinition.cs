using System;
using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Orders;
using MergeGame.Core.Rules;
using MergeGame.Core.Scoring;
using MergeGame.Core.Spawning;

namespace MergeGame.Core.Session
{
    /// <summary>Everything that makes one game configuration: content, board size, scoring data, rules and strategies.</summary>
    public sealed class GameDefinition
    {
        /// <param name="dropRules">Ordered rule pipeline; defaults to <see cref="StandardDropRules.Create"/>.</param>
        /// <param name="spawnPolicy">Defaults to <see cref="WeightedRandomSpawnPolicy"/>.</param>
        /// <param name="scoreCalculator">Defaults to <see cref="ScoreTableCalculator"/> over <paramref name="scoreTable"/>.</param>
        /// <param name="initialPlacements">Items on the board when a session starts; none by default.</param>
        /// <param name="orderSettings">Null means the game has no customer orders.</param>
        /// <param name="orderGenerator">Defaults to <see cref="RandomOrderGenerator"/>; requires <paramref name="orderSettings"/>.</param>
        public GameDefinition(
            ItemCatalog catalog,
            BoardSize boardSize,
            ScoreTable scoreTable,
            IReadOnlyList<IDropRule> dropRules = null,
            ISpawnPolicy spawnPolicy = null,
            IScoreCalculator scoreCalculator = null,
            IReadOnlyList<InitialPlacement> initialPlacements = null,
            OrderSettings orderSettings = null,
            IOrderGenerator orderGenerator = null)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            ScoreTable = scoreTable ?? throw new ArgumentNullException(nameof(scoreTable));
            if (boardSize.CellCount == 0)
                throw new ArgumentException("Board size must be set.", nameof(boardSize));
            scoreTable.EnsureCovers(catalog);
            if (orderGenerator != null && orderSettings == null)
                throw new ArgumentException("An order generator needs order settings; without them the game has no orders.", nameof(orderGenerator));
            orderSettings?.EnsureFits(catalog);

            BoardSize = boardSize;
            DropRules = CopyRules(dropRules ?? StandardDropRules.Create());
            SpawnPolicy = spawnPolicy ?? new WeightedRandomSpawnPolicy(catalog);
            ScoreCalculator = scoreCalculator ?? new ScoreTableCalculator(scoreTable, catalog);
            InitialPlacements = CopyPlacements(initialPlacements ?? Array.Empty<InitialPlacement>(), boardSize, catalog);
            OrderSettings = orderSettings;
            OrderGenerator = orderSettings == null ? null : orderGenerator ?? new RandomOrderGenerator(orderSettings, catalog);
        }

        public ItemCatalog Catalog { get; }
        public BoardSize BoardSize { get; }
        public ScoreTable ScoreTable { get; }
        public IReadOnlyList<IDropRule> DropRules { get; }
        public ISpawnPolicy SpawnPolicy { get; }
        public IScoreCalculator ScoreCalculator { get; }
        public IReadOnlyList<InitialPlacement> InitialPlacements { get; }

        /// <summary>Null when the game has no orders.</summary>
        public OrderSettings OrderSettings { get; }

        /// <summary>Null when the game has no orders.</summary>
        public IOrderGenerator OrderGenerator { get; }

        public bool HasOrders => OrderSettings != null;

        private static IDropRule[] CopyRules(IReadOnlyList<IDropRule> rules)
        {
            if (rules.Count == 0)
                throw new ArgumentException("At least one drop rule is required.", nameof(rules));

            var copy = new IDropRule[rules.Count];
            for (var i = 0; i < rules.Count; i++)
                copy[i] = rules[i] ?? throw new ArgumentException($"Drop rule at index {i} is null.", nameof(rules));
            return copy;
        }

        private static InitialPlacement[] CopyPlacements(IReadOnlyList<InitialPlacement> placements, BoardSize boardSize, ItemCatalog catalog)
        {
            var copy = new InitialPlacement[placements.Count];
            var occupied = new Dictionary<GridPosition, int>();
            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                if (!boardSize.Contains(placement.Position))
                    throw new ArgumentException(
                        $"Initial placement {i} ({placement}) is outside the {boardSize} board.", "initialPlacements");
                if (occupied.TryGetValue(placement.Position, out var earlier))
                    throw new ArgumentException(
                        $"Initial placements {earlier} and {i} both use cell {placement.Position}.", "initialPlacements");
                if (!catalog.Contains(placement.ItemId))
                    throw new ArgumentException(
                        $"Initial placement {i} ({placement}) names an item that is not in the item catalog.", "initialPlacements");

                occupied.Add(placement.Position, i);
                copy[i] = placement;
            }

            return copy;
        }
    }
}
