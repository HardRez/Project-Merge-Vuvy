using System;
using MergeGame.Core.Grid;
using MergeGame.Core.Spawning;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class WeightedRandomSpawnPolicyTests
    {
        [TestCase(0, Wood)]
        [TestCase(2, Wood)]
        [TestCase(3, Stone)]
        [TestCase(3 + 5, Flower)]
        public void Choose_MapsTheRollOntoChainsByWeight(int roll, string expectedChain)
        {
            var catalog = Catalog(Chain(Wood, spawnWeight: 3), Chain(Stone, spawnWeight: 5), Chain(Flower, spawnWeight: 2));
            var policy = new WeightedRandomSpawnPolicy(catalog);

            var placement = policy.Choose(new Board(new BoardSize(2, 2)), new ScriptedRandom(roll, 0));

            Assert.That(placement.ItemId, Is.EqualTo(Item(expectedChain, 1)));
        }

        [Test]
        public void Choose_PicksAmongEmptyCellsOnly()
        {
            var board = new Board(new BoardSize(2, 1));
            board.Place(new GridPosition(0, 0), new BoardItem(Item(Wood, 1)));
            var policy = new WeightedRandomSpawnPolicy(DefaultCatalog());

            var placement = policy.Choose(board, new ScriptedRandom(0, 0));

            Assert.That(placement.Position, Is.EqualTo(new GridPosition(1, 0)));
        }

        [Test]
        public void Constructor_WithoutAnySpawnableChain_Throws()
        {
            var catalog = Catalog(Chain(Wood, spawnWeight: 0));

            var exception = Assert.Throws<ArgumentException>(() => new WeightedRandomSpawnPolicy(catalog));
            Assert.That(exception.Message, Does.Contain("spawn weight"));
        }
    }
}
