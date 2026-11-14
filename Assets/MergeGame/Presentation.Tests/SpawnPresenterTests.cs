using MergeGame.Presentation.Notices;
using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;
using static MergeGame.Presentation.Tests.Support.TestContent;

namespace MergeGame.Presentation.Tests
{
    public sealed class SpawnPresenterTests
    {
        [Test]
        public void Initialize_MakesSpawnAvailable_WhenTheBoardHasRoom()
        {
            using var screen = GameScreen.For(new GameBuilder());

            Assert.That(screen.Spawn.IsAvailable, Is.True);
        }

        [Test]
        public void Initialize_MakesSpawnUnavailable_WhenTheBoardStartsFull()
        {
            using var screen = GameScreen.For(new GameBuilder().WithSize(2, 2).WithFullBoardOf(Item(Wood, 1)));

            Assert.That(screen.Spawn.IsAvailable, Is.False);
        }

        [Test]
        public void PressingSpawn_ShowsTheNewItemOnTheBoard()
        {
            using var screen = GameScreen.For(new GameBuilder());

            screen.Spawn.PressSpawn();

            Assert.That(screen.Board.Items, Is.EquivalentTo(screen.SessionItems()));
            Assert.That(screen.Board.Items.Count, Is.EqualTo(1));
        }

        [Test]
        public void SpawningIntoTheLastEmptyCell_MakesSpawnUnavailable()
        {
            using var screen = GameScreen.For(new GameBuilder().WithSize(2, 1).WithItem(0, 0, Item(Stone, 3)));

            screen.Spawn.PressSpawn();

            Assert.That(screen.Spawn.IsAvailable, Is.False);
            Assert.That(screen.Session.CanSpawn, Is.False);
        }

        [Test]
        public void PressingSpawn_OnAFullBoard_ShowsTheBoardFullNotice()
        {
            using var screen = GameScreen.For(new GameBuilder().WithSize(2, 1).WithFullBoardOf(Item(Wood, 1)));

            screen.Spawn.PressSpawn();

            Assert.That(screen.Notices.Notices, Is.EqualTo(new[] { Notice.BoardFull }));
            Assert.That(screen.Board.Items, Is.EquivalentTo(screen.SessionItems()));
        }

        [Test]
        public void MergingOnAFullBoard_MakesSpawnAvailableAgain()
        {
            using var screen = GameScreen.For(new GameBuilder().WithSize(2, 1).WithFullBoardOf(Item(Wood, 1)));

            screen.Board.DragAndDrop(0, 0, 1, 0);

            Assert.That(screen.Spawn.IsAvailable, Is.True);
        }

        [Test]
        public void DiscardingOnAFullBoard_MakesSpawnAvailableAgain()
        {
            using var screen = GameScreen.For(new GameBuilder().WithSize(2, 1).WithFullBoardOf(Item(Wood, 1)));

            screen.Selection.Tap(1, 0);
            screen.Info.RequestDiscard();

            Assert.That(screen.Spawn.IsAvailable, Is.True);
        }
    }
}
