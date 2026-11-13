using System;
using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Rules;
using MergeGame.Core.Scoring;
using MergeGame.Core.Spawning;

namespace MergeGame.Core.Session
{
    /// <summary>
    /// The single entry point for presentation code: owns the board and score, executes player commands and reports
    /// every resulting change through <see cref="EventRaised"/>. Events are raised after the state is fully updated.
    /// </summary>
    public sealed class MergeGameSession
    {
        private readonly Board board;
        private readonly DropResolver dropResolver;
        private readonly ISpawnPolicy spawnPolicy;
        private readonly IScoreCalculator scoreCalculator;
        private readonly IRandom random;

        public MergeGameSession(GameDefinition definition, IRandom random)
            : this(definition, random, new Board(RequireDefinition(definition).BoardSize))
        {
        }

        internal MergeGameSession(GameDefinition definition, IRandom random, Board board)
        {
            RequireDefinition(definition);
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            if (board.Size != definition.BoardSize)
                throw new ArgumentException($"Board is {board.Size} but the definition requires {definition.BoardSize}.", nameof(board));

            Catalog = definition.Catalog;
            dropResolver = new DropResolver(definition.DropRules);
            spawnPolicy = definition.SpawnPolicy;
            scoreCalculator = definition.ScoreCalculator;
        }

        public event Action<GameEvent> EventRaised;

        public IReadOnlyBoard Board => board;
        public ItemCatalog Catalog { get; }
        public int Score { get; private set; }
        public bool CanSpawn => !board.IsFull;

        public DropOutcome PreviewDrop(GridPosition from, GridPosition to) => dropResolver.Resolve(board, Catalog, from, to);

        public DropOutcome Drop(GridPosition from, GridPosition to)
        {
            var outcome = PreviewDrop(from, to);
            switch (outcome.Kind)
            {
                case DropOutcomeKind.Move:
                    ApplyMove(outcome);
                    break;
                case DropOutcomeKind.Merge:
                    ApplyMerge(outcome);
                    break;
                default:
                    Raise(new DropRejected(from, to, outcome.RejectReason));
                    break;
            }

            return outcome;
        }

        public SpawnResult Spawn()
        {
            if (board.IsFull)
                return SpawnResult.Failure(SpawnFailureReason.BoardFull);

            var placement = spawnPolicy.Choose(board, random);
            if (!Catalog.Contains(placement.ItemId))
                throw new InvalidOperationException($"Spawn policy chose {placement.ItemId}, which is not in the item catalog.");

            board.Place(placement.Position, new BoardItem(placement.ItemId));

            Raise(new ItemSpawned(placement.Position, placement.ItemId));
            if (board.IsFull)
                Raise(new BoardFullChanged(true));
            return SpawnResult.Success(placement);
        }

        private void ApplyMove(DropOutcome outcome)
        {
            var itemId = board.GetItem(outcome.From).ItemId;
            board.Move(outcome.From, outcome.To);
            Raise(new ItemMoved(outcome.From, outcome.To, itemId));
        }

        private void ApplyMerge(DropOutcome outcome)
        {
            var wasFull = board.IsFull;
            var consumedItemId = board.Remove(outcome.From).ItemId;
            board.Replace(outcome.To, new BoardItem(outcome.ResultItemId));

            var points = scoreCalculator.PointsForMerge(outcome.ResultItemId);
            var previousScore = Score;
            Score += points;

            Raise(new ItemsMerged(outcome.From, outcome.To, consumedItemId, outcome.ResultItemId, points));
            if (points != 0)
                Raise(new ScoreChanged(previousScore, Score));
            if (wasFull)
                Raise(new BoardFullChanged(false));
        }

        private void Raise(GameEvent gameEvent) => EventRaised?.Invoke(gameEvent);

        private static GameDefinition RequireDefinition(GameDefinition definition) =>
            definition ?? throw new ArgumentNullException(nameof(definition));
    }
}
