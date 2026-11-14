using MergeGame.Presentation.Notices;
using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;
using static MergeGame.Presentation.Tests.Support.TestContent;

namespace MergeGame.Presentation.Tests
{
    public sealed class SelectionPresenterTests
    {
        [Test]
        public void Initialize_ShowsTheHint()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));

            Assert.That(screen.Info.IsShowingHint, Is.True);
            Assert.That(screen.Selection.Marker, Is.Null);
        }

        [Test]
        public void TappingAnItem_SelectsIt_AndDescribesIt()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(2, 1, Item(Stone, 2)));

            screen.Selection.Tap(2, 1);

            Assert.That(screen.Selection.Marker, Is.EqualTo(At(2, 1)));
            var info = screen.Info.Info;
            Assert.That(info.Position, Is.EqualTo(At(2, 1)));
            Assert.That(info.ItemId, Is.EqualTo(Item(Stone, 2)));
            Assert.That(info.DisplayName, Is.EqualTo(DisplayName(Item(Stone, 2))));
            Assert.That(info.Level, Is.EqualTo(2));
            Assert.That(info.MaxLevel, Is.EqualTo(ChainLength));
            Assert.That(info.IsMaxLevel, Is.False);
            Assert.That(info.IsLocked, Is.False);
            Assert.That(info.NextItemId, Is.EqualTo(Item(Stone, 3)));
            Assert.That(info.CanDiscard, Is.True);
        }

        [Test]
        public void TappingAMaxLevelItem_DescribesItAsMaxLevelWithoutANextItem()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, ChainLength)));

            screen.Selection.Tap(0, 0);

            Assert.That(screen.Info.Info.IsMaxLevel, Is.True);
            Assert.That(screen.Info.Info.NextItemId, Is.Null);
        }

        [Test]
        public void TappingALockedItem_DescribesItAsLockedAndNotDiscardable()
        {
            using var screen = GameScreen.For(new GameBuilder().WithLockedItem(0, 0, Item(Wood, 1)));

            screen.Selection.Tap(0, 0);

            Assert.That(screen.Info.Info.IsLocked, Is.True);
            Assert.That(screen.Info.Info.CanDiscard, Is.False);
        }

        [Test]
        public void TappingAnEmptyCell_ClearsTheSelection()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));
            screen.Selection.Tap(0, 0);

            screen.Selection.Tap(4, 4);

            Assert.That(screen.Selection.Marker, Is.Null);
            Assert.That(screen.Info.IsShowingHint, Is.True);
            Assert.That(screen.SelectionPresenter.SelectedPosition, Is.Null);
        }

        [Test]
        public void MovingTheSelectedItem_MovesTheSelectionWithIt()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));
            screen.Selection.Tap(0, 0);

            screen.Board.DragAndDrop(0, 0, 3, 3);

            Assert.That(screen.Selection.Marker, Is.EqualTo(At(3, 3)));
            Assert.That(screen.Info.Info.Position, Is.EqualTo(At(3, 3)));
        }

        [Test]
        public void MovingAnotherItem_KeepsTheSelection()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Stone, 1)));
            screen.Selection.Tap(0, 0);

            screen.Board.DragAndDrop(1, 0, 3, 3);

            Assert.That(screen.Selection.Marker, Is.EqualTo(At(0, 0)));
        }

        [Test]
        public void Merging_SelectsTheResult_EvenWhenAnotherItemWasSelected()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithItem(4, 4, Item(Stone, 1)));
            screen.Selection.Tap(4, 4);

            screen.Board.DragAndDrop(0, 0, 1, 0);

            Assert.That(screen.Selection.Marker, Is.EqualTo(At(1, 0)));
            Assert.That(screen.Info.Info.ItemId, Is.EqualTo(Item(Wood, 2)));
        }

        [Test]
        public void MergingOntoTheSelectedLockedItem_ShowsTheResultUnlocked()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)).WithLockedItem(1, 0, Item(Wood, 1)));
            screen.Selection.Tap(1, 0);

            screen.Board.DragAndDrop(0, 0, 1, 0);

            Assert.That(screen.Info.Info.IsLocked, Is.False);
            Assert.That(screen.Info.Info.CanDiscard, Is.True);
        }

        [Test]
        public void DiscardRequest_RemovesTheSelectedItem_AndClearsTheSelection()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));
            screen.Selection.Tap(0, 0);

            screen.Info.RequestDiscard();

            Assert.That(screen.Session.Board.IsEmpty(At(0, 0)), Is.True);
            Assert.That(screen.Board.Items, Is.Empty);
            Assert.That(screen.Selection.Marker, Is.Null);
            Assert.That(screen.Info.IsShowingHint, Is.True);
        }

        [Test]
        public void DiscardRequest_ForALockedItem_ShowsANoticeAndKeepsIt()
        {
            using var screen = GameScreen.For(new GameBuilder().WithLockedItem(0, 0, Item(Wood, 1)));
            screen.Selection.Tap(0, 0);

            screen.Info.RequestDiscard();

            Assert.That(screen.Notices.Notices, Is.EqualTo(new[] { Notice.LockedItemCannotBeDiscarded }));
            Assert.That(screen.Session.Board.IsEmpty(At(0, 0)), Is.False);
            Assert.That(screen.Selection.Marker, Is.EqualTo(At(0, 0)));
        }

        [Test]
        public void DiscardRequest_WithoutSelection_DoesNothing()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));
            screen.Log.Clear();

            screen.Info.RequestDiscard();

            Assert.That(screen.Log.Entries, Is.Empty);
            Assert.That(screen.Session.Board.IsEmpty(At(0, 0)), Is.False);
        }

        [Test]
        public void DeliveringTheSelectedItem_ClearsTheSelection()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithOrders(1, new[] { Item(Wood, 1) }));
            screen.Selection.Tap(0, 0);

            screen.Orders.RequestDelivery(0);

            Assert.That(screen.Selection.Marker, Is.Null);
            Assert.That(screen.Info.IsShowingHint, Is.True);
        }

        [Test]
        public void DeliveringOtherItems_KeepsTheSelection()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Stone, 1))
                .WithOrders(1, new[] { Item(Wood, 1) }));
            screen.Selection.Tap(1, 0);

            screen.Orders.RequestDelivery(0);

            Assert.That(screen.Selection.Marker, Is.EqualTo(At(1, 0)));
        }

        [Test]
        public void Selection_NeverPointsAtAnEmptyCell_ThroughAMixOfActions()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithSize(3, 2)
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithItem(2, 0, Item(Wood, 2))
                .WithOrders(1, new[] { Item(Wood, 3) }));

            void AssertSelectionValid()
            {
                var selected = screen.SelectionPresenter.SelectedPosition;
                Assert.That(selected, Is.EqualTo(screen.Selection.Marker));
                if (selected.HasValue)
                    Assert.That(screen.Session.Board.IsEmpty(selected.Value), Is.False);
            }

            screen.Selection.Tap(0, 0);
            screen.Board.DragAndDrop(0, 0, 1, 0);
            AssertSelectionValid();
            screen.Board.DragAndDrop(1, 0, 2, 0);
            AssertSelectionValid();
            screen.Spawn.PressSpawn();
            AssertSelectionValid();
            screen.Orders.RequestDelivery(0);
            AssertSelectionValid();
            Assert.That(screen.Selection.Marker, Is.Null);
        }
    }
}
