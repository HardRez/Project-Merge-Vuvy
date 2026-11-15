using System;
using System.Linq;
using MergeGame.Core.Grid;
using MergeGame.Core.Orders;
using MergeGame.Core.Session;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.ScriptedOrderGenerator;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class SnapshotTests
    {
        [Test]
        public void CaptureSnapshot_HoldsEveryItemWithItsFlags_TheScoreAndTheOrders()
        {
            var generator = new ScriptedOrderGenerator(Wants(Item(Wood, 2)), Wants(Item(Stone, 1), Item(Flower, 3)));
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(generator))
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithLockedItem(4, 4, Item(Stone, 2))
                .WithHiddenItem(3, 4, Item(Flower, 1))
                .Build();
            session.Drop(new GridPosition(0, 0), new GridPosition(1, 0));

            var snapshot = session.CaptureSnapshot();

            Assert.That(snapshot.Score, Is.EqualTo(session.Score).And.GreaterThan(0));
            Assert.That(snapshot.Items.Select(Describe), Is.EqualTo(new[]
            {
                "wood:2 at (1, 0) free",
                "flower:1 at (3, 4) hidden",
                "stone:2 at (4, 4) locked"
            }));
            Assert.That(snapshot.Orders.Select(order => order.Id), Is.EqualTo(session.ActiveOrders.Select(order => order.Id)));
            Assert.That(snapshot.NextOrderId, Is.GreaterThan(session.ActiveOrders.Max(order => order.Id)));
        }

        [Test]
        public void Resume_CarriesOnExactlyWhereTheSnapshotStopped()
        {
            var generator = new ScriptedOrderGenerator(Wants(Item(Wood, 2)), Wants(Item(Stone, 1)), Wants(Item(Flower, 1)));
            var definition = DefinitionWithOrders(generator, OrderSetup(customerCount: 2));
            var original = new SessionBuilder()
                .WithDefinition(definition)
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithLockedItem(2, 2, Item(Stone, 3))
                .WithHiddenItem(2, 3, Item(Flower, 1))
                .Build();
            original.Drop(new GridPosition(0, 0), new GridPosition(1, 0));

            var resumed = MergeGameSession.Resume(definition, new ScriptedRandom(), original.CaptureSnapshot());

            Assert.That(resumed.Score, Is.EqualTo(original.Score));
            Assert.That(resumed.Board.TryGetItem(new GridPosition(0, 0), out _), Is.False);
            Assert.That(resumed.Board.GetItem(new GridPosition(1, 0)).ItemId, Is.EqualTo(Item(Wood, 2)));
            Assert.That(resumed.Board.GetItem(new GridPosition(2, 2)).IsLocked, Is.True);
            Assert.That(resumed.Board.GetItem(new GridPosition(2, 3)).IsHidden, Is.True);
            Assert.That(resumed.Board.EmptyCellCount, Is.EqualTo(original.Board.EmptyCellCount));
            Assert.That(resumed.ActiveOrders.Select(order => (order.Id, order.CustomerIndex, order.RewardPoints)),
                Is.EqualTo(original.ActiveOrders.Select(order => (order.Id, order.CustomerIndex, order.RewardPoints))));
            Assert.That(resumed.ActiveOrders[0].Requirements, Is.EqualTo(original.ActiveOrders[0].Requirements));
        }

        [Test]
        public void Resume_DoesNotRegenerateOrders_AndANewOrderGetsTheNextFreeId()
        {
            var generator = new ScriptedOrderGenerator(Wants(Item(Wood, 1)), Wants(Item(Stone, 1)), Wants(Item(Flower, 1)));
            var definition = DefinitionWithOrders(generator, OrderSetup(maxActiveOrders: 2, customerCount: 3));
            var snapshot = new SessionBuilder().WithDefinition(definition).Build().CaptureSnapshot();
            var generatedBefore = generator.GenerateCount;

            var resumed = MergeGameSession.Resume(definition, new ScriptedRandom(), snapshot);
            Assert.That(generator.GenerateCount, Is.EqualTo(generatedBefore), "A resumed game keeps its saved orders.");

            var resumedBoard = MergeGameSession.Resume(definition, new ScriptedRandom(),
                new GameSnapshot(0, new[] { new InitialPlacement(new GridPosition(0, 0), Item(Wood, 1)) }, snapshot.Orders, snapshot.NextOrderId));
            var delivered = resumedBoard.DeliverOrder(snapshot.Orders[0].Id);

            Assert.That(delivered.Succeeded, Is.True);
            Assert.That(resumedBoard.ActiveOrders[0].Id, Is.EqualTo(snapshot.NextOrderId));
            Assert.That(resumed.ActiveOrders.Count, Is.EqualTo(2));
        }

        [Test]
        public void Resume_DoesNotApplyTheDefinitionsInitialPlacements()
        {
            var definition = Definition(initialPlacements: new[] { new InitialPlacement(new GridPosition(0, 0), Item(Wood, 1)) });
            var empty = new GameSnapshot(0, Array.Empty<InitialPlacement>(), Array.Empty<Order>(), 1);

            var resumed = MergeGameSession.Resume(definition, new ScriptedRandom(), empty);

            Assert.That(resumed.Board.EmptyCellCount, Is.EqualTo(25));
        }

        [Test]
        public void Resume_RejectsASnapshotThatNoLongerFitsTheGame()
        {
            var definition = Definition(width: 3, height: 3);
            var noOrders = Array.Empty<Order>();

            Assert.Throws<ArgumentException>(() => MergeGameSession.Resume(definition, new ScriptedRandom(),
                new GameSnapshot(0, new[] { new InitialPlacement(new GridPosition(4, 4), Item(Wood, 1)) }, noOrders, 1)), "outside the board");
            Assert.Throws<ArgumentException>(() => MergeGameSession.Resume(definition, new ScriptedRandom(),
                new GameSnapshot(0, new[] { new InitialPlacement(new GridPosition(0, 0), Item("gone", 1)) }, noOrders, 1)), "unknown chain");
            Assert.Throws<ArgumentException>(() => MergeGameSession.Resume(definition, new ScriptedRandom(),
                new GameSnapshot(0, new[] { new InitialPlacement(new GridPosition(0, 0), Item(Wood, ChainLength + 1)) }, noOrders, 1)), "level beyond the chain");
            Assert.Throws<ArgumentException>(() => MergeGameSession.Resume(definition, new ScriptedRandom(),
                new GameSnapshot(0, new[]
                {
                    new InitialPlacement(new GridPosition(1, 1), Item(Wood, 1)),
                    new InitialPlacement(new GridPosition(1, 1), Item(Stone, 1))
                }, noOrders, 1)), "two items in a cell");
            Assert.Throws<ArgumentException>(() => MergeGameSession.Resume(definition, new ScriptedRandom(),
                new GameSnapshot(0, Array.Empty<InitialPlacement>(), new[] { new Order(1, 0, new[] { Item(Wood, 1) }, 5) }, 2)), "orders in a game without orders");
        }

        [Test]
        public void Resume_RejectsSavedOrdersThatNoLongerFitTheOrderSettings()
        {
            var definition = DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 1))), OrderSetup(maxActiveOrders: 1, maxLevel: 3, customerCount: 2));
            var items = Array.Empty<InitialPlacement>();

            Assert.Throws<ArgumentException>(() => MergeGameSession.Resume(definition, new ScriptedRandom(),
                new GameSnapshot(0, items, Array.Empty<Order>(), 1)), "too few orders for the slots");
            Assert.Throws<ArgumentException>(() => MergeGameSession.Resume(definition, new ScriptedRandom(),
                new GameSnapshot(0, items, new[] { new Order(1, 5, new[] { Item(Wood, 1) }, 5) }, 2)), "unknown customer");
            Assert.Throws<ArgumentException>(() => MergeGameSession.Resume(definition, new ScriptedRandom(),
                new GameSnapshot(0, items, new[] { new Order(1, 0, new[] { Item(Wood, 5) }, 5) }, 2)), "level the orders no longer ask for");
        }

        [Test]
        public void Resume_RepricesSavedOrdersWithTheCurrentRewards()
        {
            var definition = DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 1))), OrderSetup(maxActiveOrders: 1));
            var saved = new Order(7, 0, new[] { Item(Wood, 2) }, rewardPoints: 999);

            var resumed = MergeGameSession.Resume(definition, new ScriptedRandom(), new GameSnapshot(0, Array.Empty<InitialPlacement>(), new[] { saved }, 8));

            Assert.That(resumed.ActiveOrders[0].Id, Is.EqualTo(7));
            Assert.That(resumed.ActiveOrders[0].RewardPoints, Is.EqualTo(DefaultRewardPoints[1]));
        }

        [Test]
        public void Snapshot_RejectsInconsistentData()
        {
            var items = Array.Empty<InitialPlacement>();
            var order = new Order(3, 0, new[] { Item(Wood, 1) }, 5);

            Assert.Throws<ArgumentOutOfRangeException>(() => new GameSnapshot(-1, items, Array.Empty<Order>(), 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameSnapshot(0, items, Array.Empty<Order>(), 0));
            Assert.Throws<ArgumentException>(() => new GameSnapshot(0, items, new[] { order }, 3));
            Assert.Throws<ArgumentNullException>(() => MergeGameSession.Resume(Definition(), new ScriptedRandom(), null));
        }

        private static string Describe(InitialPlacement item) =>
            $"{item.ItemId} at {item.Position} {(item.IsHidden ? "hidden" : item.IsLocked ? "locked" : "free")}";
    }
}
