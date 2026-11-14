using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Rules;
using MergeGame.Core.Scoring;
using MergeGame.Core.Session;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class LockedItemTests
    {
        private static readonly GridPosition Source = new GridPosition(0, 0);
        private static readonly GridPosition Target = new GridPosition(1, 0);

        [Test]
        public void BoardItem_IsUnlockedByDefault()
        {
            Assert.That(new BoardItem(Item(Wood, 1)).IsLocked, Is.False);
        }

        [Test]
        public void Drop_FromLockedItem_OntoEmptyCell_IsRejectedAsSourceLocked()
        {
            var session = new SessionBuilder().WithLockedItem(0, 0, Item(Wood, 2)).Build();
            var recorder = new EventRecorder(session);

            var outcome = session.Drop(Source, Target);

            Assert.That(outcome.RejectReason, Is.EqualTo(DropRejectReason.SourceLocked));
            Assert.That(session.Board.GetItem(Source).IsLocked, Is.True);
            Assert.That(session.Board.IsEmpty(Target), Is.True);
            Assert.That(recorder.Single<DropRejected>().Reason, Is.EqualTo(DropRejectReason.SourceLocked));
            Assert.That(recorder.Events.Count, Is.EqualTo(1));
        }

        [Test]
        public void Drop_FromLockedItem_OntoMatchingItem_IsRejectedAsSourceLocked()
        {
            var session = new SessionBuilder().WithLockedItem(0, 0, Item(Wood, 2)).WithItem(1, 0, Item(Wood, 2)).Build();

            var outcome = session.Drop(Source, Target);

            Assert.That(outcome.RejectReason, Is.EqualTo(DropRejectReason.SourceLocked));
            Assert.That(session.Board.GetItem(Target).ItemId, Is.EqualTo(Item(Wood, 2)));
            Assert.That(session.Score, Is.Zero);
        }

        [Test]
        public void PreviewDrop_FromLockedItem_PredictsTheRejection()
        {
            var session = new SessionBuilder().WithLockedItem(0, 0, Item(Wood, 2)).Build();

            Assert.That(session.PreviewDrop(Source, Target).RejectReason, Is.EqualTo(DropRejectReason.SourceLocked));
        }

        [Test]
        public void Drop_FreeItemOntoMatchingLockedItem_MergesIntoAnUnlockedItem()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Stone, 2)).WithLockedItem(1, 0, Item(Stone, 2)).Build();

            var outcome = session.Drop(Source, Target);

            Assert.That(outcome.Kind, Is.EqualTo(DropOutcomeKind.Merge));
            var result = session.Board.GetItem(Target);
            Assert.That(result.ItemId, Is.EqualTo(Item(Stone, 3)));
            Assert.That(result.IsLocked, Is.False);
            Assert.That(session.Board.IsEmpty(Source), Is.True);
            Assert.That(session.Score, Is.EqualTo(20));
        }

        [Test]
        public void ItemsMerged_ReportsWhetherTheTargetWasLocked()
        {
            var unlocking = new SessionBuilder().WithItem(0, 0, Item(Wood, 1)).WithLockedItem(1, 0, Item(Wood, 1)).Build();
            var plain = new SessionBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).Build();
            var unlockingRecorder = new EventRecorder(unlocking);
            var plainRecorder = new EventRecorder(plain);

            unlocking.Drop(Source, Target);
            plain.Drop(Source, Target);

            Assert.That(unlockingRecorder.Single<ItemsMerged>().TargetWasLocked, Is.True);
            Assert.That(plainRecorder.Single<ItemsMerged>().TargetWasLocked, Is.False);
        }

        [TestCase(Wood, 2, Stone, 2, DropRejectReason.DifferentChain)]
        [TestCase(Wood, 2, Wood, 3, DropRejectReason.DifferentLevel)]
        [TestCase(Wood, ChainLength, Wood, ChainLength, DropRejectReason.MaxLevel)]
        public void Drop_FreeItemOntoMismatchingLockedItem_IsRejectedWithTheUsualReason(
            string sourceChain, int sourceLevel, string targetChain, int targetLevel, DropRejectReason expected)
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(sourceChain, sourceLevel))
                .WithLockedItem(1, 0, Item(targetChain, targetLevel))
                .Build();

            var outcome = session.Drop(Source, Target);

            Assert.That(outcome.RejectReason, Is.EqualTo(expected));
            Assert.That(session.Board.GetItem(Target).IsLocked, Is.True);
        }

        [Test]
        public void LockedItems_OccupyTheirCells()
        {
            var session = new SessionBuilder().WithFullBoardOf(Item(Flower, 1), isLocked: true).Build();

            Assert.That(session.Board.IsFull, Is.True);
            Assert.That(session.CanSpawn, Is.False);
        }

        [Test]
        public void Spawn_NeverChoosesACellHoldingALockedItem()
        {
            var session = new SessionBuilder().WithDefinition(Definition(2, 1)).WithLockedItem(0, 0, Item(Wood, 1)).Build();

            var result = session.Spawn();

            Assert.That(result.Placement.Position, Is.EqualTo(new GridPosition(1, 0)));
            Assert.That(session.Board.GetItem(Source).IsLocked, Is.True);
        }

        [Test]
        public void Locking_IsEnforcedByTheRulePipeline_NotByTheResolver()
        {
            var rules = new IDropRule[] { new MoveToEmptyCellRule(), new MergeRule() };
            var definition = new GameDefinition(DefaultCatalog(), new BoardSize(5, 5), new ScoreTable(DefaultPoints), rules);
            var session = new SessionBuilder().WithDefinition(definition).WithLockedItem(0, 0, Item(Wood, 1)).Build();

            var outcome = session.Drop(Source, Target);

            Assert.That(outcome.Kind, Is.EqualTo(DropOutcomeKind.Move));
            Assert.That(session.Board.GetItem(Target).IsLocked, Is.True);
        }
    }
}
