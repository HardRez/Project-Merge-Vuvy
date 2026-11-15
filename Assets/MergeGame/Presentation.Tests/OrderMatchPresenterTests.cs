using MergeGame.Core.Grid;
using MergeGame.Presentation.Events;
using MergeGame.Presentation.Orders;
using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;
using static MergeGame.Presentation.Tests.Support.TestContent;

namespace MergeGame.Presentation.Tests
{
    public sealed class OrderMatchPresenterTests
    {
        [Test]
        public void Initialize_SendsEveryMatchedCellOnce_InRowMajorOrder()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 1, Item(Stone, 2))
                .WithItem(2, 0, Item(Wood, 1))
                .WithItem(4, 4, Item(Wood, 1))
                .WithOrders(2, new[] { Item(Wood, 1), Item(Stone, 2) }, new[] { Item(Wood, 1) }));

            Assert.That(screen.OrderMatches.SentSets, Is.EqualTo(new[] { new[] { At(2, 0), At(0, 1) } }));
        }

        [Test]
        public void CellMatchedByTwoOrders_IsReportedOnce()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(1, 1, Item(Wood, 1))
                .WithOrders(2, new[] { Item(Wood, 1) }, new[] { Item(Stone, 3), Item(Wood, 1) }));

            Assert.That(screen.OrderMatches.Marked, Is.EqualTo(new[] { At(1, 1) }));
        }

        [Test]
        public void LockedAndHiddenItems_AreNeverMarked()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithLockedItem(0, 0, Item(Wood, 1))
                .WithHiddenItem(1, 0, Item(Wood, 1))
                .WithItem(4, 4, Item(Wood, 1))
                .WithOrders(1, new[] { Item(Wood, 1), Item(Wood, 1) }));

            Assert.That(screen.OrderMatches.Marked, Is.EqualTo(new[] { At(4, 4) }));
        }

        [Test]
        public void SpawningANeededItem_AddsItsCell()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(3, 3, Item(Wood, 1))
                .WithOrders(1, new[] { Item(Wood, 1), Item(Wood, 1) }));

            screen.Spawn.PressSpawn();

            Assert.That(screen.OrderMatches.SentSets, Is.EqualTo(new[] { new[] { At(3, 3) }, new[] { At(0, 0), At(3, 3) } }));
        }

        [Test]
        public void SpawningAnItemIdenticalToAMarkedOne_KeepsTheMarkWhereItIs()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(3, 3, Item(Wood, 1))
                .WithOrders(1, new[] { Item(Wood, 1) }));

            screen.Spawn.PressSpawn();

            Assert.That(screen.OrderMatches.SentSets, Is.EqualTo(new[] { new[] { At(3, 3) } }));
        }

        [Test]
        public void SpawningAnUnneededItem_DoesNotResendTheSet()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(3, 3, Item(Stone, 1))
                .WithOrders(1, new[] { Item(Stone, 1) }));

            screen.Spawn.PressSpawn();

            Assert.That(screen.OrderMatches.SentSets.Count, Is.EqualTo(1));
        }

        [Test]
        public void MovedItem_IsReportedAtItsNewCell()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithOrders(1, new[] { Item(Wood, 1) }));

            screen.Board.DragAndDrop(0, 0, 3, 3);

            Assert.That(screen.OrderMatches.Marked, Is.EqualTo(new[] { At(3, 3) }));
        }

        [Test]
        public void MovingAnItemThatChangesNothing_DoesNotResendTheSet()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(4, 4, Item(Wood, 1))
                .WithItem(2, 2, Item(Stone, 1))
                .WithOrders(1, new[] { Item(Wood, 1) }));

            screen.Board.DragAndDrop(4, 4, 4, 3);
            screen.Board.DragAndDrop(2, 2, 2, 3);

            Assert.That(screen.OrderMatches.SentSets.Count, Is.EqualTo(1));
        }

        [Test]
        public void Merge_MarksANeededResult_AndUnmarksConsumedItems()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithOrders(1, new[] { Item(Wood, 1), Item(Wood, 2) }));
            Assert.That(screen.OrderMatches.Marked, Is.EqualTo(new[] { At(0, 0) }));

            screen.Board.DragAndDrop(0, 0, 1, 0);

            Assert.That(screen.OrderMatches.Marked, Is.EqualTo(new[] { At(1, 0) }));
        }

        [Test]
        public void DiscardingAMatchedItem_UnmarksIt()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(2, 2, Item(Stone, 3))
                .WithOrders(1, new[] { Item(Stone, 3) }));

            screen.Selection.Tap(2, 2);
            screen.Info.RequestDiscard();

            Assert.That(screen.OrderMatches.Marked, Is.Empty);
        }

        [Test]
        public void Delivery_UnmarksTheConsumedCells_AndMarksTheReplacementOrdersMatches()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(3, 3, Item(Stone, 1))
                .WithOrders(1, new[] { Item(Wood, 1) }, new[] { Item(Stone, 1) }));

            screen.Orders.RequestDelivery(0);

            Assert.That(screen.OrderMatches.SentSets, Is.EqualTo(new[] { new[] { At(0, 0) }, new[] { At(3, 3) } }));
        }

        [Test]
        public void GameWithoutOrders_SendsOneEmptySetAndNothingMore()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)));

            screen.Spawn.PressSpawn();
            screen.Board.DragAndDrop(0, 0, 1, 0);

            Assert.That(screen.OrderMatches.SentSets, Is.EqualTo(new[] { new GridPosition[0] }));
        }

        [Test]
        public void AfterDispose_NoFurtherSetIsSent()
        {
            var session = new GameBuilder().WithItem(3, 3, Item(Wood, 1)).WithOrders(1, new[] { Item(Wood, 1), Item(Wood, 1) }).Build();
            using var events = new SessionEventStream(session);
            var view = new FakeOrderMatchView();
            var presenter = new OrderMatchPresenter(session, events, view);
            presenter.Initialize();

            presenter.Dispose();
            session.Spawn();

            Assert.That(view.SentSets.Count, Is.EqualTo(1));
        }
    }
}
