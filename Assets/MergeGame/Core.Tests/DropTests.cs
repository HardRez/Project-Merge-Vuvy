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
    public sealed class DropTests
    {
        private static readonly GridPosition Source = new GridPosition(0, 0);
        private static readonly GridPosition Target = new GridPosition(1, 0);

        [Test]
        public void Drop_OnSameChainAndLevel_MergesIntoNextLevel()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Wood, 2)).WithItem(1, 0, Item(Wood, 2)).Build();

            var outcome = session.Drop(Source, Target);

            Assert.That(outcome.Kind, Is.EqualTo(DropOutcomeKind.Merge));
            Assert.That(outcome.ResultItemId, Is.EqualTo(Item(Wood, 3)));
            Assert.That(session.Board.GetItem(Target).ItemId, Is.EqualTo(Item(Wood, 3)));
        }

        [Test]
        public void Drop_Merge_EmptiesTheSourceCell()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).Build();

            session.Drop(Source, Target);

            Assert.That(session.Board.IsEmpty(Source), Is.True);
            Assert.That(session.Board.EmptyCellCount, Is.EqualTo(24));
        }

        [Test]
        public void Drop_OnEmptyCell_MovesTheItem()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Stone, 3)).Build();
            var recorder = new EventRecorder(session);

            var outcome = session.Drop(Source, Target);

            Assert.That(outcome.Kind, Is.EqualTo(DropOutcomeKind.Move));
            Assert.That(session.Board.IsEmpty(Source), Is.True);
            Assert.That(session.Board.GetItem(Target).ItemId, Is.EqualTo(Item(Stone, 3)));
            Assert.That(recorder.Single<ItemMoved>().ItemId, Is.EqualTo(Item(Stone, 3)));
        }

        [Test]
        public void Drop_OnEmptyCell_DoesNotChangeTheScore()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Stone, 3)).Build();

            session.Drop(Source, Target);

            Assert.That(session.Score, Is.Zero);
        }

        [TestCase(Wood, 2, Stone, 2, DropRejectReason.DifferentChain)]
        [TestCase(Wood, 2, Wood, 3, DropRejectReason.DifferentLevel)]
        [TestCase(Wood, ChainLength, Wood, ChainLength, DropRejectReason.MaxLevel)]
        public void Drop_OnIncompatibleItem_IsRejectedWithReason(string sourceChain, int sourceLevel, string targetChain, int targetLevel, DropRejectReason expected)
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(sourceChain, sourceLevel))
                .WithItem(1, 0, Item(targetChain, targetLevel))
                .Build();

            var outcome = session.Drop(Source, Target);

            Assert.That(outcome.Kind, Is.EqualTo(DropOutcomeKind.Reject));
            Assert.That(outcome.RejectReason, Is.EqualTo(expected));
        }

        [TestCase(Wood, 2, Stone, 2)]
        [TestCase(Wood, 2, Wood, 3)]
        [TestCase(Wood, ChainLength, Wood, ChainLength)]
        public void Drop_WhenRejected_LeavesTheBoardAndScoreUntouched(string sourceChain, int sourceLevel, string targetChain, int targetLevel)
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(sourceChain, sourceLevel))
                .WithItem(1, 0, Item(targetChain, targetLevel))
                .Build();

            session.Drop(Source, Target);

            Assert.That(session.Board.GetItem(Source).ItemId, Is.EqualTo(Item(sourceChain, sourceLevel)));
            Assert.That(session.Board.GetItem(Target).ItemId, Is.EqualTo(Item(targetChain, targetLevel)));
            Assert.That(session.Score, Is.Zero);
        }

        [Test]
        public void Drop_OnMaxLevelPair_RaisesDropRejectedWithMaxLevelReason()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Flower, ChainLength)).WithItem(1, 0, Item(Flower, ChainLength)).Build();
            var recorder = new EventRecorder(session);

            session.Drop(Source, Target);

            var rejected = recorder.Single<DropRejected>();
            Assert.That(rejected.Reason, Is.EqualTo(DropRejectReason.MaxLevel));
            Assert.That(rejected.From, Is.EqualTo(Source));
            Assert.That(rejected.To, Is.EqualTo(Target));
            Assert.That(recorder.Events.Count, Is.EqualTo(1));
        }

        [Test]
        public void Drop_OntoItsOwnCell_IsRejectedAsSameCell()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Wood, 1)).Build();

            Assert.That(session.Drop(Source, Source).RejectReason, Is.EqualTo(DropRejectReason.SameCell));
        }

        [Test]
        public void Drop_FromEmptyCell_IsRejectedAsSourceEmpty()
        {
            var session = new SessionBuilder().WithItem(1, 0, Item(Wood, 1)).Build();

            Assert.That(session.Drop(Source, Target).RejectReason, Is.EqualTo(DropRejectReason.SourceEmpty));
        }

        [Test]
        public void Drop_OutsideTheBoard_IsRejectedAsOutOfBounds()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Wood, 1)).Build();

            Assert.That(session.Drop(Source, new GridPosition(5, 0)).RejectReason, Is.EqualTo(DropRejectReason.OutOfBounds));
        }

        [Test]
        public void PreviewDrop_DoesNotChangeTheBoardOrRaiseEvents()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).Build();
            var recorder = new EventRecorder(session);

            var preview = session.PreviewDrop(Source, Target);

            Assert.That(preview.Kind, Is.EqualTo(DropOutcomeKind.Merge));
            Assert.That(session.Board.GetItem(Source).ItemId, Is.EqualTo(Item(Wood, 1)));
            Assert.That(session.Board.GetItem(Target).ItemId, Is.EqualTo(Item(Wood, 1)));
            Assert.That(recorder.Events, Is.Empty);
        }

        [TestCase(Wood, 1, Wood, 1)]
        [TestCase(Wood, 1, Stone, 1)]
        [TestCase(Wood, 1, Wood, 2)]
        [TestCase(Wood, ChainLength, Wood, ChainLength)]
        public void PreviewDrop_AgreesWithTheActualDrop(string sourceChain, int sourceLevel, string targetChain, int targetLevel)
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(sourceChain, sourceLevel))
                .WithItem(1, 0, Item(targetChain, targetLevel))
                .Build();

            var preview = session.PreviewDrop(Source, Target);
            var actual = session.Drop(Source, Target);

            Assert.That(actual.Kind, Is.EqualTo(preview.Kind));
            Assert.That(actual.RejectReason, Is.EqualTo(preview.RejectReason));
        }

        [Test]
        public void DropRule_PlacedBeforeTheStandardRules_IsConsultedFirst()
        {
            var rules = StandardDropRules.Create();
            rules.Insert(0, new RejectEverythingRule());
            var definition = new GameDefinition(DefaultCatalog(), new BoardSize(5, 5), new ScoreTable(DefaultPoints), rules);
            var session = new SessionBuilder().WithDefinition(definition).WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).Build();

            var outcome = session.Drop(Source, Target);

            Assert.That(outcome.Kind, Is.EqualTo(DropOutcomeKind.Reject));
            Assert.That(session.Board.GetItem(Source).ItemId, Is.EqualTo(Item(Wood, 1)));
        }

        [Test]
        public void DropRule_AnsweringForOtherCells_IsRefusedBeforeAnythingChanges()
        {
            var session = SessionWithFirstRule(new MoveElsewhereRule());

            var exception = Assert.Throws<System.InvalidOperationException>(() => session.Drop(Source, Target));
            Assert.That(exception.Message, Does.Contain(nameof(MoveElsewhereRule)));
            Assert.That(session.Board.GetItem(Source).ItemId, Is.EqualTo(Item(Wood, 1)));
        }

        [Test]
        public void DropRule_ClaimingTheDropWithoutOutcome_IsRefused()
        {
            var session = SessionWithFirstRule(new NullOutcomeRule());

            Assert.Throws<System.InvalidOperationException>(() => session.PreviewDrop(Source, Target));
        }

        private static MergeGameSession SessionWithFirstRule(IDropRule rule)
        {
            var rules = StandardDropRules.Create();
            rules.Insert(0, rule);
            var definition = new GameDefinition(DefaultCatalog(), new BoardSize(5, 5), new ScoreTable(DefaultPoints), rules);
            return new SessionBuilder().WithDefinition(definition).WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).Build();
        }

        private sealed class MoveElsewhereRule : IDropRule
        {
            public bool TryResolve(DropContext context, out DropOutcome outcome)
            {
                outcome = DropOutcome.Move(context.From, new GridPosition(4, 4));
                return true;
            }
        }

        private sealed class NullOutcomeRule : IDropRule
        {
            public bool TryResolve(DropContext context, out DropOutcome outcome)
            {
                outcome = null;
                return true;
            }
        }

        private sealed class RejectEverythingRule : IDropRule
        {
            public bool TryResolve(DropContext context, out DropOutcome outcome)
            {
                outcome = context.Reject(DropRejectReason.DifferentChain);
                return true;
            }
        }
    }
}
