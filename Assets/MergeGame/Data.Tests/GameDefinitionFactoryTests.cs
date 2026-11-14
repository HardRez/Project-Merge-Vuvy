using System;
using System.Linq;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Data.Authoring;
using MergeGame.Data.Definitions;
using MergeGame.Data.Tests.Support;
using NUnit.Framework;
using static MergeGame.Data.Tests.Support.TestAssets;

namespace MergeGame.Data.Tests
{
    public sealed class GameDefinitionFactoryTests
    {
        private TestAssets assets;
        private GameDefinitionFactory factory;

        [SetUp]
        public void SetUp()
        {
            assets = new TestAssets();
            factory = new GameDefinitionFactory();
        }

        [TearDown]
        public void TearDown() => assets.Dispose();

        [Test]
        public void Create_MapsBoardSize()
        {
            var config = assets.Config();
            config.BoardWidth = 7;
            config.BoardHeight = 4;

            var definition = factory.Create(config);

            Assert.That(definition.BoardSize, Is.EqualTo(new BoardSize(7, 4)));
        }

        [Test]
        public void Create_MapsChainsInOrderWithWeightsMultipliersAndLevels()
        {
            var config = assets.Config(
                assets.Chain(Wood, maxLevel: 3, spawnWeight: 4, scoreMultiplier: 1.5f),
                assets.Chain(Stone, maxLevel: 5, spawnWeight: 0, scoreMultiplier: 2f),
                assets.Chain("gem", maxLevel: 2, spawnWeight: 1, scoreMultiplier: 0.5f));

            var chains = factory.Create(config).Catalog.Chains;

            Assert.That(chains.Select(chain => chain.Id), Is.EqualTo(new[] { Wood, Stone, "gem" }));
            Assert.That(chains.Select(chain => chain.SpawnWeight), Is.EqualTo(new[] { 4, 0, 1 }));
            Assert.That(chains.Select(chain => chain.ScoreMultiplier), Is.EqualTo(new[] { 1.5f, 2f, 0.5f }));
            Assert.That(chains.Select(chain => chain.MaxLevel), Is.EqualTo(new[] { 3, 5, 2 }));
            Assert.That(chains[0].Levels.Select(level => level.DisplayName), Is.EqualTo(new[] { "wood 1", "wood 2", "wood 3" }));
            Assert.That(chains[0].Levels.Select(level => level.Level), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Create_MapsScoreTable()
        {
            var config = assets.Config();
            config.ScoreTable = assets.ScoreTable(1, 2, 3, 50, 70, 900);

            var scoreTable = factory.Create(config).ScoreTable;

            Assert.That(scoreTable.HighestLevel, Is.EqualTo(6));
            Assert.That(Enumerable.Range(1, 6).Select(scoreTable.GetPoints), Is.EqualTo(new[] { 1, 2, 3, 50, 70, 900 }));
        }

        [Test]
        public void Create_MapsOrderSettings()
        {
            var config = assets.Config();
            var orders = assets.Orders(customerCount: 4);
            orders.MaxActiveOrders = 3;
            orders.MinItemsPerOrder = 2;
            orders.MaxItemsPerOrder = 3;
            orders.MinRequestedLevel = 1;
            orders.MaxRequestedLevel = 4;
            orders.RewardPointsByLevel = new[] { 1, 2, 3, 4 };
            config.Orders = orders;

            var settings = factory.Create(config).OrderSettings;

            Assert.That(settings.MaxActiveOrders, Is.EqualTo(3));
            Assert.That(settings.MinItemsPerOrder, Is.EqualTo(2));
            Assert.That(settings.MaxItemsPerOrder, Is.EqualTo(3));
            Assert.That(settings.MinRequestedLevel, Is.EqualTo(1));
            Assert.That(settings.MaxRequestedLevel, Is.EqualTo(4));
            Assert.That(settings.CustomerCount, Is.EqualTo(4));
            Assert.That(Enumerable.Range(1, 4).Select(settings.RewardPointsByLevel.GetPoints), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void Create_MapsInitialLayoutIncludingLocks()
        {
            var config = assets.Config();
            config.InitialLayout = assets.Layout(
                new InitialPlacementEntry(4, 0, config.Chains[0], 3, locked: true),
                new InitialPlacementEntry(0, 4, config.Chains[1], 1, locked: false));

            var placements = factory.Create(config).InitialPlacements;

            Assert.That(placements.Count, Is.EqualTo(2));
            Assert.That(placements[0].Position, Is.EqualTo(new GridPosition(4, 0)));
            Assert.That(placements[0].ItemId, Is.EqualTo(new ItemId(Wood, 3)));
            Assert.That(placements[0].IsLocked, Is.True);
            Assert.That(placements[1].Position, Is.EqualTo(new GridPosition(0, 4)));
            Assert.That(placements[1].ItemId, Is.EqualTo(new ItemId(Stone, 1)));
            Assert.That(placements[1].IsLocked, Is.False);
        }

        [Test]
        public void Create_WithoutOrdersOrLayout_GivesAGameWithoutThem()
        {
            var definition = factory.Create(assets.Config());

            Assert.That(definition.HasOrders, Is.False);
            Assert.That(definition.OrderGenerator, Is.Null);
            Assert.That(definition.InitialPlacements, Is.Empty);
        }

        [Test]
        public void Create_WithNullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => factory.Create(null));
        }

        [Test]
        public void Create_WithEmptyChainSlot_NamesConfigAndSlot()
        {
            var config = assets.Config();
            config.Chains = new[] { config.Chains[0], null };

            var exception = Assert.Throws<GameConfigException>(() => factory.Create(config));

            Assert.That(exception.Message, Does.Contain("TestConfig").And.Contain("chain slot 1"));
        }

        [Test]
        public void Create_WithEmptyLevelEntry_NamesChainAndLevel()
        {
            var config = assets.Config();
            config.Chains[0].Levels = config.Chains[0].Levels.Take(2).Append(null).ToList();

            var exception = Assert.Throws<GameConfigException>(() => factory.Create(config));

            Assert.That(exception.Message, Does.Contain("Chain 'wood' level 3"));
        }

        [Test]
        public void Create_WithoutScoreTable_SaysSo()
        {
            var config = assets.Config();
            config.ScoreTable = null;

            var exception = Assert.Throws<GameConfigException>(() => factory.Create(config));

            Assert.That(exception.Message, Does.Contain("no score table"));
        }

        [Test]
        public void Create_WhenCoreRejectsAChain_AddsChainContext()
        {
            var config = assets.Config();
            config.Chains[1].Levels = new[] { new ItemLevelEntry("fine", null), new ItemLevelEntry(" ", null) };

            var exception = Assert.Throws<GameConfigException>(() => factory.Create(config));

            Assert.That(exception.Message, Does.Contain("TestConfig").And.Contain("Chain 'stone'").And.Contain("Level 2 needs a display name"));
            Assert.That(exception.InnerException, Is.InstanceOf<ArgumentException>());
        }

        [Test]
        public void Create_WhenScoreTableIsShorterThanTheLongestChain_ReportsCoreMessage()
        {
            var config = assets.Config();
            config.ScoreTable = assets.ScoreTable(0, 10, 20);

            var exception = Assert.Throws<GameConfigException>(() => factory.Create(config));

            Assert.That(exception.Message, Does.Contain("TestConfig").And.Contain("reaches level 5"));
        }

        [Test]
        public void Create_WhenOrderSettingsAreInvalid_NamesTheOrderAsset()
        {
            var config = assets.Config();
            config.Orders = assets.Orders();
            config.Orders.MaxItemsPerOrder = 0;

            var exception = Assert.Throws<GameConfigException>(() => factory.Create(config));

            Assert.That(exception.Message, Does.Contain("order settings 'TestOrders'"));
        }

        [Test]
        public void Create_WithLayoutPlacementWithoutChain_NamesThePlacement()
        {
            var config = assets.Config();
            config.InitialLayout = assets.Layout(new InitialPlacementEntry(2, 3, null, 1, locked: false));

            var exception = Assert.Throws<GameConfigException>(() => factory.Create(config));

            Assert.That(exception.Message, Does.Contain("placement 0 at (2, 3) has no chain"));
        }

        [Test]
        public void Create_WithLayoutChainNotInTheConfig_SaysSo()
        {
            var config = assets.Config();
            config.InitialLayout = assets.Layout(new InitialPlacementEntry(0, 0, assets.Chain("stray"), 1, locked: false));

            var exception = Assert.Throws<GameConfigException>(() => factory.Create(config));

            Assert.That(exception.Message, Does.Contain("Chain 'stray'").And.Contain("not in the config's chain list"));
        }

        [Test]
        public void Create_WithLayoutOutsideTheBoard_ReportsCoreMessage()
        {
            var config = assets.Config();
            config.BoardWidth = 3;
            config.InitialLayout = assets.Layout(new InitialPlacementEntry(3, 0, config.Chains[0], 1, locked: true));

            var exception = Assert.Throws<GameConfigException>(() => factory.Create(config));

            Assert.That(exception.Message, Does.Contain("outside the 3x5 board"));
        }
    }
}
