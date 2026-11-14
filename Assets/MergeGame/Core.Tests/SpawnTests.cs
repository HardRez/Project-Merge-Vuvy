using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Scoring;
using MergeGame.Core.Session;
using MergeGame.Core.Spawning;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class SpawnTests
    {
        [Test]
        public void Spawn_PlacesALevelOneItemInAnEmptyCell()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Wood, 3)).Build();

            var result = session.Spawn();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Placement.ItemId.Level, Is.EqualTo(1));
            Assert.That(result.Placement.Position, Is.Not.EqualTo(new GridPosition(0, 0)));
            Assert.That(session.Board.GetItem(result.Placement.Position).ItemId, Is.EqualTo(result.Placement.ItemId));
            Assert.That(session.Board.EmptyCellCount, Is.EqualTo(23));
        }

        [Test]
        public void Spawn_RaisesItemSpawned()
        {
            var session = new SessionBuilder().Build();
            var recorder = new EventRecorder(session);

            var result = session.Spawn();

            var spawned = recorder.Single<ItemSpawned>();
            Assert.That(spawned.Position, Is.EqualTo(result.Placement.Position));
            Assert.That(spawned.ItemId, Is.EqualTo(result.Placement.ItemId));
        }

        [Test]
        public void Spawn_OnFullBoard_FailsWithBoardFull()
        {
            var session = new SessionBuilder().WithFullBoardOf(Item(Stone, 2)).Build();
            var recorder = new EventRecorder(session);

            var result = session.Spawn();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(SpawnFailureReason.BoardFull));
            Assert.That(recorder.Events, Is.Empty);
        }

        [Test]
        public void CanSpawn_OnFullBoard_IsFalse()
        {
            var session = new SessionBuilder().WithFullBoardOf(Item(Stone, 2)).Build();

            Assert.That(session.CanSpawn, Is.False);
        }

        [Test]
        public void CanSpawn_WithAnEmptyCell_IsTrue()
        {
            var session = new SessionBuilder().Build();

            Assert.That(session.CanSpawn, Is.True);
        }

        [Test]
        public void Spawn_FillingTheLastCell_RaisesBoardFullChangedAfterItemSpawned()
        {
            var session = new MergeGameSession(Definition(1, 1), new ScriptedRandom());
            var recorder = new EventRecorder(session);

            session.Spawn();

            Assert.That(recorder.Events.Count, Is.EqualTo(2));
            Assert.That(recorder.Events[0], Is.InstanceOf<ItemSpawned>());
            Assert.That(((BoardFullChanged)recorder.Events[1]).IsFull, Is.True);
            Assert.That(session.CanSpawn, Is.False);
        }

        [Test]
        public void Spawn_NeverChoosesAChainWithZeroSpawnWeight()
        {
            var catalog = Catalog(Chain(Wood, spawnWeight: 0), Chain(Stone, spawnWeight: 1));
            var session = new MergeGameSession(Definition(catalog: catalog), new SystemRandom(42));

            for (var i = 0; i < 25; i++)
                Assert.That(session.Spawn().Placement.ItemId.ChainId, Is.EqualTo(Stone));
        }

        [Test]
        public void Spawn_WhenThePolicyChoosesAnOccupiedCell_ThrowsWithoutChangingTheBoard()
        {
            var definition = new GameDefinition(
                DefaultCatalog(), new BoardSize(5, 5), new ScoreTable(DefaultPoints), spawnPolicy: new FixedCellPolicy(new GridPosition(0, 0)));
            var session = new SessionBuilder().WithDefinition(definition).WithLockedItem(0, 0, Item(Stone, 2)).Build();

            var exception = Assert.Throws<System.InvalidOperationException>(() => session.Spawn());
            Assert.That(exception.Message, Does.Contain("(0, 0)"));
            Assert.That(session.Board.GetItem(new GridPosition(0, 0)).ItemId, Is.EqualTo(Item(Stone, 2)));
        }

        [Test]
        public void Spawn_UntilFull_FillsEveryCellExactlyOnce()
        {
            var session = new MergeGameSession(Definition(3, 7), new SystemRandom(7));

            for (var i = 0; i < 21; i++)
                Assert.That(session.Spawn().Succeeded, Is.True);

            Assert.That(session.Board.IsFull, Is.True);
            Assert.That(session.Spawn().FailureReason, Is.EqualTo(SpawnFailureReason.BoardFull));
        }

        private sealed class FixedCellPolicy : ISpawnPolicy
        {
            private readonly GridPosition cell;

            public FixedCellPolicy(GridPosition cell)
            {
                this.cell = cell;
            }

            public SpawnPlacement Choose(IReadOnlyBoard board, IRandom random) => new SpawnPlacement(Item(Wood, 1), cell);
        }
    }
}
