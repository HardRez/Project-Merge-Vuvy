using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Session;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class DiscardTests
    {
        private static readonly GridPosition Cell = new GridPosition(2, 1);

        [Test]
        public void Discard_RemovesTheItemWithoutChangingTheScore()
        {
            var session = new SessionBuilder().WithItem(2, 1, Item(Stone, 4)).Build();

            var result = session.Discard(Cell);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.DiscardedItemId, Is.EqualTo(Item(Stone, 4)));
            Assert.That(session.Board.IsEmpty(Cell), Is.True);
            Assert.That(session.Score, Is.Zero);
        }

        [Test]
        public void Discard_RaisesOnlyItemDiscarded()
        {
            var session = new SessionBuilder().WithItem(2, 1, Item(Stone, 4)).Build();
            var recorder = new EventRecorder(session);

            session.Discard(Cell);

            var discarded = recorder.Single<ItemDiscarded>();
            Assert.That(discarded.Position, Is.EqualTo(Cell));
            Assert.That(discarded.ItemId, Is.EqualTo(Item(Stone, 4)));
            Assert.That(recorder.Events.Count, Is.EqualTo(1));
        }

        [Test]
        public void Discard_OnFullBoard_RaisesItemDiscardedThenBoardFullChanged()
        {
            var session = new SessionBuilder().WithFullBoardOf(Item(Wood, ChainLength)).Build();
            var recorder = new EventRecorder(session);

            session.Discard(Cell);

            Assert.That(recorder.Types, Is.EqualTo(new[] { typeof(ItemDiscarded), typeof(BoardFullChanged) }));
            Assert.That(((BoardFullChanged)recorder.Events[1]).IsFull, Is.False);
            Assert.That(session.CanSpawn, Is.True);
        }

        [TestCase(5, 1, DiscardFailureReason.OutOfBounds)]
        [TestCase(0, 0, DiscardFailureReason.EmptyCell)]
        [TestCase(3, 3, DiscardFailureReason.ItemLocked)]
        public void Discard_WhenNotPossible_FailsWithReasonAndChangesNothing(int x, int y, DiscardFailureReason expected)
        {
            var session = new SessionBuilder().WithItem(2, 1, Item(Wood, 1)).WithLockedItem(3, 3, Item(Wood, 1)).Build();
            var recorder = new EventRecorder(session);

            var result = session.Discard(new GridPosition(x, y));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(expected));
            Assert.That(recorder.Events, Is.Empty);
            Assert.That(session.Board.EmptyCellCount, Is.EqualTo(23));
            Assert.That(session.Board.GetItem(new GridPosition(3, 3)).IsLocked, Is.True);
        }

        [Test]
        public void DiscardedItemId_OfAFailedDiscard_Throws()
        {
            var session = new SessionBuilder().Build();

            Assert.Throws<System.InvalidOperationException>(() => _ = session.Discard(Cell).DiscardedItemId);
        }

        [TestCase(2, 1, true)]
        [TestCase(5, 1, false)]
        [TestCase(0, 0, false)]
        [TestCase(3, 3, false)]
        public void CanDiscard_AgreesWithDiscardAndHasNoSideEffects(int x, int y, bool expected)
        {
            var session = new SessionBuilder().WithItem(2, 1, Item(Wood, 1)).WithLockedItem(3, 3, Item(Wood, 1)).Build();
            var recorder = new EventRecorder(session);
            var position = new GridPosition(x, y);

            var canDiscard = session.CanDiscard(position);

            Assert.That(canDiscard, Is.EqualTo(expected));
            Assert.That(recorder.Events, Is.Empty);
            Assert.That(session.Board.EmptyCellCount, Is.EqualTo(23));
            Assert.That(session.Discard(position).Succeeded, Is.EqualTo(canDiscard));
        }
    }
}
