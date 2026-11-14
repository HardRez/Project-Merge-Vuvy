using System;
using MergeGame.Core.Grid;
using MergeGame.Core.Session;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class InitialLayoutTests
    {
        [Test]
        public void Session_StartsWithTheDefinitionsInitialPlacements()
        {
            var definition = Definition(initialPlacements: new[]
            {
                new InitialPlacement(new GridPosition(0, 0), Item(Wood, 2)),
                new InitialPlacement(new GridPosition(4, 3), Item(Flower, 4), isLocked: true)
            });

            var session = new MergeGameSession(definition, new ScriptedRandom());

            Assert.That(session.Board.GetItem(new GridPosition(0, 0)).ItemId, Is.EqualTo(Item(Wood, 2)));
            Assert.That(session.Board.GetItem(new GridPosition(0, 0)).IsLocked, Is.False);
            Assert.That(session.Board.GetItem(new GridPosition(4, 3)).ItemId, Is.EqualTo(Item(Flower, 4)));
            Assert.That(session.Board.GetItem(new GridPosition(4, 3)).IsLocked, Is.True);
            Assert.That(session.Board.EmptyCellCount, Is.EqualTo(23));
            Assert.That(session.Score, Is.Zero);
        }

        [Test]
        public void Definition_WithoutInitialPlacements_StartsEmpty()
        {
            var definition = Definition();

            Assert.That(definition.InitialPlacements, Is.Empty);
            Assert.That(new MergeGameSession(definition, new ScriptedRandom()).Board.EmptyCellCount, Is.EqualTo(25));
        }

        [Test]
        public void InitialPlacements_FillingTheBoard_MakeSpawningUnavailable()
        {
            var definition = Definition(1, 2, initialPlacements: new[]
            {
                new InitialPlacement(new GridPosition(0, 0), Item(Wood, 1)),
                new InitialPlacement(new GridPosition(0, 1), Item(Stone, 1), isLocked: true)
            });

            var session = new MergeGameSession(definition, new ScriptedRandom());

            Assert.That(session.Board.IsFull, Is.True);
            Assert.That(session.CanSpawn, Is.False);
        }

        [Test]
        public void InitialPlacements_AreCopiedFromTheSource()
        {
            var source = new[] { new InitialPlacement(new GridPosition(0, 0), Item(Wood, 1)) };
            var definition = Definition(initialPlacements: source);

            source[0] = new InitialPlacement(new GridPosition(1, 1), Item(Stone, 1));

            Assert.That(definition.InitialPlacements[0].Position, Is.EqualTo(new GridPosition(0, 0)));
        }

        [TestCase(5, 0)]
        [TestCase(0, -1)]
        public void InitialPlacement_OutsideTheBoard_Throws(int x, int y)
        {
            var placements = new[] { new InitialPlacement(new GridPosition(x, y), Item(Wood, 1)) };

            var exception = Assert.Throws<ArgumentException>(() => Definition(initialPlacements: placements));
            Assert.That(exception.Message, Does.Contain("outside the 5x5 board"));
            Assert.That(exception.ParamName, Is.EqualTo("initialPlacements"));
        }

        [Test]
        public void InitialPlacements_SharingACell_Throw()
        {
            var placements = new[]
            {
                new InitialPlacement(new GridPosition(2, 2), Item(Wood, 1)),
                new InitialPlacement(new GridPosition(0, 0), Item(Wood, 1)),
                new InitialPlacement(new GridPosition(2, 2), Item(Stone, 3), isLocked: true)
            };

            var exception = Assert.Throws<ArgumentException>(() => Definition(initialPlacements: placements));
            Assert.That(exception.Message, Does.Contain("0 and 2").And.Contain("(2, 2)"));
        }

        [TestCase("unknown", 1)]
        [TestCase(Wood, ChainLength + 1)]
        public void InitialPlacement_OfAnItemMissingFromTheCatalog_Throws(string chainId, int level)
        {
            var placements = new[] { new InitialPlacement(new GridPosition(0, 0), Item(chainId, level)) };

            var exception = Assert.Throws<ArgumentException>(() => Definition(initialPlacements: placements));
            Assert.That(exception.Message, Does.Contain("not in the item catalog"));
        }

        [Test]
        public void InitialPlacement_WithUnsetItem_Throws()
        {
            var placements = new[] { new InitialPlacement(new GridPosition(0, 0), default) };

            Assert.Throws<ArgumentException>(() => Definition(initialPlacements: placements));
        }
    }
}
