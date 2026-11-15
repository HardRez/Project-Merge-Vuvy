using System.Collections;
using System.Linq;
using MergeGame.PlayTests.Support;
using MergeGame.View.Common;
using MergeGame.View.Orders;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MergeGame.PlayTests
{
    /// <summary>The shop counter the customers stand behind, through the real Game scene.</summary>
    public sealed class OrderCounterTests
    {
        // Lets every customer finish its arrival pop-in and settle into its waiting loop.
        private const float SettleSeconds = 1.3f;

        // How much of a customer the counter hides: enough to cover where the drawn bodies end (arms cut off at the
        // elbow or wrist), not so much that the customer sinks.
        private const float MinHiddenShare = 0.1f;
        private const float MaxHiddenShare = 0.35f;

        private const int HighestRequestedLevel = 4;

        // From delivering until the next customer comes in: the items' flight to the tray, then the leaving.
        private const float HandoverLimitSeconds = 1.3f;

        private GameScreenDriver game;

        [UnityTest]
        public IEnumerator EveryCustomer_StandsBehindTheCounter_WhichHidesItsLowerBody()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            yield return GameScreenDriver.WaitSeconds(SettleSeconds);

            var plank = WorldRect(Counter("Plank"));
            var cloth = WorldRect(Counter("Cloth"));
            var screen = WorldRect((RectTransform)game.Orders.GetComponentInParent<Canvas>().rootCanvas.transform);
            for (var slot = 0; slot < game.Orders.CardCount; slot++)
            {
                var body = game.Orders.GetCard(slot).Customer.PortraitWorldRect;
                Assert.That(body.height, Is.GreaterThan(0f), $"Customer {slot} draws nothing.");
                var hidden = (plank.yMax - body.yMin) / body.height;
                Assert.That(hidden, Is.InRange(MinHiddenShare, MaxHiddenShare), $"Customer {slot} shows too much or too little above the counter.");
                Assert.That(body.yMin, Is.GreaterThan(cloth.yMin), $"Customer {slot} reaches out below the counter.");
                Assert.That(body.yMax, Is.LessThan(screen.yMax), $"Customer {slot} is cut off by the top of the screen.");
            }
        }

        [UnityTest]
        public IEnumerator TheCounter_RunsAcrossTheScreen_AndTheWallReachesDownBehindIt()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            yield return null;

            var canvas = (RectTransform)game.Orders.GetComponentInParent<Canvas>().rootCanvas.transform;
            var screen = WorldRect(canvas);
            var plank = WorldRect(Counter("Plank"));
            var tolerance = screen.width * 0.001f;
            Assert.That(plank.xMin, Is.LessThanOrEqualTo(screen.xMin + tolerance));
            Assert.That(plank.xMax, Is.GreaterThanOrEqualTo(screen.xMax - tolerance));

            var wall = WorldRect((RectTransform)canvas.Find("Wall"));
            Assert.That(wall.yMax, Is.GreaterThanOrEqualTo(screen.yMax - tolerance));
            Assert.That(wall.yMin, Is.InRange(plank.yMin, plank.yMax), "The wall stops above the counter or shows under it.");
        }

        [UnityTest]
        public IEnumerator EveryCustomer_IsClippedBehindTheCounter_SoLeavingCannotShowBelowIt()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            yield return GameScreenDriver.WaitSeconds(SettleSeconds);

            var plank = WorldRect(Counter("Plank"));
            var cloth = WorldRect(Counter("Cloth"));
            for (var slot = 0; slot < game.Orders.CardCount; slot++)
            {
                var customer = game.Orders.GetCard(slot).Customer;
                var clip = customer.GetComponentInChildren<RectMask2D>();
                Assert.That(clip, Is.Not.Null, $"Customer {slot} is not clipped.");
                Assert.That(customer.GetComponentInChildren<Spine.Unity.SkeletonGraphic>(true).transform.IsChildOf(clip.transform), Is.True);

                var clipRect = WorldRect(clip.rectTransform);
                Assert.That(clipRect.yMin, Is.InRange(cloth.yMax, plank.yMax), $"Customer {slot} is clipped below the plank, or above the counter top.");
                var body = customer.PortraitWorldRect;
                Assert.That(clipRect.xMin < body.xMin && clipRect.xMax > body.xMax && clipRect.yMax > body.yMax, Is.True,
                    $"The clip cuts into customer {slot} above the counter.");
            }
        }

        [UnityTest]
        public IEnumerator ADeliveredOrder_IsGoneCompletely_BeforeTheNextCustomerComesIn()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(AutoPlayer.PlayUntilAnOrderIsDeliverable(game.Session, HighestRequestedLevel), Is.True);
            yield return GameScreenDriver.WaitSeconds(SettleSeconds);

            var slot = Enumerable.Range(0, game.Orders.CardCount).First(index => game.Orders.GetCard(index).CanDeliver);
            var card = game.Orders.GetCard(slot);
            var customer = card.Customer;
            // This card's own order: when two orders can be delivered, delivering the other one may take this one's items.
            var order = game.Session.ActiveOrders[slot];
            Assert.That(game.Session.CanDeliver(order.Id), Is.True);
            game.Session.DeliverOrder(order.Id);

            var left = false;
            var trayGone = false;
            var customerGone = false;
            var arrivedAfter = -1f;
            for (var elapsed = 0f; elapsed < HandoverLimitSeconds + 1f; elapsed += Time.deltaTime)
            {
                yield return null;
                var animation = customer.CurrentAnimation ?? string.Empty;
                if (animation.EndsWith("_leave"))
                {
                    left = true;
                    trayGone |= card.ContentAlpha <= 0.001f;
                    customerGone |= customer.Opacity <= 0.001f;
                }
                else if (left)
                {
                    arrivedAfter = elapsed;
                    break;
                }
            }

            Assert.That(left, Is.True, "The customer never left.");
            Assert.That(arrivedAfter, Is.GreaterThan(0f), "The next customer never came in.");
            Assert.That(trayGone, Is.True, "The tray was still showing when the next order came in.");
            Assert.That(customerGone, Is.True, "The leaving customer was still showing when the next one came in.");
            Assert.That(arrivedAfter, Is.LessThan(HandoverLimitSeconds), "The handover takes too long.");
            Assert.That(customer.Opacity, Is.EqualTo(1f), "The next customer came in faded.");
        }

        [UnityTest]
        public IEnumerator TheDeliverButton_IsOnScreenOnlyWhileItsOrderCanBeDelivered()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            yield return GameScreenDriver.WaitSeconds(0.5f);
            AssertDeliverButtonsMatchOrders("at the start");

            Assert.That(AutoPlayer.PlayUntilAnOrderIsDeliverable(game.Session, HighestRequestedLevel), Is.True);
            yield return GameScreenDriver.WaitSeconds(SettleSeconds);
            AssertDeliverButtonsMatchOrders("once an order can be delivered");
            var slot = Enumerable.Range(0, game.Orders.CardCount).First(index => game.Orders.GetCard(index).CanDeliver);
            var button = DeliverButton(game.Orders.GetCard(slot));
            Assert.That(button.transform.parent.localScale.x, Is.GreaterThan(0.95f), "The deliver button did not pop all the way in.");

            game.Session.DeliverOrder(game.Session.ActiveOrders[slot].Id);
            yield return GameScreenDriver.WaitSeconds(HandoverLimitSeconds + 0.5f);
            AssertDeliverButtonsMatchOrders("after a delivery");
        }

        private void AssertDeliverButtonsMatchOrders(string when)
        {
            for (var slot = 0; slot < game.Orders.CardCount; slot++)
            {
                var card = game.Orders.GetCard(slot);
                Assert.That(card.IsDeliverButtonShown, Is.EqualTo(card.CanDeliver), $"Card {slot} {when}.");
                Assert.That(DeliverButton(card).gameObject.activeInHierarchy, Is.EqualTo(card.CanDeliver),
                    $"Card {slot}'s deliver button is on screen {when} though its order can{(card.CanDeliver ? "" : "not")} be delivered.");
            }
        }

        private static TactileButton DeliverButton(OrderCardView card) => card.GetComponentInChildren<TactileButton>(true);

        [UnityTest]
        public IEnumerator ACustomer_IsFramedAgain_WhenTheCounterStripChangesHeight()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            yield return GameScreenDriver.WaitSeconds(SettleSeconds);
            var customer = game.Orders.GetCard(0).Customer;
            var before = customer.PortraitWorldRect.height / customer.FrameWorldRect.height;

            var strip = game.Orders.GetComponent<LayoutElement>();
            strip.preferredHeight += 160f;
            strip.minHeight += 160f;
            yield return GameScreenDriver.WaitSeconds(0.2f);

            var after = customer.PortraitWorldRect.height / customer.FrameWorldRect.height;
            Assert.That(after, Is.EqualTo(before).Within(0.03f), "The customer kept its old size in the resized frame.");
        }

        private RectTransform Counter(string part) => (RectTransform)game.Orders.transform.Find("Counter/" + part);

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }
    }
}
