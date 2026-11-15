using System.Collections;
using System.Linq;
using MergeGame.PlayTests.Support;
using MergeGame.View.Orders;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MergeGame.PlayTests
{
    /// <summary>The Spine customers on the order cards, through the real Game scene.</summary>
    public sealed class CustomerPortraitTests
    {
        private const int HighestRequestedLevel = 4;
        private const float SettleSeconds = 1.3f;

        // The card starts the celebration (and the customer leaves) once the delivered items have arrived.
        private const float LeaveStartsWithinSeconds = 0.7f;

        private GameScreenDriver game;

        [UnityTest]
        public IEnumerator EveryCard_ShowsAnAnimatedCustomer_WhoArrives()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);

            var cards = Cards();
            Assert.That(cards.Length, Is.EqualTo(game.Session.ActiveOrders.Count));
            foreach (var card in cards)
            {
                Assert.That(card.Customer.IsAnimated, Is.True, $"Card {card.Card.SlotIndex} shows no animated customer.");
                Assert.That(card.Customer.CurrentAnimation, Does.EndWith("_appear").Or.EndWith("_repeat"));
            }

            var distinct = cards.Select(card => card.Card.CustomerIndex).Distinct().Count();
            Assert.That(distinct, Is.EqualTo(cards.Length), "Two active orders show the same customer.");
        }

        [UnityTest]
        public IEnumerator ADeliverableOrder_MakesItsCustomerFinish_AndDeliveringItMakesTheCustomerLeave()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(AutoPlayer.PlayUntilAnOrderIsDeliverable(game.Session, HighestRequestedLevel), Is.True);
            yield return GameScreenDriver.WaitSeconds(SettleSeconds);

            var card = Cards().First(view => view.CanDeliver);
            Assert.That(card.Customer.CurrentAnimation, Does.EndWith("_finished").Or.EndWith("_finished_repeat"));

            // This card's own order: when two orders can be delivered, delivering the other one may take this one's items.
            var order = game.Session.ActiveOrders[card.Card.SlotIndex];
            Assert.That(game.Session.CanDeliver(order.Id), Is.True);
            game.Session.DeliverOrder(order.Id);
            yield return GameScreenDriver.WaitSeconds(LeaveStartsWithinSeconds);
            Assert.That(card.Customer.CurrentAnimation, Does.EndWith("_leave"));

            yield return GameScreenDriver.WaitSeconds(SettleSeconds);
            Assert.That(card.Customer.IsAnimated, Is.True);
            Assert.That(card.Customer.CurrentAnimation, Does.Not.EndWith("_leave"), "The next customer did not arrive.");
        }

        private static OrderCardView[] Cards() =>
            Object.FindObjectsByType<OrderCardView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }
}
