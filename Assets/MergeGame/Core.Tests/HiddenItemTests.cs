using System;
using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Hints;
using MergeGame.Core.Rules;
using MergeGame.Core.Session;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.ScriptedOrderGenerator;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class HiddenItemTests
    {
        private static GridPosition At(int x, int y) => new GridPosition(x, y);

        [Test]
        public void BoardItem_IsVisibleByDefault_AndAHiddenItemIsAlsoLocked()
        {
            Assert.That(new BoardItem(Item(Wood, 1), isLocked: true).IsHidden, Is.False);

            var box = BoardItem.Hidden(Item(Wood, 1));

            Assert.That(box.IsHidden, Is.True);
            Assert.That(box.IsLocked, Is.True);
        }

        [Test]
        public void HiddenButUnlockedItem_IsRejected()
        {
            Assert.That(() => new BoardItem(Item(Wood, 1), isLocked: false, isHidden: true), Throws.ArgumentException);
            Assert.That(() => new InitialPlacement(At(0, 0), Item(Wood, 1), isLocked: false, isHidden: true), Throws.ArgumentException);
        }

        [Test]
        public void HiddenPlacement_StartsTheSessionAsAHiddenLockedItem()
        {
            var definition = Definition(initialPlacements: new[]
            {
                new InitialPlacement(At(0, 0), Item(Wood, 1), isLocked: true),
                InitialPlacement.Hidden(At(1, 0), Item(Stone, 2))
            });

            var item = new MergeGameSession(definition, new ScriptedRandom()).Board.GetItem(At(1, 0));

            Assert.That(item.ItemId, Is.EqualTo(Item(Stone, 2)));
            Assert.That(item.IsHidden, Is.True);
            Assert.That(item.IsLocked, Is.True);
        }

        [Test]
        public void Drop_OntoAHiddenItem_IsRejectedAsTargetHidden_EvenWhenItMatches()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Wood, 1)).WithHiddenItem(1, 0, Item(Wood, 1)).Build();
            var recorder = new EventRecorder(session);

            var outcome = session.Drop(At(0, 0), At(1, 0));

            Assert.That(outcome.RejectReason, Is.EqualTo(DropRejectReason.TargetHidden));
            Assert.That(recorder.Single<DropRejected>().Reason, Is.EqualTo(DropRejectReason.TargetHidden));
            Assert.That(session.Board.GetItem(At(0, 0)).ItemId, Is.EqualTo(Item(Wood, 1)));
            Assert.That(session.Board.GetItem(At(1, 0)).IsHidden, Is.True);
        }

        [Test]
        public void Drop_FromAHiddenItem_IsRejectedAsSourceLocked()
        {
            var session = new SessionBuilder().WithHiddenItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).Build();

            Assert.That(session.PreviewDrop(At(0, 0), At(1, 0)).RejectReason, Is.EqualTo(DropRejectReason.SourceLocked));
            Assert.That(session.PreviewDrop(At(0, 0), At(2, 2)).RejectReason, Is.EqualTo(DropRejectReason.SourceLocked));
        }

        [Test]
        public void FreeingALockedItem_RevealsEveryHiddenOrthogonalNeighbour_AsALockedItem()
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithLockedItem(2, 2, Item(Wood, 1))
                .WithHiddenItem(2, 1, Item(Stone, 1))
                .WithHiddenItem(1, 2, Item(Stone, 2))
                .WithHiddenItem(3, 2, Item(Stone, 3))
                .WithHiddenItem(2, 3, Item(Stone, 4))
                .WithHiddenItem(3, 3, Item(Wood, 4))
                .Build();

            session.Drop(At(0, 0), At(2, 2));

            foreach (var revealed in new[] { At(2, 1), At(1, 2), At(3, 2), At(2, 3) })
            {
                var item = session.Board.GetItem(revealed);
                Assert.That(item.IsHidden, Is.False, $"{revealed} should be revealed.");
                Assert.That(item.IsLocked, Is.True, $"{revealed} should stay locked.");
            }

            Assert.That(session.Board.GetItem(At(3, 3)).IsHidden, Is.True, "Diagonal neighbours stay hidden.");
            Assert.That(session.Board.GetItem(At(2, 2)).IsLocked, Is.False);
        }

        [Test]
        public void Merge_RaisesItsRevealsInRowMajorOrder_BetweenItemsMergedAndScoreChanged()
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithLockedItem(2, 2, Item(Wood, 1))
                .WithHiddenItem(2, 3, Item(Stone, 4))
                .WithHiddenItem(1, 2, Item(Stone, 2))
                .WithHiddenItem(2, 1, Item(Stone, 1))
                .Build();
            var recorder = new EventRecorder(session);

            session.Drop(At(0, 0), At(2, 2));

            Assert.That(recorder.Types, Is.EqualTo(new[]
            {
                typeof(ItemsMerged), typeof(ItemRevealed), typeof(ItemRevealed), typeof(ItemRevealed), typeof(ScoreChanged)
            }));
            var first = (ItemRevealed)recorder.Events[1];
            Assert.That(first.Position, Is.EqualTo(At(2, 1)));
            Assert.That(first.ItemId, Is.EqualTo(Item(Stone, 1)));
            Assert.That(((ItemRevealed)recorder.Events[2]).Position, Is.EqualTo(At(1, 2)));
            Assert.That(((ItemRevealed)recorder.Events[3]).Position, Is.EqualTo(At(2, 3)));
        }

        [Test]
        public void MergeOnAFullBoard_RaisesRevealsBeforeScoreAndBoardFullChanged()
        {
            var session = new SessionBuilder()
                .WithDefinition(Definition(width: 3, height: 1))
                .WithItem(0, 0, Item(Wood, 1))
                .WithLockedItem(1, 0, Item(Wood, 1))
                .WithHiddenItem(2, 0, Item(Stone, 1))
                .Build();
            var recorder = new EventRecorder(session);

            session.Drop(At(0, 0), At(1, 0));

            Assert.That(recorder.Types, Is.EqualTo(new[]
            {
                typeof(ItemsMerged), typeof(ItemRevealed), typeof(ScoreChanged), typeof(BoardFullChanged)
            }));
        }

        [Test]
        public void MergeOntoAFreeItem_RevealsNothing()
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithHiddenItem(2, 0, Item(Stone, 1))
                .Build();
            var recorder = new EventRecorder(session);

            session.Drop(At(0, 0), At(1, 0));

            Assert.That(session.Board.GetItem(At(2, 0)).IsHidden, Is.True);
            Assert.That(recorder.Types, Has.No.Member(typeof(ItemRevealed)));
        }

        [Test]
        public void RevealedItem_DoesNotRevealItsOwnNeighbours_UntilItIsFreed()
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithLockedItem(1, 1, Item(Wood, 1))
                .WithHiddenItem(2, 1, Item(Stone, 2))
                .WithHiddenItem(3, 1, Item(Stone, 3))
                .Build();

            session.Drop(At(0, 0), At(1, 1));

            Assert.That(session.Board.GetItem(At(2, 1)).IsHidden, Is.False);
            Assert.That(session.Board.GetItem(At(3, 1)).IsHidden, Is.True);
        }

        [Test]
        public void RevealChain_EachFreedItemOpensTheNextBox()
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithLockedItem(1, 1, Item(Wood, 1))
                .WithHiddenItem(2, 1, Item(Stone, 2))
                .WithHiddenItem(3, 1, Item(Flower, 3))
                .WithItem(4, 4, Item(Stone, 2))
                .Build();
            var recorder = new EventRecorder(session);

            session.Drop(At(0, 0), At(1, 1));
            Assert.That(session.PreviewDrop(At(4, 4), At(2, 1)).Kind, Is.EqualTo(DropOutcomeKind.Merge));
            session.Drop(At(4, 4), At(2, 1));

            var c = session.Board.GetItem(At(2, 1));
            Assert.That(c.ItemId, Is.EqualTo(Item(Stone, 3)));
            Assert.That(c.IsLocked, Is.False);
            var d = session.Board.GetItem(At(3, 1));
            Assert.That(d.ItemId, Is.EqualTo(Item(Flower, 3)));
            Assert.That(d.IsHidden, Is.False);
            Assert.That(d.IsLocked, Is.True);
            var revealedCells = new System.Collections.Generic.List<GridPosition>();
            foreach (var gameEvent in recorder.Events)
            {
                if (gameEvent is ItemRevealed revealed)
                    revealedCells.Add(revealed.Position);
            }

            Assert.That(revealedCells, Is.EqualTo(new[] { At(2, 1), At(3, 1) }));
        }

        [Test]
        public void HiddenItems_DoNotFulfilOrders()
        {
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 1))), OrderSetup(maxActiveOrders: 1)))
                .WithHiddenItem(0, 0, Item(Wood, 1))
                .Build();
            var orderId = session.ActiveOrders[0].Id;

            Assert.That(session.GetRequirementFulfilment(orderId), Is.EqualTo(new[] { false }));
            Assert.That(session.CanDeliver(orderId), Is.False);
        }

        [Test]
        public void HiddenItems_CannotBeDiscarded()
        {
            var session = new SessionBuilder().WithHiddenItem(0, 0, Item(Wood, 1)).Build();

            Assert.That(session.CanDiscard(At(0, 0)), Is.False);
            Assert.That(session.Discard(At(0, 0)).FailureReason, Is.EqualTo(DiscardFailureReason.ItemLocked));
        }

        [Test]
        public void Spawn_UsesOnlyEmptyCells_AndLeavesHiddenItemsAlone()
        {
            var session = new SessionBuilder()
                .WithDefinition(Definition(width: 2, height: 1))
                .WithHiddenItem(0, 0, Item(Stone, 2))
                .Build();

            var result = session.Spawn();

            Assert.That(result.Placement.Position, Is.EqualTo(At(1, 0)));
            Assert.That(session.Board.GetItem(At(0, 0)).IsHidden, Is.True);
            Assert.That(session.CanSpawn, Is.False);
        }

        [Test]
        public void MergeHint_NeverSuggestsHiddenItems()
        {
            var session = new SessionBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithHiddenItem(1, 0, Item(Wood, 1))
                .WithHiddenItem(2, 0, Item(Wood, 1))
                .Build();

            Assert.That(new MergeHintFinder(session).TryFind(out _), Is.False);
        }

        [Test]
        public void Definition_RejectsAHiddenPlacementThatNothingCanOpen_NamingTheCell()
        {
            var placements = new[]
            {
                new InitialPlacement(At(0, 0), Item(Wood, 1)),
                InitialPlacement.Hidden(At(1, 0), Item(Wood, 1)),
                new InitialPlacement(At(4, 4), Item(Wood, 1), isLocked: true)
            };

            var error = Assert.Throws<ArgumentException>(() => Definition(initialPlacements: placements));

            Assert.That(error.Message, Does.Contain("(1, 0)"));
        }

        [Test]
        public void Definition_AcceptsAGroupOfBoxes_WhenOneOfThemTouchesALockedItem()
        {
            var placements = new[]
            {
                InitialPlacement.Hidden(At(0, 0), Item(Wood, 1)),
                InitialPlacement.Hidden(At(0, 1), Item(Wood, 2)),
                InitialPlacement.Hidden(At(1, 1), Item(Wood, 3)),
                new InitialPlacement(At(2, 1), Item(Stone, 1), isLocked: true)
            };

            Assert.That(() => Definition(initialPlacements: placements), Throws.Nothing);
        }

        [Test]
        public void Definition_RejectsABoxGroupThatOnlyDiagonallyTouchesALockedItem()
        {
            var placements = new[]
            {
                InitialPlacement.Hidden(At(0, 0), Item(Wood, 1)),
                InitialPlacement.Hidden(At(1, 0), Item(Wood, 2)),
                new InitialPlacement(At(2, 1), Item(Stone, 1), isLocked: true)
            };

            var error = Assert.Throws<ArgumentException>(() => Definition(initialPlacements: placements));

            Assert.That(error.Message, Does.Contain("(0, 0)"));
        }
    }
}
