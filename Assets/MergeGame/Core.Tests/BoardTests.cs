using MergeGame.Core.Grid;
using MergeGame.Core.Session;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class BoardTests
    {
        [TestCase(5, 5)]
        [TestCase(3, 7)]
        public void Session_BoardSize_ComesFromTheDefinition(int width, int height)
        {
            var session = new MergeGameSession(Definition(width, height), new ScriptedRandom());

            Assert.That(session.Board.Size, Is.EqualTo(new BoardSize(width, height)));
            Assert.That(session.Board.Positions.Count, Is.EqualTo(width * height));
            Assert.That(session.Board.EmptyCellCount, Is.EqualTo(width * height));
        }

        [Test]
        public void NewBoard_HasOnlyEmptyCells()
        {
            var board = new Board(new BoardSize(3, 2));

            foreach (var position in board.Positions)
                Assert.That(board.IsEmpty(position), Is.True, position.ToString());
        }

        [Test]
        public void Place_MakesTheCellOccupiedByThatItem()
        {
            var board = new Board(new BoardSize(3, 3));
            var item = new BoardItem(Item(Wood, 2));

            board.Place(new GridPosition(1, 2), item);

            Assert.That(board.IsEmpty(new GridPosition(1, 2)), Is.False);
            Assert.That(board.GetItem(new GridPosition(1, 2)), Is.SameAs(item));
            Assert.That(board.EmptyCellCount, Is.EqualTo(8));
        }

        [Test]
        public void Place_OnOccupiedCell_Throws()
        {
            var board = new Board(new BoardSize(2, 2));
            board.Place(new GridPosition(0, 0), new BoardItem(Item(Wood, 1)));

            Assert.Throws<System.InvalidOperationException>(() => board.Place(new GridPosition(0, 0), new BoardItem(Item(Stone, 1))));
        }

        [Test]
        public void TryGetItem_OnEmptyCell_ReturnsFalse()
        {
            var board = new Board(new BoardSize(2, 2));

            Assert.That(board.TryGetItem(new GridPosition(1, 1), out var item), Is.False);
            Assert.That(item, Is.Null);
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(5, 0)]
        [TestCase(0, 5)]
        public void IsInside_OutsideTheBoard_IsFalse(int x, int y)
        {
            var board = new Board(new BoardSize(5, 5));

            Assert.That(board.IsInside(new GridPosition(x, y)), Is.False);
        }

        [Test]
        public void IsFull_WhenEveryCellIsOccupied_IsTrue()
        {
            var session = new SessionBuilder().WithFullBoardOf(Item(Wood, 1)).Build();

            Assert.That(session.Board.IsFull, Is.True);
            Assert.That(session.Board.EmptyCellCount, Is.Zero);
        }
    }
}
