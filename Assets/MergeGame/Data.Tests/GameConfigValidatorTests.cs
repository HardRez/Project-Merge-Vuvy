using System.Linq;
using MergeGame.Data.Authoring;
using MergeGame.Data.Tests.Support;
using MergeGame.Data.Validation;
using NUnit.Framework;
using static MergeGame.Data.Tests.Support.TestAssets;

namespace MergeGame.Data.Tests
{
    public sealed class GameConfigValidatorTests
    {
        private TestAssets assets;

        [SetUp]
        public void SetUp() => assets = new TestAssets();

        [TearDown]
        public void TearDown() => assets.Dispose();

        [Test]
        public void HealthyConfig_ReportsNothing()
        {
            Assert.That(GameConfigValidator.Validate(assets.FullConfig()), Is.Empty);
        }

        [Test]
        public void NullConfig_IsReported()
        {
            AssertReports(null, "No game config");
        }

        [Test]
        public void MissingLevelIcon_IsReported()
        {
            var config = assets.FullConfig();
            config.Chains[1].Levels = config.Chains[1].Levels.Take(3).Append(new ItemLevelEntry("stone 4", null)).ToList();

            AssertReports(config, "Chain 'stone' level 4 has no icon sprite");
        }

        [Test]
        public void MissingCustomerAvatar_IsReported()
        {
            var config = assets.FullConfig();
            config.Orders.CustomerAvatars = new[] { config.Orders.CustomerAvatars[0], null };

            AssertReports(config, "customer 1 has no avatar sprite");
        }

        [Test]
        public void OrdersWithoutAnyCustomer_AreReported()
        {
            var config = assets.FullConfig();
            config.Orders.CustomerAvatars = new UnityEngine.Sprite[0];

            AssertReports(config, "no customer avatars");
        }

        [Test]
        public void EmptyChainSlot_IsReported()
        {
            var config = assets.FullConfig();
            config.Chains = config.Chains.Append(null).ToList();

            AssertReports(config, "Chain slot 2 is empty");
        }

        [Test]
        public void NoChains_AreReported()
        {
            var config = assets.Config();
            config.Chains = new ItemChainAsset[0];

            AssertReports(config, "lists no item chains");
        }

        [Test]
        public void DuplicateChainIds_AreReported()
        {
            var config = assets.Config(assets.Chain(Wood), assets.Chain(Stone), assets.Chain(Wood));

            AssertReports(config, "Chain slots 0 and 2 both use the chain id 'wood'");
        }

        [Test]
        public void ChainWithoutLevels_IsReported()
        {
            var config = assets.Config();
            config.Chains[0].Levels = new ItemLevelEntry[0];

            AssertReports(config, "Chain 'wood' has no levels");
        }

        [Test]
        public void ChainWithoutId_IsReportedByAssetName()
        {
            var config = assets.Config();
            config.Chains[0].Id = "";

            AssertReports(config, "Chain asset 'wood' has no id");
        }

        [Test]
        public void LevelWithoutDisplayName_IsReported()
        {
            var config = assets.Config();
            config.Chains[0].Levels = config.Chains[0].Levels.Take(1).Append(new ItemLevelEntry("", assets.Sprite("icon"))).ToList();

            AssertReports(config, "Chain 'wood' level 2 has no display name");
        }

        [Test]
        public void NonPositiveScoreMultiplier_IsReported()
        {
            var config = assets.Config();
            config.Chains[0].ScoreMultiplier = 0f;

            AssertReports(config, "Chain 'wood' needs a positive score multiplier");
        }

        [Test]
        public void InvalidBoardSize_IsReported()
        {
            var config = assets.Config();
            config.BoardHeight = 0;

            AssertReports(config, "Board height must be at least 1");
        }

        [Test]
        public void MissingScoreTable_IsReported()
        {
            var config = assets.Config();
            config.ScoreTable = null;

            AssertReports(config, "No score table");
        }

        [Test]
        public void LayoutOutsideTheBoard_IsReported()
        {
            var config = assets.Config();
            config.InitialLayout = assets.Layout(new InitialPlacementEntry(1, 5, config.Chains[0], 1, locked: true));

            AssertReports(config, "placement 0 at (1, 5) is outside the 5x5 board");
        }

        [Test]
        public void LayoutCellUsedTwice_IsReported()
        {
            var config = assets.Config();
            config.InitialLayout = assets.Layout(
                new InitialPlacementEntry(2, 2, config.Chains[0], 1, locked: true),
                new InitialPlacementEntry(2, 2, config.Chains[1], 1, locked: false));

            AssertReports(config, "placement 1 at (2, 2) uses the same cell as placement 0");
        }

        [Test]
        public void LayoutWithChainNotInTheConfig_IsReported()
        {
            var config = assets.Config();
            config.InitialLayout = assets.Layout(new InitialPlacementEntry(0, 0, assets.Chain("stray"), 1, locked: false));

            AssertReports(config, "uses Chain 'stray', which is not in the config's chain list");
        }

        [Test]
        public void LayoutLevelBeyondTheChain_IsReported()
        {
            var config = assets.Config();
            config.InitialLayout = assets.Layout(new InitialPlacementEntry(0, 0, config.Chains[0], 6, locked: false));

            AssertReports(config, "asks for level 6, but Chain 'wood' has levels 1..5");
        }

        [Test]
        public void LayoutPlacementWithoutChain_IsReported()
        {
            var config = assets.Config();
            config.InitialLayout = assets.Layout(new InitialPlacementEntry(0, 0, null, 1, locked: false));

            AssertReports(config, "placement 0 at (0, 0) has no chain");
        }

        [Test]
        public void ProblemsOnlyCoreDetects_AreReported()
        {
            var config = assets.Config();
            config.ScoreTable = assets.ScoreTable(0, 10, 20);

            AssertReports(config, "reaches level 5");
        }

        [Test]
        public void ConfigWithoutSpawnableChains_IsReported()
        {
            var config = assets.Config(assets.Chain(Wood, spawnWeight: 0), assets.Chain(Stone, spawnWeight: 0));

            AssertReports(config, "spawnable");
        }

        [Test]
        public void ValidateChain_ReportsOnlyThatChainsProblems()
        {
            var chain = assets.Chain(Wood);
            Assert.That(GameConfigValidator.ValidateChain(chain), Is.Empty);

            chain.Levels = chain.Levels.Take(1).Append(new ItemLevelEntry("wood 2", null)).ToList();

            Assert.That(GameConfigValidator.ValidateChain(chain), Is.EqualTo(new[] { "Chain 'wood' level 2 has no icon sprite." }));
        }

        private static void AssertReports(GameConfigAsset config, string expectedFragment)
        {
            var problems = GameConfigValidator.Validate(config);
            Assert.That(problems, Has.Some.Contains(expectedFragment), $"Reported: {string.Join(" | ", problems)}");
        }
    }
}
