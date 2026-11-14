using System;
using MergeGame.Core.Orders;
using MergeGame.Core.Spawning;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class RandomOrderGeneratorTests
    {
        [Test]
        public void Generate_MapsRollsOntoCountChainAndLevel()
        {
            var generator = new RandomOrderGenerator(OrderSetup(minItems: 1, maxItems: 3, minLevel: 1, maxLevel: 3), DefaultCatalog());

            // 1 + 1 items, (stone, 1 + 2), (wood, 1 + 0)
            var requirements = generator.Generate(new ScriptedRandom(1, 1, 2, 0, 0));

            Assert.That(requirements, Is.EqualTo(new[] { Item(Stone, 3), Item(Wood, 1) }));
        }

        [Test]
        public void Generate_StaysWithinTheSettings()
        {
            var settings = OrderSetup(minItems: 2, maxItems: 4, minLevel: 2, maxLevel: 4);
            var generator = new RandomOrderGenerator(settings, DefaultCatalog());
            var random = new SystemRandom(11);

            for (var i = 0; i < 200; i++)
            {
                var requirements = generator.Generate(random);
                Assert.That(requirements.Count, Is.InRange(2, 4));
                foreach (var item in requirements)
                    Assert.That(item.Level, Is.InRange(2, 4));
            }
        }

        [Test]
        public void Generate_OnlyRequestsSpawnableChains()
        {
            var catalog = Catalog(Chain(Wood, spawnWeight: 0), Chain(Stone, spawnWeight: 1));
            var generator = new RandomOrderGenerator(OrderSetup(), catalog);
            var random = new SystemRandom(3);

            for (var i = 0; i < 50; i++)
            {
                foreach (var item in generator.Generate(random))
                    Assert.That(item.ChainId, Is.EqualTo(Stone));
            }
        }

        [Test]
        public void Generate_CapsTheLevelAtTheChainsMaxLevel()
        {
            var catalog = Catalog(Chain(Wood, maxLevel: 2), Chain(Stone, maxLevel: 6));
            var generator = new RandomOrderGenerator(OrderSetup(minLevel: 1, maxLevel: 6), catalog);
            var random = new SystemRandom(5);
            var sawStoneAboveTwo = false;

            for (var i = 0; i < 200; i++)
            {
                foreach (var item in generator.Generate(random))
                {
                    if (item.ChainId == Wood)
                        Assert.That(item.Level, Is.LessThanOrEqualTo(2));
                    else if (item.Level > 2)
                        sawStoneAboveTwo = true;
                }
            }

            Assert.That(sawStoneAboveTwo, Is.True);
        }

        [Test]
        public void Generate_SkipsChainsShorterThanTheMinimumLevel()
        {
            var catalog = Catalog(Chain(Wood, maxLevel: 2), Chain(Stone, maxLevel: 5));
            var generator = new RandomOrderGenerator(OrderSetup(minLevel: 3, maxLevel: 5), catalog);
            var random = new SystemRandom(9);

            for (var i = 0; i < 50; i++)
            {
                foreach (var item in generator.Generate(random))
                    Assert.That(item.ChainId, Is.EqualTo(Stone));
            }
        }

        [Test]
        public void Constructor_WithoutEligibleChain_Throws()
        {
            var catalog = Catalog(Chain(Wood, spawnWeight: 0));

            Assert.Throws<ArgumentException>(() => new RandomOrderGenerator(OrderSetup(), catalog));
        }
    }
}
