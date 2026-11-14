using MergeGame.Presentation.Board;
using MergeGame.Presentation.Notices;
using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;
using static MergeGame.Presentation.Tests.Support.TestContent;

namespace MergeGame.Presentation.Tests
{
    /// <summary>Closed boxes: placed without their content, inert for the player, revealed when a neighbour is freed.</summary>
    public sealed class HiddenItemPresentationTests
    {
        /// <summary>A free wood 1 at (0, 0) that can free the webbed wood 1 at (2, 2), next to boxes at (3, 2) and (3, 3).</summary>
        private static GameBuilder BoardWithBoxes() =>
            new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithLockedItem(2, 2, Item(Wood, 1))
                .WithHiddenItem(3, 2, Item(Stone, 2))
                .WithHiddenItem(3, 3, Item(Wood, 3));

        [Test]
        public void Initialize_PlacesBoxesWithoutTellingTheViewWhatIsInside()
        {
            using var screen = GameScreen.For(BoardWithBoxes());

            Assert.That(screen.Board.Items[At(3, 2)], Is.EqualTo(ShownItem.Box));
            Assert.That(screen.Board.Items, Is.EquivalentTo(screen.SessionItems()));
            Assert.That(screen.Log.Entries, Has.Member("Board.PlaceHiddenItem at (3, 2)"));
            Assert.That(screen.Log.Only("Board.PlaceItem"), Has.None.Contains("stone:2").And.None.Contains("wood:3"));
        }

        [Test]
        public void StartDrag_OnABox_IsRefusedWithTheHiddenNotice()
        {
            using var screen = GameScreen.For(BoardWithBoxes());

            screen.Board.StartDrag(3, 2);
            screen.Board.Release(4, 4);

            Assert.That(screen.Board.RefusedDrags, Is.EqualTo(new[] { At(3, 2) }));
            Assert.That(screen.Notices.Notices, Is.EqualTo(new[] { Notice.ItemIsHidden }));
            Assert.That(screen.Board.DraggedItem, Is.Null);
            Assert.That(screen.Session.Board.GetItem(At(3, 2)).IsHidden, Is.True);
        }

        [Test]
        public void HoveringABox_HighlightsItAsInvalid_EvenWhenItsContentWouldMatch()
        {
            using var screen = GameScreen.For(BoardWithBoxes().WithItem(4, 4, Item(Stone, 2)));

            screen.Board.StartDrag(4, 4);
            screen.Board.MoveFingerTo(3, 2);

            Assert.That(screen.Board.Highlight, Is.EqualTo(DropHighlight.Invalid));
        }

        [Test]
        public void DropOntoABox_ReturnsTheItemWithTheHiddenNotice()
        {
            using var screen = GameScreen.For(BoardWithBoxes().WithItem(4, 4, Item(Stone, 2)));

            screen.Board.DragAndDrop(4, 4, 3, 2);

            Assert.That(screen.Board.ReturnedItems, Is.EqualTo(new[] { At(4, 4) }));
            Assert.That(screen.Notices.Notices, Is.EqualTo(new[] { Notice.ItemIsHidden }));
            Assert.That(screen.Board.Items, Is.EquivalentTo(screen.SessionItems()));
        }

        [Test]
        public void FreeingAWebbedItem_RevealsTheNeighbouringBox_AfterTheMergeAndItsPoints()
        {
            using var screen = GameScreen.For(BoardWithBoxes());
            screen.Log.Clear();

            screen.Board.DragAndDrop(0, 0, 2, 2);

            Assert.That(screen.Log.Only("Board.MergeItems", "Board.ShowPointsPopup", "Board.RevealItem"), Is.EqualTo(new[]
            {
                "Board.MergeItems (0, 0) -> (2, 2) = wood:2 unlocked",
                "Board.ShowPointsPopup +10 at (2, 2)",
                "Board.RevealItem stone:2 at (3, 2)"
            }));
            Assert.That(screen.Board.Items[At(3, 2)], Is.EqualTo(new ShownItem(Item(Stone, 2), true)));
            Assert.That(screen.Board.Items[At(3, 3)], Is.EqualTo(ShownItem.Box));
            Assert.That(screen.Board.Items, Is.EquivalentTo(screen.SessionItems()));
        }

