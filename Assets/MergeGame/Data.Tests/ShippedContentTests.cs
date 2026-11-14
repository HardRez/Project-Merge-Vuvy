using System;
using System.Linq;
using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Rules;
using MergeGame.Core.Session;
using MergeGame.Core.Spawning;
using MergeGame.Data.Authoring;
using MergeGame.Data.Definitions;
using MergeGame.Data.Tests.Support;
using MergeGame.Data.Validation;
using MergeGame.Data.Visuals;
using NUnit.Framework;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MergeGame.Data.Tests
{
    /// <summary>Checks the generated assets under Content/Config; regenerate them with Merge Game/Seed Content.</summary>
    public sealed class ShippedContentTests
    {
        private const string GameConfigPath = "Assets/MergeGame/Content/Config/GameConfig.asset";

        private GameConfigAsset config;
        private TestAssets assets;

        [SetUp]
        public void SetUp()
        {
            config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath);
            Assert.That(config, Is.Not.Null, $"{GameConfigPath} is missing; run Merge Game/Seed Content.");
            assets = new TestAssets();
        }

        [TearDown]
        public void TearDown() => assets.Dispose();

        [Test]
        public void ValidatesWithoutProblems()
        {
            Assert.That(GameConfigValidator.Validate(config), Is.Empty);
        }

        [Test]
        public void BuildsAPlayableSessionWithTheOpeningLayoutAndOrders()
        {
            var definition = new GameDefinitionFactory().Create(config);
            var session = new MergeGameSession(definition, new SystemRandom(1));

            Assert.That(session.Board.Size.CellCount - session.Board.EmptyCellCount, Is.EqualTo(definition.InitialPlacements.Count));
            Assert.That(session.ActiveOrders.Count, Is.EqualTo(definition.OrderSettings.MaxActiveOrders));
        }

        [Test]
        public void BoardIsFiveByFive()
        {
            Assert.That(new BoardSize(config.BoardWidth, config.BoardHeight), Is.EqualTo(new BoardSize(5, 5)));
        }

        [Test]
        public void HasAtLeastThreeChainsOfAtLeastFiveLevelsWithIcons()
        {
            Assert.That(config.Chains.Count, Is.GreaterThanOrEqualTo(3));
            foreach (var chain in config.Chains)
            {
                Assert.That(chain.MaxLevel, Is.GreaterThanOrEqualTo(5), chain.Id);
                Assert.That(chain.Levels.Select(level => level.Icon), Has.All.Not.Null, chain.Id);
            }
        }

        [Test]
        public void ScoreTable_StrictlyRewardsHigherLevelsMore()
        {
            var points = config.ScoreTable.PointsByLevel;
            for (var i = 1; i < points.Count; i++)
                Assert.That(points[i], Is.GreaterThan(points[i - 1]), $"level {i + 1}");
        }

        [Test]
        public void OrderRewards_BeatTheMergePointsSpentOnTheRequestedItems()
        {
            var orders = config.Orders;
            Assert.That(orders, Is.Not.Null);
            for (var level = orders.MinRequestedLevel; level <= orders.MaxRequestedLevel; level++)
            {
                Assert.That(orders.RewardPointsByLevel[level - 1], Is.GreaterThan(MergePointsToBuild(level)), $"level {level}");
            }
        }

        [Test]
        public void EveryCustomerIsAnAnimatedSpinePortrait_AndNoneRepeats()
        {
            var customers = config.Orders.Customers;
            Assert.That(customers.Count, Is.GreaterThanOrEqualTo(config.Orders.MaxActiveOrders), "Active orders should show distinct customers.");
            Assert.That(customers.Select(customer => customer.Portrait), Is.Unique);
            for (var i = 0; i < customers.Count; i++)
            {
                var skeleton = customers[i].Portrait != null ? customers[i].Portrait.GetSkeletonData(true) : null;
                Assert.That(skeleton, Is.Not.Null, $"customer {i} has no loadable portrait");
                var names = skeleton.Animations.Items.Take(skeleton.Animations.Count).Select(animation => animation.Name);
                Assert.That(CustomerAnimationSet.TryResolve(names, out _, out var missing), Is.True,
                    $"customer {i} ({customers[i].Portrait.name}) misses {string.Join(", ", missing)}");
            }
        }

        [Test]
        public void OpeningLayout_HasLockedItemsAndAnImmediateMerge()
        {
            var session = new MergeGameSession(new GameDefinitionFactory().Create(config), new SystemRandom(1));
            var occupied = session.Board.Positions.Where(position => !session.Board.IsEmpty(position)).ToList();

            Assert.That(occupied.Count(position => session.Board.GetItem(position).IsLocked), Is.GreaterThan(0));
            Assert.That(session.Board.EmptyCellCount, Is.GreaterThanOrEqualTo(11));
            Assert.That(
                occupied.SelectMany(from => occupied.Select(to => session.PreviewDrop(from, to).Kind)),
                Has.Some.EqualTo(DropOutcomeKind.Merge));
        }

        [Test]
        public void OpeningLayout_HasBoxesThatCanAllBeOpened()
        {
            var placements = new GameDefinitionFactory().Create(config).InitialPlacements;
            var boxes = placements.Where(placement => placement.IsHidden).Select(placement => placement.Position).ToList();
            var webbed = placements.Where(placement => placement.IsLocked && !placement.IsHidden).Select(placement => placement.Position).ToList();

            Assert.That(boxes, Is.Not.Empty);
            foreach (var box in boxes)
                Assert.That(webbed.Any(cell => Math.Abs(cell.X - box.X) + Math.Abs(cell.Y - box.Y) == 1), Is.True, $"The box at {box} touches no webbed item.");
        }

        [Test]
        public void OpeningLayout_OneMergeFreesAWebbedItemAndOpensABox()
        {
            var session = new MergeGameSession(new GameDefinitionFactory().Create(config), new SystemRandom(1));
            var revealed = 0;
            session.EventRaised += gameEvent => revealed += gameEvent is ItemRevealed ? 1 : 0;
            var opening = session.Board.Positions.SelectMany(from => session.Board.Positions.Select(to => (from, to)))
                .First(pair => session.PreviewDrop(pair.from, pair.to).Kind == DropOutcomeKind.Merge
                               && session.Board.GetItem(pair.to).IsLocked
                               && Neighbours(pair.to).Any(cell => session.Board.IsInside(cell) && session.Board.TryGetItem(cell, out var item) && item.IsHidden));

            session.Drop(opening.from, opening.to);

            Assert.That(revealed, Is.GreaterThan(0));
        }

        [Test]
        public void NewChainAddedAsDataOnly_SpawnsMergesAndHasVisuals()
        {
            var gem = assets.Chain("gem", maxLevel: 5, spawnWeight: 1, scoreMultiplier: 2f);
            var copy = assets.Track(Object.Instantiate(config));
            copy.Chains = config.Chains.Append(gem).ToList();
            copy.Orders = null;
            var definition = new GameDefinitionFactory().Create(copy);
            var weightBeforeGem = config.Chains.Sum(chain => chain.SpawnWeight);
            var session = new MergeGameSession(definition, new ScriptedRandom(weightBeforeGem, 0, weightBeforeGem, 0));

            var first = session.Spawn().Placement;
            var second = session.Spawn().Placement;
            var outcome = session.Drop(first.Position, second.Position);

            Assert.That(first.ItemId, Is.EqualTo(new ItemId("gem", 1)));
            Assert.That(second.ItemId, Is.EqualTo(new ItemId("gem", 1)));
            Assert.That(outcome.Kind, Is.EqualTo(DropOutcomeKind.Merge));
            Assert.That(session.Board.GetItem(second.Position).ItemId, Is.EqualTo(new ItemId("gem", 2)));
            Assert.That(session.Score, Is.GreaterThan(0));
            Assert.That(new ItemVisualLibrary(copy).GetIcon(new ItemId("gem", 2)), Is.SameAs(gem.Levels[1].Icon));
        }

        private static GridPosition[] Neighbours(GridPosition cell) =>
            new[] { new GridPosition(cell.X, cell.Y - 1), new GridPosition(cell.X - 1, cell.Y), new GridPosition(cell.X + 1, cell.Y), new GridPosition(cell.X, cell.Y + 1) };

        /// <summary>Merge points earned while building one item of <paramref name="level"/> from level-1 items.</summary>
        private int MergePointsToBuild(int level)
        {
            var points = config.ScoreTable.PointsByLevel;
            var total = 0;
            for (var created = 2; created <= level; created++)
                total += (1 << (level - created)) * points[created - 1];
            return total;
        }
    }
}
