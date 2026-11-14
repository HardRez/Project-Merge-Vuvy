using System;
using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Orders;
using MergeGame.Core.Session;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.ScriptedOrderGenerator;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class OrderTests
    {
        [Test]
        public void GameWithoutOrderSettings_HasNoOrdersAndCannotDeliver()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Wood, 1)).Build();
            var recorder = new EventRecorder(session);

            Assert.That(session.ActiveOrders, Is.Empty);
            Assert.That(session.CanDeliver(1), Is.False);
            Assert.That(session.DeliverOrder(1).FailureReason, Is.EqualTo(DeliveryFailureReason.UnknownOrder));
            Assert.That(recorder.Events, Is.Empty);
        }

        [Test]
        public void Construction_FillsEverySlotWithUniqueIds()
        {
            var generator = new ScriptedOrderGenerator(Wants(Item(Wood, 1)), Wants(Item(Stone, 2)), Wants(Item(Flower, 3)));

            var session = new SessionBuilder().WithDefinition(DefinitionWithOrders(generator, OrderSetup(maxActiveOrders: 3))).Build();

            Assert.That(generator.GenerateCount, Is.EqualTo(3));
            Assert.That(session.ActiveOrders.Count, Is.EqualTo(3));
            Assert.That(session.ActiveOrders[0].Requirements, Is.EqualTo(new[] { Item(Wood, 1) }));
            Assert.That(session.ActiveOrders[2].Requirements, Is.EqualTo(new[] { Item(Flower, 3) }));
            Assert.That(new[] { session.ActiveOrders[0].Id, session.ActiveOrders[1].Id, session.ActiveOrders[2].Id }, Is.Unique);
        }

        [Test]
        public void Construction_WithTheDefaultGenerator_UsesOnlyTheInjectedRandom()
        {
            var definition = Definition(orderSettings: OrderSetup(maxActiveOrders: 1, minItems: 1, maxItems: 2, customerCount: 3));

            var session = new MergeGameSession(definition, new ScriptedRandom(1, 1, 2, 1, 0, 2));

            var order = session.ActiveOrders[0];
            Assert.That(order.CustomerIndex, Is.EqualTo(1));
            Assert.That(order.Requirements, Is.EqualTo(new[] { Item(Flower, 2), Item(Wood, 3) }));
        }

        [Test]
        public void ActiveOrders_CannotBeModifiedByCallers()
        {
            var session = new SessionBuilder().WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 1))))).Build();

            Assert.That(session.ActiveOrders, Is.Not.InstanceOf<Order[]>());
        }

        [Test]
        public void Reward_IsTheSumOfLevelPointsTimesChainMultiplier_RoundedPerItem()
        {
            var catalog = Catalog(Chain(Wood, scoreMultiplier: 1.5f), Chain(Stone, scoreMultiplier: 2f));
            var generator = new ScriptedOrderGenerator(Wants(Item(Wood, 2), Item(Stone, 3), Item(Wood, 2)));
            var settings = OrderSetup(maxActiveOrders: 1, rewardPoints: new[] { 5, 15, 30, 60, 120 });

            var session = new SessionBuilder().WithDefinition(DefinitionWithOrders(generator, settings, catalog)).Build();

            // wood 2: 15 * 1.5 = 22.5 -> 23; stone 3: 30 * 2 = 60
            Assert.That(session.ActiveOrders[0].RewardPoints, Is.EqualTo(23 + 60 + 23));
        }

        [Test]
        public void Reward_ComesFromTheSettingsData()
        {
            var cheap = OrderSession(OrderSetup(rewardPoints: new[] { 1, 2, 3, 4, 5 }), Wants(Item(Stone, 3)));
            var generous = OrderSession(OrderSetup(rewardPoints: new[] { 10, 50, 300, 400, 500 }), Wants(Item(Stone, 3)));

            Assert.That(cheap.ActiveOrders[0].RewardPoints, Is.EqualTo(3));
            Assert.That(generous.ActiveOrders[0].RewardPoints, Is.EqualTo(300));
        }

        [Test]
        public void SlotCount_ComesFromTheSettingsData()
        {
            var session = OrderSession(OrderSetup(maxActiveOrders: 4), Wants(Item(Wood, 1)));

            Assert.That(session.ActiveOrders.Count, Is.EqualTo(4));
        }

        [Test]
        public void CanDeliver_IsTrueOnlyWhenEveryRequirementIsOnTheBoard()
        {
            var definition = DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 2), Item(Stone, 1))), OrderSetup(maxActiveOrders: 1));
            var missingOne = new SessionBuilder().WithDefinition(definition).WithItem(0, 0, Item(Wood, 2)).Build();
            var complete = new SessionBuilder().WithDefinition(definition).WithItem(0, 0, Item(Wood, 2)).WithItem(4, 4, Item(Stone, 1)).Build();

            Assert.That(missingOne.CanDeliver(missingOne.ActiveOrders[0].Id), Is.False);
            Assert.That(complete.CanDeliver(complete.ActiveOrders[0].Id), Is.True);
        }

        [Test]
        public void CanDeliver_NeedsADistinctItemForEachRepeatedRequirement()
        {
            var definition = DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 2), Item(Wood, 2))), OrderSetup(maxActiveOrders: 1));
            var one = new SessionBuilder().WithDefinition(definition).WithItem(0, 0, Item(Wood, 2)).Build();
            var two = new SessionBuilder().WithDefinition(definition).WithItem(0, 0, Item(Wood, 2)).WithItem(1, 0, Item(Wood, 2)).Build();

            Assert.That(one.CanDeliver(one.ActiveOrders[0].Id), Is.False);
            Assert.That(two.CanDeliver(two.ActiveOrders[0].Id), Is.True);
        }

        [Test]
        public void CanDeliver_IgnoresLockedItems()
        {
            var definition = DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 2))), OrderSetup(maxActiveOrders: 1));
            var session = new SessionBuilder().WithDefinition(definition).WithLockedItem(0, 0, Item(Wood, 2)).Build();
            var orderId = session.ActiveOrders[0].Id;

            Assert.That(session.CanDeliver(orderId), Is.False);
            Assert.That(session.DeliverOrder(orderId).FailureReason, Is.EqualTo(DeliveryFailureReason.MissingItems));
        }

        [Test]
        public void CanDeliver_HasNoSideEffects()
        {
            var generator = new ScriptedOrderGenerator(Wants(Item(Wood, 2)));
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(generator, OrderSetup(maxActiveOrders: 1)))
                .WithItem(0, 0, Item(Wood, 2))
                .Build();
            var recorder = new EventRecorder(session);
            var order = session.ActiveOrders[0];

            Assert.That(session.CanDeliver(order.Id), Is.True);
            Assert.That(session.CanDeliver(order.Id), Is.True);

            Assert.That(recorder.Events, Is.Empty);
            Assert.That(session.ActiveOrders[0], Is.SameAs(order));
            Assert.That(session.Board.GetItem(new GridPosition(0, 0)).ItemId, Is.EqualTo(Item(Wood, 2)));
            Assert.That(generator.GenerateCount, Is.EqualTo(1));
        }

        [Test]
        public void CanDeliver_ForUnknownOrder_IsFalse()
        {
            var session = OrderSession(OrderSetup(), Wants(Item(Wood, 1)));

            Assert.That(session.CanDeliver(999), Is.False);
        }

        [Test]
        public void DeliverOrder_UnknownOrder_FailsAndChangesNothing()
        {
            var session = OrderSession(OrderSetup(), Wants(Item(Wood, 1)));
            var recorder = new EventRecorder(session);

            var result = session.DeliverOrder(999);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(DeliveryFailureReason.UnknownOrder));
            Assert.That(recorder.Events, Is.Empty);
            Assert.Throws<InvalidOperationException>(() => _ = result.DeliveredOrder);
        }

        [Test]
        public void DeliverOrder_WithMissingItems_FailsAndChangesNothing()
        {
            var generator = new ScriptedOrderGenerator(Wants(Item(Wood, 2), Item(Stone, 1)));
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(generator, OrderSetup(maxActiveOrders: 1)))
                .WithItem(0, 0, Item(Wood, 2))
                .Build();
            var recorder = new EventRecorder(session);
            var order = session.ActiveOrders[0];

            var result = session.DeliverOrder(order.Id);

            Assert.That(result.FailureReason, Is.EqualTo(DeliveryFailureReason.MissingItems));
            Assert.That(recorder.Events, Is.Empty);
            Assert.That(session.Board.IsEmpty(new GridPosition(0, 0)), Is.False);
            Assert.That(session.ActiveOrders[0], Is.SameAs(order));
            Assert.That(session.Score, Is.Zero);
        }

        [Test]
        public void DeliverOrder_RemovesTheItemsAndAwardsTheReward()
        {
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 2), Item(Stone, 1))), OrderSetup(maxActiveOrders: 1)))
                .WithItem(0, 0, Item(Wood, 2))
                .WithItem(3, 2, Item(Stone, 1))
                .Build();
            var order = session.ActiveOrders[0];

            var result = session.DeliverOrder(order.Id);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.DeliveredOrder, Is.SameAs(order));
            Assert.That(session.Board.EmptyCellCount, Is.EqualTo(25));
            Assert.That(session.Score, Is.EqualTo(15 + 5));
        }

        [Test]
        public void DeliverOrder_TakesTheFirstUnlockedMatchingItemsInRowMajorOrder()
        {
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 2), Item(Wood, 2))), OrderSetup(maxActiveOrders: 1)))
                .WithLockedItem(0, 0, Item(Wood, 2))
                .WithItem(1, 2, Item(Wood, 2))
                .WithItem(4, 0, Item(Wood, 2))
                .WithItem(0, 1, Item(Wood, 2))
                .Build();
            var recorder = new EventRecorder(session);

            session.DeliverOrder(session.ActiveOrders[0].Id);

            var consumed = recorder.Single<OrderDelivered>().ConsumedItems;
            Assert.That(consumed.Count, Is.EqualTo(2));
            Assert.That(consumed[0].Position, Is.EqualTo(new GridPosition(4, 0)));
            Assert.That(consumed[1].Position, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(session.Board.GetItem(new GridPosition(0, 0)).IsLocked, Is.True);
            Assert.That(session.Board.IsEmpty(new GridPosition(1, 2)), Is.False);
        }

        [Test]
        public void DeliverOrder_RaisesDeliveredThenScoreThenAdded()
        {
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 1))), OrderSetup(maxActiveOrders: 1)))
                .WithItem(2, 2, Item(Wood, 1))
                .Build();
            var recorder = new EventRecorder(session);

            session.DeliverOrder(session.ActiveOrders[0].Id);

            Assert.That(recorder.Types, Is.EqualTo(new[] { typeof(OrderDelivered), typeof(ScoreChanged), typeof(OrderAdded) }));
        }

        [Test]
        public void DeliverOrder_OnFullBoard_RaisesBoardFullChangedBeforeOrderAdded()
        {
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Stone, 3))), OrderSetup(maxActiveOrders: 1)))
                .WithFullBoardOf(Item(Stone, 3))
                .Build();
            var recorder = new EventRecorder(session);

            session.DeliverOrder(session.ActiveOrders[0].Id);

            Assert.That(recorder.Types, Is.EqualTo(new[]
            {
                typeof(OrderDelivered), typeof(ScoreChanged), typeof(BoardFullChanged), typeof(OrderAdded)
            }));
            Assert.That(((BoardFullChanged)recorder.Events[2]).IsFull, Is.False);
            Assert.That(session.CanSpawn, Is.True);
        }

        [Test]
        public void DeliverOrder_WorthNothing_RaisesNoScoreChanged()
        {
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(
                    new ScriptedOrderGenerator(Wants(Item(Wood, 1))),
                    OrderSetup(maxActiveOrders: 1, rewardPoints: new[] { 0, 10, 20, 30, 40 })))
                .WithItem(0, 0, Item(Wood, 1))
                .Build();
            var recorder = new EventRecorder(session);

            session.DeliverOrder(session.ActiveOrders[0].Id);

            Assert.That(recorder.Types, Is.EqualTo(new[] { typeof(OrderDelivered), typeof(OrderAdded) }));
            Assert.That(session.Score, Is.Zero);
        }

        [Test]
        public void OrderDelivered_DescribesTheWholeDelivery()
        {
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(
                    new ScriptedOrderGenerator(Wants(Item(Wood, 1)), Wants(Item(Stone, 2), Item(Wood, 3))),
                    OrderSetup(maxActiveOrders: 2)))
                .WithItem(1, 1, Item(Wood, 3))
                .WithItem(3, 0, Item(Stone, 2))
                .Build();
            var recorder = new EventRecorder(session);
            var order = session.ActiveOrders[1];

            session.DeliverOrder(order.Id);

            var delivered = recorder.Single<OrderDelivered>();
            Assert.That(delivered.Order, Is.SameAs(order));
            Assert.That(delivered.SlotIndex, Is.EqualTo(1));
            Assert.That(delivered.AwardedPoints, Is.EqualTo(15 + 30));
            Assert.That(delivered.ConsumedItems[0], Is.EqualTo(new ConsumedItem(new GridPosition(3, 0), Item(Stone, 2))));
            Assert.That(delivered.ConsumedItems[1], Is.EqualTo(new ConsumedItem(new GridPosition(1, 1), Item(Wood, 3))));
        }

        [Test]
        public void DeliverOrder_ReplacesTheOrderInTheSameSlot()
        {
            var generator = new ScriptedOrderGenerator(Wants(Item(Wood, 1)), Wants(Item(Stone, 1)), Wants(Item(Flower, 2)));
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(generator, OrderSetup(maxActiveOrders: 2)))
                .WithItem(0, 0, Item(Stone, 1))
                .Build();
            var recorder = new EventRecorder(session);
            var untouched = session.ActiveOrders[0];
            var delivered = session.ActiveOrders[1];

            session.DeliverOrder(delivered.Id);

            var added = recorder.Single<OrderAdded>();
            Assert.That(added.SlotIndex, Is.EqualTo(1));
            Assert.That(session.ActiveOrders[1], Is.SameAs(added.Order));
            Assert.That(added.Order.Requirements, Is.EqualTo(new[] { Item(Flower, 2) }));
            Assert.That(session.ActiveOrders[0], Is.SameAs(untouched));
            Assert.That(session.ActiveOrders.Count, Is.EqualTo(2));
        }

        [Test]
        public void DeliverOrder_GivesTheReplacementANewIdAndRetiresTheOldOne()
        {
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 1))), OrderSetup(maxActiveOrders: 2)))
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .Build();
            var firstId = session.ActiveOrders[0].Id;
            var secondId = session.ActiveOrders[1].Id;

            session.DeliverOrder(firstId);
            var replacementId = session.ActiveOrders[0].Id;

            Assert.That(replacementId, Is.Not.EqualTo(firstId).And.Not.EqualTo(secondId));
            Assert.That(session.DeliverOrder(firstId).FailureReason, Is.EqualTo(DeliveryFailureReason.UnknownOrder));
            Assert.That(session.DeliverOrder(replacementId).Succeeded, Is.True);
        }

        [Test]
        public void EventHandlers_SeeTheDeliveryAlreadyApplied()
        {
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 1)), Wants(Item(Stone, 1))), OrderSetup(maxActiveOrders: 1)))
                .WithItem(0, 0, Item(Wood, 1))
                .Build();
            Order activeDuringEvent = null;
            var scoreDuringEvent = -1;
            var cellEmptyDuringEvent = false;
            session.EventRaised += gameEvent =>
            {
                if (!(gameEvent is OrderDelivered))
                    return;
                activeDuringEvent = session.ActiveOrders[0];
                scoreDuringEvent = session.Score;
                cellEmptyDuringEvent = session.Board.IsEmpty(new GridPosition(0, 0));
            };

            session.DeliverOrder(session.ActiveOrders[0].Id);

            Assert.That(activeDuringEvent.Requirements, Is.EqualTo(new[] { Item(Stone, 1) }));
            Assert.That(scoreDuringEvent, Is.EqualTo(5));
            Assert.That(cellEmptyDuringEvent, Is.True);
        }

        [Test]
        public void Generator_ProposingAnOrderOutsideTheSettings_IsRejected()
        {
            var tooHigh = DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 4))), OrderSetup(maxLevel: 3));
            var tooMany = DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 1), Item(Wood, 1))), OrderSetup(maxItems: 1));
            var unknownItem = DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item("gem", 1))), OrderSetup());

            Assert.That(() => new SessionBuilder().WithDefinition(tooHigh).Build(), Throws.InvalidOperationException.With.Message.Contains("wood:4"));
            Assert.That(() => new SessionBuilder().WithDefinition(tooMany).Build(), Throws.InvalidOperationException.With.Message.Contains("2 items"));
            Assert.That(() => new SessionBuilder().WithDefinition(unknownItem).Build(), Throws.InvalidOperationException.With.Message.Contains("gem:1"));
        }

        [Test]
        public void DeliverOrder_WhenTheReplacementIsInvalid_ThrowsAndChangesNothing()
        {
            var generator = new ScriptedOrderGenerator(Wants(Item(Wood, 1)), Wants(Item(Wood, 5)));
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(generator, OrderSetup(maxActiveOrders: 1, maxLevel: 3)))
                .WithItem(0, 0, Item(Wood, 1))
                .Build();
            var recorder = new EventRecorder(session);
            var order = session.ActiveOrders[0];

            Assert.Throws<InvalidOperationException>(() => session.DeliverOrder(order.Id));

            Assert.That(session.Board.IsEmpty(new GridPosition(0, 0)), Is.False);
            Assert.That(session.ActiveOrders[0], Is.SameAs(order));
            Assert.That(session.Score, Is.Zero);
            Assert.That(recorder.Events, Is.Empty);
        }

        private static MergeGameSession OrderSession(OrderSettings settings, params ItemId[][] requirementLists) =>
            new SessionBuilder().WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(requirementLists), settings)).Build();
    }
}
