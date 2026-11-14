using System;
using MergeGame.Core.Orders;
using MergeGame.Core.Scoring;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class OrderSettingsTests
    {
        [Test]
        public void Constructor_KeepsEveryValue()
        {
            var table = new ScoreTable(DefaultRewardPoints);

            var settings = new OrderSettings(3, 1, 4, 2, 5, 6, table);

            Assert.That(settings.MaxActiveOrders, Is.EqualTo(3));
            Assert.That(settings.MinItemsPerOrder, Is.EqualTo(1));
            Assert.That(settings.MaxItemsPerOrder, Is.EqualTo(4));
            Assert.That(settings.MinRequestedLevel, Is.EqualTo(2));
            Assert.That(settings.MaxRequestedLevel, Is.EqualTo(5));
            Assert.That(settings.CustomerCount, Is.EqualTo(6));
            Assert.That(settings.RewardPointsByLevel, Is.SameAs(table));
        }

        [TestCase(0, 1, 1, 1, 1, 1, "maxActiveOrders")]
        [TestCase(1, 0, 1, 1, 1, 1, "minItemsPerOrder")]
        [TestCase(1, 3, 2, 1, 1, 1, "maxItemsPerOrder")]
        [TestCase(1, 1, 1, 0, 1, 1, "minRequestedLevel")]
        [TestCase(1, 1, 1, 3, 2, 1, "maxRequestedLevel")]
        [TestCase(1, 1, 1, 1, 1, 0, "customerCount")]
        public void Constructor_WithInvalidValue_ThrowsNamingIt(
            int maxActive, int minItems, int maxItems, int minLevel, int maxLevel, int customers, string expectedParameter)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                new OrderSettings(maxActive, minItems, maxItems, minLevel, maxLevel, customers, new ScoreTable(DefaultRewardPoints)));
            Assert.That(exception.ParamName, Is.EqualTo(expectedParameter));
        }

        [Test]
        public void Constructor_WithoutRewardTable_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new OrderSettings(1, 1, 1, 1, 1, 1, null));
        }

        [Test]
        public void Definition_WithRewardTableShorterThanTheRequestableLevels_Throws()
        {
            var settings = OrderSetup(maxLevel: 4, rewardPoints: new[] { 5, 10, 20 });

            var exception = Assert.Throws<ArgumentException>(() => Definition(orderSettings: settings));
            Assert.That(exception.Message, Does.Contain("up to level 3").And.Contain("request level 4"));
        }

        [Test]
        public void Definition_RewardTableOnlyNeedsToCoverLevelsTheCatalogCanReach()
        {
            var catalog = Catalog(Chain(Wood, maxLevel: 2));
            var settings = OrderSetup(minLevel: 1, maxLevel: 9, rewardPoints: new[] { 5, 10 });

            Assert.DoesNotThrow(() => Definition(catalog: catalog, orderSettings: settings));
        }

        [Test]
        public void Definition_WithMinRequestedLevelBeyondEveryChain_Throws()
        {
            var settings = OrderSetup(minLevel: ChainLength + 1, maxLevel: ChainLength + 1, rewardPoints: new[] { 1, 2, 3, 4, 5, 6 });

            var exception = Assert.Throws<ArgumentException>(() => Definition(orderSettings: settings));
            Assert.That(exception.Message, Does.Contain("level 6"));
        }

        [Test]
        public void Definition_WithOrderGeneratorButNoSettings_Throws()
        {
            var exception = Assert.Throws<ArgumentException>(() => Definition(orderGenerator: new ScriptedOrderGenerator()));
            Assert.That(exception.ParamName, Is.EqualTo("orderGenerator"));
        }

        [Test]
        public void Definition_WithSettingsButNoGenerator_UsesTheRandomOrderGenerator()
        {
            var definition = Definition(orderSettings: OrderSetup());

            Assert.That(definition.HasOrders, Is.True);
            Assert.That(definition.OrderGenerator, Is.InstanceOf<RandomOrderGenerator>());
        }

        [Test]
        public void Definition_WithoutSettings_HasNoOrders()
        {
            var definition = Definition();

            Assert.That(definition.HasOrders, Is.False);
            Assert.That(definition.OrderSettings, Is.Null);
            Assert.That(definition.OrderGenerator, Is.Null);
        }

        [Test]
        public void Definition_WhereOnlyUnspawnableChainsReachTheMinimumLevel_Throws()
        {
            var catalog = Catalog(Chain(Wood, maxLevel: 2), Chain(Stone, maxLevel: 5, spawnWeight: 0));
            var settings = OrderSetup(minLevel: 3, maxLevel: 5);

            var exception = Assert.Throws<ArgumentException>(() => Definition(catalog: catalog, orderSettings: settings));
            Assert.That(exception.Message, Does.Contain("spawnable chain").And.Contain("level 3"));
        }

        [Test]
        public void Order_WithoutRequirements_Throws()
        {
            Assert.Throws<ArgumentException>(() => new Order(1, 0, new Items.ItemId[0], 10));
        }

        [Test]
        public void Order_CopiesItsRequirements()
        {
            var requirements = new[] { Item(Wood, 1) };
            var order = new Order(1, 0, requirements, 10);

            requirements[0] = Item(Stone, 2);

            Assert.That(order.Requirements[0], Is.EqualTo(Item(Wood, 1)));
        }
    }
}
