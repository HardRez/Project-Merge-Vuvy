using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Session;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.ScriptedOrderGenerator;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class RequirementFulfilmentTests
    {
        [Test]
        public void Fulfilment_HasOneEntryPerRequirement_CountingRepeatedItemsSeparately()
        {
            var session = Builder(Item(Wood, 2), Item(Stone, 1), Item(Wood, 2))
                .WithItem(0, 0, Item(Wood, 2))
                .WithItem(1, 0, Item(Stone, 1))
                .Build();

            Assert.That(Fulfilment(session), Is.EqualTo(new[] { true, true, false }));
            Assert.That(session.CanDeliver(session.ActiveOrders[0].Id), Is.False);
        }

        [Test]
        public void Fulfilment_ContinuesPastAMissingRequirement()
        {
            var session = Builder(Item(Stone, 3), Item(Wood, 1)).WithItem(4, 4, Item(Wood, 1)).Build();

            Assert.That(Fulfilment(session), Is.EqualTo(new[] { false, true }));
        }

        [Test]
        public void Fulfilment_IgnoresLockedItems()
        {
            var session = Builder(Item(Wood, 2), Item(Wood, 2))
                .WithLockedItem(0, 0, Item(Wood, 2))
                .WithItem(2, 2, Item(Wood, 2))
                .Build();

            Assert.That(Fulfilment(session), Is.EqualTo(new[] { true, false }));
        }

        [Test]
        public void Fulfilment_AllTrue_ExactlyWhenTheOrderCanBeDelivered()
        {
            var session = Builder(Item(Wood, 2), Item(Stone, 1)).WithItem(3, 1, Item(Stone, 1)).WithItem(0, 4, Item(Wood, 2)).Build();

            Assert.That(Fulfilment(session), Is.EqualTo(new[] { true, true }));
            Assert.That(session.CanDeliver(session.ActiveOrders[0].Id), Is.True);
        }

        [Test]
        public void Fulfilment_HasNoSideEffects()
        {
            var generator = new ScriptedOrderGenerator(Wants(Item(Wood, 2)));
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(generator, OrderSetup(maxActiveOrders: 1)))
                .WithItem(0, 0, Item(Wood, 2))
                .Build();
            var recorder = new EventRecorder(session);
            var order = session.ActiveOrders[0];

            var first = session.GetRequirementFulfilment(order.Id);
            var second = session.GetRequirementFulfilment(order.Id);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(recorder.Events, Is.Empty);
            Assert.That(session.ActiveOrders[0], Is.SameAs(order));
            Assert.That(session.Board.GetItem(new GridPosition(0, 0)).ItemId, Is.EqualTo(Item(Wood, 2)));
            Assert.That(generator.GenerateCount, Is.EqualTo(1));
        }

        [Test]
        public void Fulfilment_ForUnknownOrder_IsEmpty()
        {
            var session = Builder(Item(Wood, 1)).Build();

            Assert.That(session.GetRequirementFulfilment(999), Is.Empty);
        }

        [Test]
        public void Fulfilment_InAGameWithoutOrders_IsEmpty()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Wood, 1)).Build();

            Assert.That(session.GetRequirementFulfilment(1), Is.Empty);
        }

        private static SessionBuilder Builder(params ItemId[] requirements) =>
            new SessionBuilder().WithDefinition(
                DefinitionWithOrders(new ScriptedOrderGenerator(requirements), OrderSetup(maxActiveOrders: 1)));

        private static IReadOnlyList<bool> Fulfilment(MergeGameSession session) =>
            session.GetRequirementFulfilment(session.ActiveOrders[0].Id);
    }
}
