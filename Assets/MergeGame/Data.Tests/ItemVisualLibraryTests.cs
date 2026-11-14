using System;
using MergeGame.Core.Items;
using MergeGame.Data.Authoring;
using MergeGame.Data.Definitions;
using MergeGame.Data.Tests.Support;
using MergeGame.Data.Visuals;
using NUnit.Framework;
using Spine.Unity;
using static MergeGame.Data.Tests.Support.TestAssets;

namespace MergeGame.Data.Tests
{
    public sealed class ItemVisualLibraryTests
    {
        private TestAssets assets;

        [SetUp]
        public void SetUp() => assets = new TestAssets();

        [TearDown]
        public void TearDown() => assets.Dispose();

        [Test]
        public void GetIconAndDisplayName_ComeFromTheLevelEntry()
        {
            var config = assets.Config();
            IItemVisuals visuals = new ItemVisualLibrary(config);

            Assert.That(visuals.GetIcon(new ItemId(Stone, 3)), Is.SameAs(config.Chains[1].Levels[2].Icon));
            Assert.That(visuals.GetDisplayName(new ItemId(Stone, 3)), Is.EqualTo("stone 3"));
        }

        [Test]
        public void GetChainDisplayName_ComesFromTheChain()
        {
            var visuals = new ItemVisualLibrary(assets.Config());

            Assert.That(visuals.GetChainDisplayName(Wood), Is.EqualTo("wood chain"));
        }

        [Test]
        public void Customers_ComeFromTheOrderSettingsInOrder()
        {
            var config = assets.FullConfig();
            var portrait = assets.Create<SkeletonDataAsset>("Portrait");
            config.Orders.Customers = new[] { config.Orders.Customers[0], config.Orders.Customers[1], new CustomerEntry(portrait, null) };
            var visuals = new ItemVisualLibrary(config);

            Assert.That(visuals.CustomerCount, Is.EqualTo(3));
            Assert.That(visuals.GetCustomerAvatar(1), Is.SameAs(config.Orders.Customers[1].Avatar));
            Assert.That(visuals.GetCustomerPortrait(1), Is.Null);
            Assert.That(visuals.GetCustomerPortrait(2), Is.SameAs(portrait));
            Assert.That(visuals.GetCustomerAvatar(2), Is.Null);
        }

        [Test]
        public void WithoutOrders_ThereAreNoCustomers()
        {
            var visuals = new ItemVisualLibrary(assets.Config());

            Assert.That(visuals.CustomerCount, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => visuals.GetCustomerAvatar(0));
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void GetCustomerAvatar_OutsideTheCustomers_Throws(int customerIndex)
        {
            var visuals = new ItemVisualLibrary(assets.FullConfig());

            Assert.Throws<ArgumentOutOfRangeException>(() => visuals.GetCustomerAvatar(customerIndex));
            Assert.Throws<ArgumentOutOfRangeException>(() => visuals.GetCustomerPortrait(customerIndex));
        }

        [Test]
        public void UnknownChain_ThrowsNamingIt()
        {
            var visuals = new ItemVisualLibrary(assets.Config());

            var exception = Assert.Throws<ArgumentException>(() => visuals.GetIcon(new ItemId("gem", 1)));
            Assert.That(exception.Message, Does.Contain("'gem'"));
            Assert.Throws<ArgumentException>(() => visuals.GetChainDisplayName("gem"));
            Assert.Throws<ArgumentException>(() => visuals.GetChainDisplayName(null));
        }

        [Test]
        public void LevelBeyondTheChain_ThrowsNamingTheRange()
        {
            var visuals = new ItemVisualLibrary(assets.Config());

            var exception = Assert.Throws<ArgumentException>(() => visuals.GetDisplayName(new ItemId(Wood, 6)));
            Assert.That(exception.Message, Does.Contain("wood:6").And.Contain("1..5"));
        }

        [Test]
        public void Constructor_WithEmptyChainSlot_Throws()
        {
            var config = assets.Config();
            config.Chains = new[] { config.Chains[0], null };

            Assert.Throws<GameConfigException>(() => new ItemVisualLibrary(config));
        }

        [Test]
        public void Constructor_WithDuplicateChainIds_Throws()
        {
            var config = assets.Config(assets.Chain(Wood), assets.Chain(Wood));

            Assert.Throws<GameConfigException>(() => new ItemVisualLibrary(config));
        }

        [Test]
        public void Constructor_WithNullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ItemVisualLibrary(null));
        }
    }
}
