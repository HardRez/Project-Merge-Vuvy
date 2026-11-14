using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;
using static MergeGame.Presentation.Tests.Support.TestContent;

namespace MergeGame.Presentation.Tests
{
    public sealed class ScorePresenterTests
    {
        [Test]
        public void Initialize_ShowsTheStartingScore()
        {
            using var screen = GameScreen.For(new GameBuilder());

            Assert.That(screen.Score.Score, Is.Zero);
            Assert.That(screen.Score.Changes, Is.Empty);
        }

        [Test]
        public void Merges_ChangeTheScoreFromThePreviousToTheNewValue()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithItem(2, 0, Item(Wood, 2)));

            screen.Board.DragAndDrop(0, 0, 1, 0);
            screen.Board.DragAndDrop(1, 0, 2, 0);

            Assert.That(screen.Score.Changes, Is.EqualTo(new[] { (0, 10), (10, 30) }));
            Assert.That(screen.Score.Score, Is.EqualTo(screen.Session.Score));
        }

        [Test]
        public void Delivery_ChangesTheScoreByTheReward()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 2))
                .WithOrders(1, new[] { Item(Wood, 2) }));

            screen.Orders.RequestDelivery(0);

            Assert.That(screen.Score.Changes, Is.EqualTo(new[] { (0, 15) }));
        }
    }
}
