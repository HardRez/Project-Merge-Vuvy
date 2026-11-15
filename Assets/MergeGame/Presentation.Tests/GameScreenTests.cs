using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;
using static MergeGame.Presentation.Tests.Support.TestContent;

namespace MergeGame.Presentation.Tests
{
    /// <summary>All game-screen presenters together: what every view sees, in which order.</summary>
    public sealed class GameScreenTests
    {
        [Test]
        public void Delivery_OnAFullBoard_ReachesTheViewsInTheSessionsEventOrder()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithSize(2, 1)
                .WithFullBoardOf(Item(Wood, 1))
                .WithOrders(1, new[] { Item(Wood, 1) }, new[] { Item(Stone, 1) }));
            screen.Selection.Tap(0, 0);
            screen.Log.Clear();

            screen.Orders.RequestDelivery(0);

            Assert.That(screen.Log.Entries, Is.EqualTo(new[]
            {
                "Board.DeliverItems to slot 0: (0, 0)",
                "Selection.HideSelectionMarker",
                "Info.ShowHint",
                "Orders.CompleteOrder slot 0 +5",
                "Score.ChangeScore 0 -> 5",
                "Spawn.SetSpawnAvailable True",
                "Orders.AddCard Slot 0: customer 1 wants stone:1 for 5"
            }));
        }

        [Test]
        public void Merge_ReachesTheViewsInTheSessionsEventOrder()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithOrders(1, new[] { Item(Wood, 2) }));
            screen.Board.StartDrag(0, 0);
            screen.Board.MoveFingerTo(1, 0);
            screen.Log.Clear();

            screen.Board.Release(1, 0);

            Assert.That(screen.Log.Entries, Is.EqualTo(new[]
            {
                "Board.ClearDropHighlight",
                "Board.MergeItems (0, 0) -> (1, 0) = wood:2",
                "Board.ShowPointsPopup +10 at (1, 0)",
                "Selection.ShowSelectionMarker (1, 0)",
                "Info.ShowItem wood:2 at (1, 0)",
                "Orders.RefreshCard Slot 0: customer 0 wants wood:2 (on board) for 15 (ready)",
                "Score.ChangeScore 0 -> 10"
            }));
        }

        [Test]
        public void BoardView_MirrorsTheSession_ThroughAPlaySessionOnANonSquareBoard()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithSize(4, 2)
                .WithItem(0, 0, Item(Wood, 1))
                .WithLockedItem(3, 1, Item(Wood, 2))
                .WithOrders(1, new[] { Item(Wood, 1) }));

            for (var i = 0; i < 4; i++)
                screen.Spawn.PressSpawn();
            screen.Board.DragAndDrop(0, 0, 1, 0);
            screen.Board.DragAndDrop(1, 0, 3, 1);
            screen.Board.DragAndDrop(2, 0, 2, 1);
            screen.Orders.RequestDelivery(0);
            screen.Selection.Tap(2, 1);
            screen.Info.RequestDiscard();

            Assert.That(screen.Board.Items, Is.EquivalentTo(screen.SessionItems()));
            Assert.That(screen.Score.Score, Is.EqualTo(screen.Session.Score));
            Assert.That(screen.Spawn.IsAvailable, Is.EqualTo(screen.Session.CanSpawn));
        }

        [Test]
        public void AfterDispose_NoViewIsCalled_AndNoViewEventIsListenedTo()
        {
            var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithOrders(1, new[] { Item(Wood, 2) }));
            var session = screen.Session;
            screen.Log.Clear();

            screen.Dispose();
            screen.Board.DragAndDrop(0, 0, 1, 0);
            screen.Spawn.PressSpawn();
            screen.Selection.Tap(0, 0);
            screen.Info.RequestDiscard();
            screen.Orders.RequestDelivery(0);
            screen.Menu.PressBackToMenu();
            session.Drop(At(0, 0), At(1, 0));
            session.Spawn();
            session.DeliverOrder(session.ActiveOrders[0].Id);

            Assert.That(screen.Log.Entries, Is.Empty);
            Assert.That(screen.Navigator.Requests, Is.Empty);
            Assert.That(screen.Board.HasInputListeners, Is.False);
            Assert.That(screen.Selection.HasInputListeners, Is.False);
            Assert.That(screen.Info.HasInputListeners, Is.False);
            Assert.That(screen.Spawn.HasInputListeners, Is.False);
            Assert.That(screen.Orders.HasInputListeners, Is.False);
            Assert.That(screen.Menu.HasInputListeners, Is.False);
        }

        [Test]
        public void BackToMenu_OpensTheMainMenu()
        {
            using var screen = GameScreen.For(new GameBuilder());

            screen.Menu.PressBackToMenu();

            Assert.That(screen.Navigator.Requests, Is.EqualTo(new[] { "OpenMainMenu" }));
        }
    }
}
