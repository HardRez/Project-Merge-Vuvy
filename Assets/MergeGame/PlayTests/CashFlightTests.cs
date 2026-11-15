using System.Collections;
using System.Linq;
using MergeGame.PlayTests.Support;
using MergeGame.View.Score;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MergeGame.PlayTests
{
    /// <summary>The money of a delivered order flying into the score pill, through the real Game scene.</summary>
    public sealed class CashFlightTests
    {
        // Lets the merges of the auto-play finish counting and every customer settle.
        private const float SettleSeconds = 1.3f;
        private const int HighestRequestedLevel = 4;

        // From delivering until the last bill has landed: the items' flight to the tray, the customer paying, the bills.
        private const float PaymentLimitSeconds = 2.5f;

        // The pill's count after the last bill lands.
        private const float CountSeconds = 0.8f;

        private GameScreenDriver game;

        [UnityTest]
        public IEnumerator ADeliveredOrder_PaysIntoTheScorePill_WhichCountsTheMoneyOnlyAsTheBillsLand()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            var cash = Object.FindAnyObjectByType<CashFlightView>();
            Assert.That(cash, Is.Not.Null, "The Game scene has no cash flight.");
            Assert.That(AutoPlayer.PlayUntilAnOrderIsDeliverable(game.Session, HighestRequestedLevel), Is.True);
            yield return GameScreenDriver.WaitSeconds(SettleSeconds);
            Assert.That(game.Score.DisplayedScore, Is.EqualTo(game.Session.Score));

            var before = game.Session.Score;
            var order = game.Session.ActiveOrders.First(active => game.Session.CanDeliver(active.Id));
            game.Session.DeliverOrder(order.Id);
            var awarded = game.Session.Score - before;
            Assert.That(awarded, Is.GreaterThan(0));

            yield return null;
            Assert.That(game.Score.DisplayedScore, Is.EqualTo(before), "The pill counted the money before it flew.");
            Assert.That(game.Score.IncomingPoints, Is.EqualTo(awarded));

            var sawBills = false;
            var countedWhileFlying = false;
            for (var elapsed = 0f; elapsed < PaymentLimitSeconds; elapsed += Time.deltaTime)
            {
                yield return null;
                if (cash.BillsInFlight > 0)
                {
                    sawBills = true;
                    countedWhileFlying |= game.Score.DisplayedScore > before && game.Score.DisplayedScore < game.Session.Score;
                }
                else if (sawBills && game.Score.IncomingPoints == 0)
                {
                    break;
                }
            }

            Assert.That(sawBills, Is.True, "No bills flew to the pill.");
            Assert.That(cash.BillsInFlight, Is.Zero, "The bills are still flying.");
            Assert.That(game.Score.IncomingPoints, Is.Zero, "Some of the money never arrived.");
            Assert.That(countedWhileFlying, Is.True, "The pill did not count bill by bill.");

            yield return GameScreenDriver.WaitSeconds(CountSeconds);
            Assert.That(game.Score.DisplayedScore, Is.EqualTo(game.Session.Score));
        }
    }
}
