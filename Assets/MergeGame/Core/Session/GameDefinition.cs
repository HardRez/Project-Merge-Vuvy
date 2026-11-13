using System;
using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
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
        public GameDefinition(
            ItemCatalog catalog,
            BoardSize boardSize,
            ScoreTable scoreTable,
            IReadOnlyList<IDropRule> dropRules = null,
            ISpawnPolicy spawnPolicy = null,
            IScoreCalculator scoreCalculator = null)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            ScoreTable = scoreTable ?? throw new ArgumentNullException(nameof(scoreTable));
            if (boardSize.CellCount == 0)
                throw new ArgumentException("Board size must be set.", nameof(boardSize));
            scoreTable.EnsureCovers(catalog);

            BoardSize = boardSize;
            DropRules = CopyRules(dropRules ?? StandardDropRules.Create());
            SpawnPolicy = spawnPolicy ?? new WeightedRandomSpawnPolicy(catalog);
            ScoreCalculator = scoreCalculator ?? new ScoreTableCalculator(scoreTable, catalog);
        }

        public ItemCatalog Catalog { get; }
        public BoardSize BoardSize { get; }
        public ScoreTable ScoreTable { get; }
        public IReadOnlyList<IDropRule> DropRules { get; }
        public ISpawnPolicy SpawnPolicy { get; }
        public IScoreCalculator ScoreCalculator { get; }

        private static IDropRule[] CopyRules(IReadOnlyList<IDropRule> rules)
        {
            if (rules.Count == 0)
                throw new ArgumentException("At least one drop rule is required.", nameof(rules));

            var copy = new IDropRule[rules.Count];
            for (var i = 0; i < rules.Count; i++)
                copy[i] = rules[i] ?? throw new ArgumentException($"Drop rule at index {i} is null.", nameof(rules));
            return copy;
        }
    }
}