        [Test]
        public void RevealDuringADrag_UpdatesTheHighlightOfTheHoveredBox()
        {
            using var screen = GameScreen.For(BoardWithBoxes().WithItem(4, 4, Item(Stone, 2)));
            screen.Board.StartDrag(4, 4);
            screen.Board.MoveFingerTo(3, 2);
            Assert.That(screen.Board.Highlight, Is.EqualTo(DropHighlight.Invalid));

            screen.Session.Drop(At(0, 0), At(2, 2));

            Assert.That(screen.Board.Highlight, Is.EqualTo(DropHighlight.Merge));
        }

        [Test]
        public void SelectingABox_ShowsTheHiddenState_WithoutItsContent()
        {
            using var screen = GameScreen.For(BoardWithBoxes());

            screen.Selection.Tap(3, 2);

            Assert.That(screen.Selection.Marker, Is.EqualTo(At(3, 2)));
            Assert.That(screen.Info.IsShowingHiddenItem, Is.True);
            Assert.That(screen.Info.Info, Is.Null);
        }

        [Test]
        public void DiscardRequest_ForASelectedBox_ShowsTheHiddenNoticeAndKeepsIt()
        {
            using var screen = GameScreen.For(BoardWithBoxes());
            screen.Selection.Tap(3, 2);

            screen.Info.RequestDiscard();

            Assert.That(screen.Notices.Notices, Is.EqualTo(new[] { Notice.ItemIsHidden }));
            Assert.That(screen.Session.Board.GetItem(At(3, 2)).IsHidden, Is.True);
        }

        [Test]
        public void SelectedBox_ThatIsRevealed_LeavesTheSelectionOnTheMergeResult_AndThenDescribesTheLockedItem()
        {
            using var screen = GameScreen.For(BoardWithBoxes());
            screen.Selection.Tap(3, 2);

            screen.Session.Drop(At(0, 0), At(2, 2));

            Assert.That(screen.Selection.Marker, Is.EqualTo(At(2, 2)), "A merge selects its result, also when it opens the selected box.");
            Assert.That(screen.Info.IsShowingHiddenItem, Is.False);

            screen.Selection.Tap(3, 2);

            Assert.That(screen.Info.Info.ItemId, Is.EqualTo(Item(Stone, 2)));
            Assert.That(screen.Info.Info.IsLocked, Is.True);
            Assert.That(screen.Info.Info.CanDiscard, Is.False);
        }

        [Test]
        public void Boxes_NeverFulfilOrders_NotEvenOnceRevealedAndStillLocked()
        {
            using var screen = GameScreen.For(BoardWithBoxes().WithOrders(1, new[] { Item(Stone, 2) }));
            Assert.That(screen.Orders.Cards[0].Requirements[0].IsFulfilled, Is.False);

            screen.Board.DragAndDrop(0, 0, 2, 2);

            Assert.That(screen.Orders.Cards[0].Requirements[0].IsFulfilled, Is.False);
            Assert.That(screen.Orders.Cards[0].CanDeliver, Is.False);
        }

        [Test]
        public void MergeHint_IgnoresBoxes()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithLockedItem(4, 0, Item(Stone, 1))
                .WithHiddenItem(4, 1, Item(Wood, 1))
                .WithItem(0, 0, Item(Wood, 1)));

            screen.Advance(GameScreen.HintDelaySeconds * 3);

            Assert.That(screen.Hint.ShowCount, Is.Zero);
        }

        [Test]
        public void MergeHint_SuggestsFreeingTheWebbedItemNextToABox()
        {
            using var screen = GameScreen.For(BoardWithBoxes());

            screen.Advance(GameScreen.HintDelaySeconds);

            Assert.That(screen.Hint.Shown, Is.EqualTo((At(0, 0), At(2, 2))));
        }
    }
}
