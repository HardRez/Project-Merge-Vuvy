using System;
using System.Collections.Generic;
using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Orders;
using MergeGame.Core.Rules;
using MergeGame.Core.Scoring;
using MergeGame.Core.Spawning;

namespace MergeGame.Core.Session
{
    /// <summary>
    /// The single entry point for presentation code: owns the board, score and orders, executes player commands and
    /// reports every resulting change through <see cref="EventRaised"/>. Events are raised after the state is fully
    /// updated. Construction raises no events; read the starting state from <see cref="Board"/> and <see cref="ActiveOrders"/>.
    /// </summary>
    public sealed class MergeGameSession
    {
        private readonly Board board;
        private readonly DropResolver dropResolver;
        private readonly ISpawnPolicy spawnPolicy;
        private readonly IScoreCalculator scoreCalculator;
        private readonly IRandom random;
        private readonly OrderBook orderBook;

        public MergeGameSession(GameDefinition definition, IRandom random)
            : this(definition, random, CreateInitialBoard(RequireDefinition(definition)))
        {
        }

        /// <param name="board">Used as is; the definition's initial placements are not applied to it.</param>
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
            if (definition.HasOrders)
                orderBook = new OrderBook(definition.OrderSettings, definition.OrderGenerator, Catalog, random);
        }

        public event Action<GameEvent> EventRaised;

        public IReadOnlyBoard Board => board;
        public ItemCatalog Catalog { get; }
        public int Score { get; private set; }
        public bool CanSpawn => !board.IsFull;

        /// <summary>
        /// One entry per order slot, always full when the game has orders, empty otherwise. A delivered order's
        /// replacement takes its slot index. This is a live view of the slots.
        /// </summary>
        public IReadOnlyList<Order> ActiveOrders => orderBook?.ActiveOrders ?? Array.Empty<Order>();

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
            if (!board.IsInside(placement.Position) || !board.IsEmpty(placement.Position))
                throw new InvalidOperationException($"Spawn policy chose cell {placement.Position}, which is not an empty cell of the board.");

            board.Place(placement.Position, new BoardItem(placement.ItemId));

            Raise(new ItemSpawned(placement.Position, placement.ItemId));
            if (board.IsFull)
                Raise(new BoardFullChanged(true));
            return SpawnResult.Success(placement);
        }

        public bool CanDiscard(GridPosition position) => CheckDiscard(position) == DiscardFailureReason.None;

        /// <summary>Removes an unlocked item without reward, so a full board without merges never leaves the player stuck.</summary>
        public DiscardResult Discard(GridPosition position)
        {
            var failure = CheckDiscard(position);
            if (failure != DiscardFailureReason.None)
                return DiscardResult.Failure(failure);

            var wasFull = board.IsFull;
            var itemId = board.Remove(position).ItemId;

            Raise(new ItemDiscarded(position, itemId));
            if (wasFull)
                Raise(new BoardFullChanged(false));
            return DiscardResult.Success(itemId);
        }

        /// <summary>True when the order is active and the board holds enough distinct unlocked items for every requirement.</summary>
        public bool CanDeliver(int orderId) =>
            orderBook != null && orderBook.TryGetSlot(orderId, out var slot) && orderBook.CanFulfil(orderBook.ActiveOrders[slot], board);

        /// <summary>
        /// Entry i is true when a distinct unlocked board item is available for <c>Requirements[i]</c> of the order, using
        /// the same matching as <see cref="DeliverOrder"/>; <see cref="CanDeliver"/> is true exactly when every entry is.
        /// Side-effect free; returns a fresh list. Empty for an id that is not active (and always in a game without
        /// orders), mirroring <see cref="CanDeliver"/> returning false rather than throwing.
        /// </summary>
        public IReadOnlyList<bool> GetRequirementFulfilment(int orderId)
        {
            if (orderBook == null || !orderBook.TryGetSlot(orderId, out var slot))
                return Array.Empty<bool>();
            return Array.AsReadOnly(orderBook.GetFulfilment(orderBook.ActiveOrders[slot], board));
        }

        /// <summary>
        /// Hands the first matching unlocked items (row-major) to the customer and replaces the order in its slot.
        /// Raises <see cref="OrderDelivered"/>, <see cref="ScoreChanged"/> (if points were awarded),
        /// <see cref="BoardFullChanged"/> (if the board was full) and <see cref="OrderAdded"/>, in that order.
        /// A failed delivery changes nothing and raises nothing.
        /// </summary>
        public DeliveryResult DeliverOrder(int orderId)
        {
            if (orderBook == null || !orderBook.TryGetSlot(orderId, out var slot))
                return DeliveryResult.Failure(DeliveryFailureReason.UnknownOrder);

            var order = orderBook.ActiveOrders[slot];
            var consumed = new List<ConsumedItem>(order.Requirements.Count);
            if (!orderBook.TryMatch(order, board, consumed))
                return DeliveryResult.Failure(DeliveryFailureReason.MissingItems);

            var replacement = orderBook.CreateOrderFor(slot);
            var wasFull = board.IsFull;
            foreach (var item in consumed)
                board.Remove(item.Position);
            orderBook.Replace(slot, replacement);
            var previousScore = Score;
            Score += order.RewardPoints;

            Raise(new OrderDelivered(order, slot, consumed.AsReadOnly(), order.RewardPoints));
            if (order.RewardPoints != 0)
                Raise(new ScoreChanged(previousScore, Score));
            if (wasFull)
                Raise(new BoardFullChanged(false));
            Raise(new OrderAdded(replacement, slot));
            return DeliveryResult.Success(order);
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
            var targetWasLocked = board.GetItem(outcome.To).IsLocked;
            var consumedItemId = board.Remove(outcome.From).ItemId;
            board.Replace(outcome.To, new BoardItem(outcome.ResultItemId));

            var points = scoreCalculator.PointsForMerge(outcome.ResultItemId);
            var previousScore = Score;
            Score += points;

            Raise(new ItemsMerged(outcome.From, outcome.To, consumedItemId, outcome.ResultItemId, points, targetWasLocked));
            if (points != 0)
                Raise(new ScoreChanged(previousScore, Score));
            if (wasFull)
                Raise(new BoardFullChanged(false));
        }

        private DiscardFailureReason CheckDiscard(GridPosition position)
        {
            if (!board.IsInside(position))
                return DiscardFailureReason.OutOfBounds;
            if (!board.TryGetItem(position, out var item))
                return DiscardFailureReason.EmptyCell;
            return item.IsLocked ? DiscardFailureReason.ItemLocked : DiscardFailureReason.None;
        }

        private void Raise(GameEvent gameEvent) => EventRaised?.Invoke(gameEvent);

        private static Board CreateInitialBoard(GameDefinition definition)
        {
            var board = new Board(definition.BoardSize);
            foreach (var placement in definition.InitialPlacements)
                board.Place(placement.Position, new BoardItem(placement.ItemId, placement.IsLocked));
            return board;
        }

        private static GameDefinition RequireDefinition(GameDefinition definition) =>
            definition ?? throw new ArgumentNullException(nameof(definition));
    }
}
