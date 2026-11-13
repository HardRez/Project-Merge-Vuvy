using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Scoring;
using MergeGame.Core.Session;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class ScoreTests
    {
        private static readonly GridPosition Source = new GridPosition(0, 0);
        private static readonly GridPosition Target = new GridPosition(1, 0);

        [TestCase(1, 10)]
        [TestCase(2, 20)]
        [TestCase(3, 40)]
        [TestCase(4, 80)]
        public void Merge_AwardsTheTablePointsForTheCreatedLevel(int mergedLevel, int expectedPoints)
        {
            var session = MergeReadySession(Item(Wood, mergedLevel));

            session.Drop(Source, Target);

            Assert.That(session.Score, Is.EqualTo(expectedPoints));
        }

        [Test]
        public void Merge_OfAHigherLevel_AwardsMorePoints()
        {
            var low = MergeReadySession(Item(Wood, 1));
            var high = MergeReadySession(Item(Wood, 3));

            low.Drop(Source, Target);
            high.Drop(Source, Target);

            Assert.That(high.Score, Is.GreaterThan(low.Score));
        }

        [Test]
        public void Merge_PointsComeFromTheScoreTableData()
        {
            var table = new ScoreTable(new[] { 0, 7, 9, 11, 13 });
            var session = MergeReadySession(Item(Wood, 1), Definition(scoreTable: table));

            session.Drop(Source, Target);

            Assert.That(session.Score, Is.EqualTo(7));
        }

        [Test]
        public void Merge_AppliesTheChainScoreMultiplier()
        {
            var catalog = Catalog(Chain(Wood, scoreMultiplier: 2.5f));
            var session = MergeReadySession(Item(Wood, 2), Definition(catalog: catalog));

            session.Drop(Source, Target);

            Assert.That(session.Score, Is.EqualTo(50));
        }

        [Test]
        public void ScoreTableCalculator_RoundsHalfPointsAwayFromZero()
        {
            var catalog = Catalog(Chain(Wood, scoreMultiplier: 1.5f));
            var calculator = new ScoreTableCalculator(new ScoreTable(new[] { 0, 5, 10, 20, 40 }), catalog);

            Assert.That(calculator.PointsForMerge(Item(Wood, 2)), Is.EqualTo(8));
        }

        [Test]
        public void Merges_AccumulateIntoTheScore()
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1))
                .WithItem(0, 1, Item(Stone, 2)).WithItem(1, 1, Item(Stone, 2))
                .Build();

            session.Drop(Source, Target);
            session.Drop(new GridPosition(0, 1), new GridPosition(1, 1));

            Assert.That(session.Score, Is.EqualTo(10 + 20));
        }

        [Test]
        public void Merge_RaisesScoreChangedWithPreviousAndNewScore()
        {
            var session = MergeReadySession(Item(Wood, 2));
            var recorder = new EventRecorder(session);

            session.Drop(Source, Target);

            var changed = recorder.Single<ScoreChanged>();
            Assert.That(changed.PreviousScore, Is.Zero);
            Assert.That(changed.Score, Is.EqualTo(20));
            Assert.That(changed.Delta, Is.EqualTo(20));
        }

        private static MergeGameSession MergeReadySession(ItemId item, GameDefinition definition = null) =>
            new SessionBuilder()
                .WithDefinition(definition ?? Definition())
                .WithItem(0, 0, item)
                .WithItem(1, 0, item)
                .Build();
    }
}
