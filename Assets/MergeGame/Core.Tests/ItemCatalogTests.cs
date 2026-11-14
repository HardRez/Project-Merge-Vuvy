using System;
using MergeGame.Core.Items;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class ItemCatalogTests
    {
        [Test]
        public void Constructor_WithDuplicateChainIds_Throws()
        {
            var exception = Assert.Throws<ArgumentException>(() => Catalog(Chain(Wood), Chain(Wood)));
            Assert.That(exception.Message, Does.Contain("'wood'"));
        }

        [Test]
        public void Constructor_WithoutChains_Throws()
        {
            Assert.Throws<ArgumentException>(() => Catalog());
        }

        [Test]
        public void ChainDefinition_WithoutLevels_Throws()
        {
            var exception = Assert.Throws<ArgumentException>(() => new ItemChainDefinition(Wood, new ItemLevelDefinition[0], 1, 1f));
            Assert.That(exception.Message, Does.Contain("at least one level"));
        }

        [Test]
        public void ChainDefinition_WithGapInLevels_Throws()
        {
            var levels = new[] { new ItemLevelDefinition(1, "Twig"), new ItemLevelDefinition(3, "Log") };

            var exception = Assert.Throws<ArgumentException>(() => new ItemChainDefinition(Wood, levels, 1, 1f));
            Assert.That(exception.Message, Does.Contain("contiguous"));
        }

        [Test]
        public void ChainDefinition_WithDuplicateLevel_Throws()
        {
            var levels = new[] { new ItemLevelDefinition(1, "Twig"), new ItemLevelDefinition(1, "Stick") };

            Assert.Throws<ArgumentException>(() => new ItemChainDefinition(Wood, levels, 1, 1f));
        }

        [Test]
        public void ChainDefinition_WithNullLevel_ThrowsNamingTheLevelsParameter()
        {
            var levels = new[] { new ItemLevelDefinition(1, "Twig"), null };

            var exception = Assert.Throws<ArgumentException>(() => new ItemChainDefinition(Wood, levels, 1, 1f));
            Assert.That(exception.ParamName, Is.EqualTo("levels"));
        }

        [Test]
        public void ChainDefinition_WithLevelsOutOfOrder_SortsThem()
        {
            var levels = new[] { new ItemLevelDefinition(2, "Stick"), new ItemLevelDefinition(1, "Twig") };

            var chain = new ItemChainDefinition(Wood, levels, 1, 1f);

            Assert.That(chain.GetLevel(1).DisplayName, Is.EqualTo("Twig"));
            Assert.That(chain.MaxLevel, Is.EqualTo(2));
        }

        [Test]
        public void ChainDefinition_WithNegativeSpawnWeight_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Chain(Wood, spawnWeight: -1));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void ChainDefinition_WithNonPositiveScoreMultiplier_Throws(float multiplier)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Chain(Wood, scoreMultiplier: multiplier));
        }

        [Test]
        public void IsMaxLevel_IsTrueOnlyForTheLastLevelOfTheChain()
        {
            var catalog = Catalog(Chain(Wood, maxLevel: 3));

            Assert.That(catalog.IsMaxLevel(Item(Wood, 2)), Is.False);
            Assert.That(catalog.IsMaxLevel(Item(Wood, 3)), Is.True);
        }

        [Test]
        public void TryGetNext_BelowMaxLevel_ReturnsTheNextLevelOfTheSameChain()
        {
            var catalog = DefaultCatalog();

            Assert.That(catalog.TryGetNext(Item(Stone, 2), out var next), Is.True);
            Assert.That(next, Is.EqualTo(Item(Stone, 3)));
        }

        [Test]
        public void TryGetNext_AtMaxLevel_ReturnsFalse()
        {
            var catalog = DefaultCatalog();

            Assert.That(catalog.TryGetNext(Item(Stone, ChainLength), out _), Is.False);
        }

        [Test]
        public void IsMaxLevel_ForUnknownItem_Throws()
        {
            var catalog = DefaultCatalog();

            Assert.Throws<ArgumentException>(() => catalog.IsMaxLevel(Item("unknown", 1)));
        }

        [Test]
        public void HighestLevel_IsTheLongestChainLength()
        {
            var catalog = Catalog(Chain(Wood, maxLevel: 4), Chain(Stone, maxLevel: 7));

            Assert.That(catalog.HighestLevel, Is.EqualTo(7));
        }

        [TestCase(null, 1)]
        [TestCase("", 1)]
        [TestCase(Wood, 0)]
        public void ItemId_WithInvalidParts_Throws(string chainId, int level)
        {
            Assert.That(() => new ItemId(chainId, level), Throws.InstanceOf<ArgumentException>());
        }
    }
}
