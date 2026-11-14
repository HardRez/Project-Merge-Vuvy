using System;
using System.Collections.Generic;
using MergeGame.Core.Orders;
using MergeGame.Core.Session;
using MergeGame.Core.Spawning;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.ScriptedOrderGenerator;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class OrderCustomerTests
    {
        [Test]
        public void Construction_GivesEveryActiveOrderADifferentCustomer()
        {
            var session = Session(OrderSetup(maxActiveOrders: 4, customerCount: 4), new ScriptedRandom());

            Assert.That(Customers(session), Is.EqualTo(new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void Customer_IsRolledAmongTheCustomersNoOtherSlotShows()
        {
            // slot 0: roll 2 of [0, 1, 2, 3] -> 2; slot 1: roll 2 of [0, 1, 3] -> 3
            var session = Session(OrderSetup(maxActiveOrders: 2, customerCount: 4), new ScriptedRandom(2, 2));

            Assert.That(Customers(session), Is.EqualTo(new[] { 2, 3 }));
        }

        [Test]
        public void Replacement_MayReuseTheDeliveredCustomerButNotTheOtherSlots()
        {
            // construction: slot 0 -> 0, slot 1 -> 1; replacement for slot 0 rolls among [0, 2]
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 1))), OrderSetup(maxActiveOrders: 2, customerCount: 3)))
                .WithRandom(new ScriptedRandom(0, 0, 1))
                .WithItem(0, 0, Item(Wood, 1))
                .Build();

            session.DeliverOrder(session.ActiveOrders[0].Id);

            Assert.That(Customers(session), Is.EqualTo(new[] { 2, 1 }));
        }

        [Test]
        public void Replacement_OffersOnlyTheFreeCustomersToTheRandomSource()
        {
            var session = new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 1))), OrderSetup(maxActiveOrders: 2, customerCount: 3)))
                .WithRandom(new ScriptedRandom(0, 0, 2))
                .WithItem(0, 0, Item(Wood, 1))
                .Build();
            var order = session.ActiveOrders[0];

            var exception = Assert.Throws<InvalidOperationException>(() => session.DeliverOrder(order.Id));
            Assert.That(exception.Message, Does.Contain("[0, 2)"));
            Assert.That(session.ActiveOrders[0], Is.SameAs(order));
        }

        [Test]
        public void WithFewerCustomersThanSlots_CustomersAreReusedOnlyOnceAllAreShown()
        {
            // slot 0 -> 0; slot 1: only 1 is free -> 1; slot 2: none free, roll 1 of all -> 1
            var session = Session(OrderSetup(maxActiveOrders: 3, customerCount: 2), new ScriptedRandom(0, 0, 1));

            Assert.That(Customers(session), Is.EqualTo(new[] { 0, 1, 1 }));
        }

        [Test]
        public void WithASingleCustomer_EveryOrderComesFromIt()
        {
            var session = Session(OrderSetup(maxActiveOrders: 3, customerCount: 1), new ScriptedRandom());

            Assert.That(Customers(session), Is.EqualTo(new[] { 0, 0, 0 }));
        }

        [Test]
        public void DefaultGenerator_StillGivesDistinctCustomers()
        {
            for (var seed = 0; seed < 20; seed++)
            {
                var definition = Definition(orderSettings: OrderSetup(maxActiveOrders: 4, customerCount: 5));
                var session = new MergeGameSession(definition, new SystemRandom(seed));

                Assert.That(Customers(session), Is.Unique, $"seed {seed}");
                Assert.That(Customers(session), Has.All.InRange(0, 4), $"seed {seed}");
            }
        }

        private static MergeGameSession Session(OrderSettings settings, IRandom random) =>
            new SessionBuilder()
                .WithDefinition(DefinitionWithOrders(new ScriptedOrderGenerator(Wants(Item(Wood, 1))), settings))
                .WithRandom(random)
                .Build();

        private static List<int> Customers(MergeGameSession session)
        {
            var customers = new List<int>();
            foreach (var order in session.ActiveOrders)
                customers.Add(order.CustomerIndex);
            return customers;
        }
    }
}
