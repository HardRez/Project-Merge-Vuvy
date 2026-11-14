using MergeGame.Core.Grid;
using MergeGame.Presentation.Board;
using MergeGame.Presentation.Notices;
using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;
using static MergeGame.Presentation.Tests.Support.TestContent;

namespace MergeGame.Presentation.Tests
{
    public sealed class BoardPresenterTests
    {
        [Test]
        public void Initialize_CreatesAGridOfTheSessionsSize_AndShowsTheInitialItems()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithSize(3, 4)
                .WithItem(0, 0, Item(Wood, 2))
                .WithLockedItem(2, 3, Item(Stone, 1)));

            Assert.That(screen.Board.GridSize, Is.EqualTo(new BoardSize(3, 4)));
            Assert.That(screen.Board.Items, Is.EquivalentTo(screen.SessionItems()));
            Assert.That(screen.Board.Items[At(2, 3)].IsLocked, Is.True);
        }

        [Test]
        public void StartDrag_OnAFreeItem_LiftsIt()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(1, 1, Item(Wood, 1)));

            screen.Board.StartDrag(1, 1);

            Assert.That(screen.Board.DraggedItem, Is.EqualTo(At(1, 1)));
        }

        [Test]
        public void StartDrag_OnALockedItem_IsRefusedWithANotice_AndTheDragGoesNowhere()
        {
            using var screen = GameScreen.For(new GameBuilder().WithLockedItem(0, 0, Item(Wood, 1)));

            screen.Board.StartDrag(0, 0);
            screen.Board.MoveFingerTo(1, 0);
            screen.Board.Release(1, 0);

            Assert.That(screen.Board.RefusedDrags, Is.EqualTo(new[] { At(0, 0) }));
            Assert.That(screen.Notices.Notices, Is.EqualTo(new[] { Notice.ItemIsLocked }));
            Assert.That(screen.Board.DraggedItem, Is.Null);
            Assert.That(screen.Board.Highlight, Is.Null);
            Assert.That(screen.Session.Board.IsEmpty(At(1, 0)), Is.True);
            Assert.That(screen.Board.ReturnedItems, Is.Empty);
        }

        [Test]
        public void StartDrag_OnAnEmptyCell_DoesNothing()
        {
            using var screen = GameScreen.For(new GameBuilder());
            screen.Log.Clear();

            screen.Board.StartDrag(2, 2);
            screen.Board.Release(3, 3);

            Assert.That(screen.Log.Entries, Is.Empty);
        }

        [TestCase(1, 0, DropHighlight.Merge)]
        [TestCase(2, 0, DropHighlight.Invalid)]
        [TestCase(3, 0, DropHighlight.Invalid)]
        [TestCase(4, 0, DropHighlight.MoveToEmptyCell)]
        [TestCase(0, 1, DropHighlight.Merge)]
        [TestCase(1, 1, DropHighlight.Invalid)]
        public void Hovering_HighlightsTheCellByWhatADropThereWouldDo(int x, int y, DropHighlight expected)
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithItem(2, 0, Item(Stone, 1))
                .WithItem(3, 0, Item(Wood, 2))
                .WithLockedItem(0, 1, Item(Wood, 1))
                .WithLockedItem(1, 1, Item(Stone, 1)));

            screen.Board.StartDrag(0, 0);
            screen.Board.MoveFingerTo(x, y);

            Assert.That(screen.Board.HighlightedCell, Is.EqualTo(At(x, y)));
            Assert.That(screen.Board.Highlight, Is.EqualTo(expected));
        }

        [Test]
        public void Hovering_AnEqualItemAtMaxLevel_HighlightsMaxLevel()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, ChainLength))
                .WithItem(1, 0, Item(Wood, ChainLength)));

            screen.Board.StartDrag(0, 0);
            screen.Board.MoveFingerTo(1, 0);

            Assert.That(screen.Board.Highlight, Is.EqualTo(DropHighlight.MaxLevel));
        }

        [Test]
        public void Hovering_TheOriginOrOutsideTheBoard_ClearsTheHighlight()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));
            screen.Board.StartDrag(0, 0);

            screen.Board.MoveFingerTo(1, 0);
            screen.Board.MoveFingerTo(0, 0);
            Assert.That(screen.Board.Highlight, Is.Null);

            screen.Board.MoveFingerTo(1, 0);
            screen.Board.MoveFingerOffBoard();
            Assert.That(screen.Board.Highlight, Is.Null);
        }

        [Test]
        public void Hovering_TheSameCellAgain_DoesNotRepeatTheHighlight()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));
            screen.Board.StartDrag(0, 0);
            screen.Log.Clear();

            screen.Board.MoveFingerTo(1, 0);
            screen.Board.MoveFingerTo(1, 0);
            screen.Board.MoveFingerOffBoard();
            screen.Board.MoveFingerOffBoard();

            Assert.That(screen.Log.Entries, Is.EqualTo(new[]
            {
                "Board.ShowDropHighlight MoveToEmptyCell at (1, 0)", "Board.ClearDropHighlight"
            }));
        }

        [Test]
        public void Release_OutsideTheBoard_ReturnsTheItem_AndLeavesTheBoardUnchanged()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));
            screen.Board.StartDrag(0, 0);
            screen.Board.MoveFingerTo(1, 0);

            screen.Board.ReleaseOffBoard();

            Assert.That(screen.Board.ReturnedItems, Is.EqualTo(new[] { At(0, 0) }));
            Assert.That(screen.Board.Highlight, Is.Null);
            Assert.That(screen.Session.Board.GetItem(At(0, 0)).ItemId, Is.EqualTo(Item(Wood, 1)));
            Assert.That(screen.Notices.Notices, Is.Empty);
        }

        [Test]
        public void Release_OnTheOrigin_ReturnsTheItemWithoutANotice()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));

            screen.Board.StartDrag(0, 0);
            screen.Board.Release(0, 0);

            Assert.That(screen.Board.ReturnedItems, Is.EqualTo(new[] { At(0, 0) }));
            Assert.That(screen.Notices.Notices, Is.Empty);
        }

        [Test]
        public void Release_OnAnEmptyCell_MovesTheItem()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));

            screen.Board.DragAndDrop(0, 0, 3, 2);

            Assert.That(screen.Session.Board.GetItem(At(3, 2)).ItemId, Is.EqualTo(Item(Wood, 1)));
            Assert.That(screen.Board.Items, Is.EquivalentTo(screen.SessionItems()));
            Assert.That(screen.Board.Highlight, Is.Null);
            Assert.That(screen.Board.ReturnedItems, Is.Empty);
        }

        [Test]
        public void Release_OnAnEqualItem_MergesIntoTheResult_AndShowsTheAwardedPointsThere()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 2)).WithItem(1, 0, Item(Wood, 2)));

            screen.Board.DragAndDrop(0, 0, 1, 0);

            Assert.That(screen.Board.Merges, Is.EqualTo(new[] { (At(0, 0), At(1, 0), Item(Wood, 3), false) }));
            Assert.That(screen.Board.PointsPopups, Is.EqualTo(new[] { (At(1, 0), 20) }));
            Assert.That(screen.Board.Items, Is.EquivalentTo(screen.SessionItems()));
        }

        [Test]
        public void Release_OnAMatchingLockedItem_MergesWithTheUnlockEffect()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Stone, 1)).WithLockedItem(1, 0, Item(Stone, 1)));

            screen.Board.DragAndDrop(0, 0, 1, 0);

            Assert.That(screen.Board.Merges, Is.EqualTo(new[] { (At(0, 0), At(1, 0), Item(Stone, 2), true) }));
            Assert.That(screen.Board.Items[At(1, 0)].IsLocked, Is.False);
        }

        [TestCase(Stone, 1, Notice.DifferentChain)]
        [TestCase(Wood, 2, Notice.DifferentLevel)]
        [TestCase(Wood, ChainLength, Notice.MaxLevelReached)]
        public void Release_OnAnItemThatCannotMerge_ReturnsTheItemAndTellsWhy(string chainId, int level, Notice expected)
        {
            var sourceLevel = expected == Notice.MaxLevelReached ? ChainLength : 1;
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, sourceLevel))
                .WithItem(1, 0, Item(chainId, level)));

            screen.Board.DragAndDrop(0, 0, 1, 0);

            Assert.That(screen.Board.ReturnedItems, Is.EqualTo(new[] { At(0, 0) }));
            Assert.That(screen.Notices.Notices, Is.EqualTo(new[] { expected }));
            Assert.That(screen.Board.Items, Is.EquivalentTo(screen.SessionItems()));
        }

        [Test]
        public void SpawnDuringADrag_UpdatesTheHighlightOfTheHoveredCell()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(4, 4, Item(Wood, 1)));
            screen.Board.StartDrag(4, 4);
            screen.Board.MoveFingerTo(0, 0);
            Assert.That(screen.Board.Highlight, Is.EqualTo(DropHighlight.MoveToEmptyCell));

            screen.Spawn.PressSpawn();

            Assert.That(screen.Session.Board.GetItem(At(0, 0)).ItemId, Is.EqualTo(Item(Wood, 1)));
            Assert.That(screen.Board.Highlight, Is.EqualTo(DropHighlight.Merge));
        }

        [Test]
        public void DiscardingTheDraggedItem_EndsTheDrag()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));
            screen.Selection.Tap(0, 0);
            screen.Board.StartDrag(0, 0);
            screen.Board.MoveFingerTo(1, 0);

            screen.Info.RequestDiscard();
            screen.Board.Release(1, 0);

            Assert.That(screen.Board.Highlight, Is.Null);
            Assert.That(screen.Board.ReturnedItems, Is.Empty);
            Assert.That(screen.Board.Items, Is.Empty);
        }

        [Test]
        public void SecondDragStart_WhileDragging_IsIgnored()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Stone, 1)));

            screen.Board.StartDrag(0, 0);
            screen.Board.StartDrag(1, 0);
            screen.Board.Release(2, 0);

            Assert.That(screen.Session.Board.GetItem(At(2, 0)).ItemId, Is.EqualTo(Item(Wood, 1)));
            Assert.That(screen.Session.Board.GetItem(At(1, 0)).ItemId, Is.EqualTo(Item(Stone, 1)));
        }
    }
}
