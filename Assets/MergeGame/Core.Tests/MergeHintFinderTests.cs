using MergeGame.Core.Grid;
using MergeGame.Core.Hints;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class MergeHintFinderTests
    {
        [Test]
        public void EmptyBoard_HasNoHint()
        {
            var session = new SessionBuilder().Build();

            Assert.That(new MergeHintFinder(session).TryFind(out _), Is.False);
        }

        [Test]
        public void BoardWithoutEqualItems_HasNoHint()
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Stone, 1))
                .WithItem(2, 0, Item(Wood, 2))
                .Build();

            Assert.That(new MergeHintFinder(session).TryFind(out _), Is.False);
        }

        [Test]
        public void TwoEqualItems_AreSuggested_WithTheMergeResult()
        {
            var session = new SessionBuilder().WithItem(3, 0, Item(Wood, 2)).WithItem(1, 2, Item(Wood, 2)).Build();

            var found = new MergeHintFinder(session).TryFind(out var hint);

            Assert.That(found, Is.True);
            Assert.That(hint.From, Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(hint.To, Is.EqualTo(new GridPosition(1, 2)));
            Assert.That(hint.ResultItemId, Is.EqualTo(Item(Wood, 3)));
        }

        [Test]
        public void MergeOntoALockedTarget_IsPreferredOverAnEarlierFreeMerge()
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithItem(2, 0, Item(Stone, 2))
                .WithLockedItem(4, 4, Item(Stone, 2))
                .Build();

            new MergeHintFinder(session).TryFind(out var hint);

            Assert.That(hint.From, Is.EqualTo(new GridPosition(2, 0)));
            Assert.That(hint.To, Is.EqualTo(new GridPosition(4, 4)));
            Assert.That(hint.ResultItemId, Is.EqualTo(Item(Stone, 3)));
        }

        [Test]
        public void LockedItem_IsNeverTheSource()
        {
            var session = new SessionBuilder().WithLockedItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).Build();

            new MergeHintFinder(session).TryFind(out var hint);

            Assert.That(hint.From, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(hint.To, Is.EqualTo(new GridPosition(0, 0)));
        }

        [Test]
        public void TwoLockedEqualItems_HaveNoHint()
        {
            var session = new SessionBuilder().WithLockedItem(0, 0, Item(Wood, 1)).WithLockedItem(1, 0, Item(Wood, 1)).Build();

            Assert.That(new MergeHintFinder(session).TryFind(out _), Is.False);
        }

        [Test]
        public void MaxLevelPair_IsNotSuggested()
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(Wood, ChainLength))
                .WithItem(1, 0, Item(Wood, ChainLength))
                .Build();

            Assert.That(new MergeHintFinder(session).TryFind(out _), Is.False);
        }

        [Test]
        public void AmongFreeMerges_TheFirstSourceInRowMajorOrderWins_ThenTheFirstTarget()
        {
            var session = new SessionBuilder()
                .WithItem(0, 1, Item(Wood, 1))
                .WithItem(4, 0, Item(Stone, 1))
                .WithItem(2, 2, Item(Stone, 1))
                .WithItem(1, 1, Item(Stone, 1))
                .WithItem(3, 3, Item(Wood, 1))
                .Build();

            new MergeHintFinder(session).TryFind(out var hint);

            Assert.That(hint.From, Is.EqualTo(new GridPosition(4, 0)));
            Assert.That(hint.To, Is.EqualTo(new GridPosition(1, 1)));
        }

        [Test]
        public void AmongLockedTargets_TheFirstPairInRowMajorOrderWins()
        {
            var session = new SessionBuilder()
                .WithLockedItem(3, 3, Item(Wood, 1))
                .WithLockedItem(2, 1, Item(Wood, 1))
                .WithItem(4, 2, Item(Wood, 1))
                .Build();

            new MergeHintFinder(session).TryFind(out var hint);

            Assert.That(hint.From, Is.EqualTo(new GridPosition(4, 2)));
            Assert.That(hint.To, Is.EqualTo(new GridPosition(2, 1)));
        }

        [Test]
        public void TryFind_LeavesTheSessionUntouched_AndRaisesNoEvents()
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithLockedItem(2, 0, Item(Stone, 1))
                .Build();
            var recorder = new EventRecorder(session);

            new MergeHintFinder(session).TryFind(out _);

            Assert.That(recorder.Events, Is.Empty);
            Assert.That(session.Score, Is.Zero);
            Assert.That(session.Board.EmptyCellCount, Is.EqualTo(22));
            Assert.That(session.Board.GetItem(new GridPosition(0, 0)).ItemId, Is.EqualTo(Item(Wood, 1)));
            Assert.That(session.Board.GetItem(new GridPosition(1, 0)).ItemId, Is.EqualTo(Item(Wood, 1)));
            Assert.That(session.Board.GetItem(new GridPosition(2, 0)).IsLocked, Is.True);
        }

        [Test]
        public void Constructor_RejectsAMissingSession()
        {
            Assert.That(() => new MergeHintFinder(null), Throws.ArgumentNullException);
        }
    }
}
