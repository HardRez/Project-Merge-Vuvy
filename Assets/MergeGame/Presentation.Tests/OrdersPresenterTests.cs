using MergeGame.Core.Orders;
using MergeGame.Presentation.Notices;
using MergeGame.Presentation.Orders;
using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;
using static MergeGame.Presentation.Tests.Support.TestContent;

namespace MergeGame.Presentation.Tests
{
    public sealed class OrdersPresenterTests
    {
        [Test]
        public void GameWithoutOrders_HidesTheStrip()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)));

            screen.Orders.RequestDelivery(0);

            Assert.That(screen.Orders.IsHidden, Is.True);
            Assert.That(screen.Orders.Cards, Is.Empty);
            Assert.That(screen.Notices.Notices, Is.Empty);
        }

        [Test]
        public void Initialize_ShowsOneCardPerSlot_WithCustomerRequirementsRewardAndDeliverability()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithOrders(2, new[] { Item(Wood, 1), Item(Stone, 2) }, new[] { Item(Wood, 1) }));

            Assert.That(screen.Orders.IsHidden, Is.False);
            Assert.That(screen.Orders.Cards.Count, Is.EqualTo(2));

            var first = screen.Orders.Cards[0];
            Assert.That(first.SlotIndex, Is.EqualTo(0));
            Assert.That(first.CustomerIndex, Is.EqualTo(0));
            Assert.That(first.Requirements, Is.EqualTo(new[]
            {
                new RequirementState(Item(Wood, 1), true), new RequirementState(Item(Stone, 2), false)
            }));
            Assert.That(first.RewardPoints, Is.EqualTo(5 + 15));
            Assert.That(first.CanDeliver, Is.False);

            var second = screen.Orders.Cards[1];
            Assert.That(second.CustomerIndex, Is.EqualTo(1));
            Assert.That(second.CanDeliver, Is.True);
        }

        [Test]
        public void LockedItems_DoNotFulfilRequirements()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithLockedItem(0, 0, Item(Wood, 1))
                .WithOrders(1, new[] { Item(Wood, 1) }));

            Assert.That(screen.Orders.Cards[0].Requirements[0].IsFulfilled, Is.False);
            Assert.That(screen.Orders.Cards[0].CanDeliver, Is.False);
        }

        [Test]
        public void Spawn_RefreshesTheCardsItFulfils()
        {
            using var screen = GameScreen.For(new GameBuilder().WithOrders(1, new[] { Item(Wood, 1) }));

            screen.Spawn.PressSpawn();

            Assert.That(screen.Orders.Refreshes.Count, Is.EqualTo(1));
            Assert.That(screen.Orders.Cards[0].Requirements[0].IsFulfilled, Is.True);
            Assert.That(screen.Orders.Cards[0].CanDeliver, Is.True);
        }

        [Test]
        public void Merge_RefreshesFulfilmentOfConsumedAndCreatedItems()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithItem(1, 0, Item(Wood, 1))
                .WithOrders(2, new[] { Item(Wood, 1) }, new[] { Item(Wood, 2) }));
            Assert.That(screen.Orders.Cards[0].CanDeliver, Is.True);
            Assert.That(screen.Orders.Cards[1].CanDeliver, Is.False);

            screen.Board.DragAndDrop(0, 0, 1, 0);

            Assert.That(screen.Orders.Cards[0].CanDeliver, Is.False);
            Assert.That(screen.Orders.Cards[1].CanDeliver, Is.True);
        }

        [Test]
        public void Discard_RefreshesTheCardsThatLostTheirItem()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(2, 2, Item(Stone, 3))
                .WithOrders(1, new[] { Item(Stone, 3) }));

            screen.Selection.Tap(2, 2);
            screen.Info.RequestDiscard();

            Assert.That(screen.Orders.Cards[0].Requirements[0].IsFulfilled, Is.False);
        }

        [Test]
        public void BoardChangesThatDoNotAffectAnOrder_DoNotRefreshItsCard()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithOrders(1, new[] { Item(Stone, 1) }));

            screen.Board.DragAndDrop(0, 0, 3, 3);

            Assert.That(screen.Orders.Refreshes, Is.Empty);
        }

        [Test]
        public void Delivery_CompletesTheSlotWithTheConsumedCells_ThenAddsTheReplacementCard()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(1, 0, Item(Wood, 1))
                .WithItem(2, 1, Item(Stone, 2))
                .WithOrders(2, new[] { Item(Stone, 1) }, new[] { Item(Wood, 1), Item(Stone, 2) }, new[] { Item(Stone, 3) }));

            screen.Orders.RequestDelivery(1);

            var completion = screen.Orders.Completions[0];
            Assert.That(completion.slot, Is.EqualTo(1));
            Assert.That(completion.items, Is.EqualTo(new[]
            {
                new ConsumedItem(At(1, 0), Item(Wood, 1)), new ConsumedItem(At(2, 1), Item(Stone, 2))
            }));
            Assert.That(completion.points, Is.EqualTo(5 + 15));
            Assert.That(screen.Orders.Cards[1].Requirements, Is.EqualTo(new[] { new RequirementState(Item(Stone, 3), false) }));
            Assert.That(screen.Board.Items, Is.Empty);
            var cardCalls = screen.Log.Only("Orders.");
            Assert.That(cardCalls.Count, Is.EqualTo(3));
            Assert.That(cardCalls[1], Is.EqualTo("Orders.CompleteOrder slot 1 +20"));
            Assert.That(cardCalls[2], Does.StartWith("Orders.AddCard Slot 1:"));
        }

        [Test]
        public void DeliveryRequest_ForAnOrderTheBoardCannotFill_ShowsANoticeAndChangesNothing()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithOrders(1, new[] { Item(Wood, 1), Item(Wood, 1) }));

            screen.Orders.RequestDelivery(0);

            Assert.That(screen.Notices.Notices, Is.EqualTo(new[] { Notice.OrderNotReady }));
            Assert.That(screen.Orders.Completions, Is.Empty);
            Assert.That(screen.Session.Board.IsEmpty(At(0, 0)), Is.False);
        }

        [Test]
        public void DeliveryRequest_ForAnUnknownSlot_IsIgnored()
        {
            using var screen = GameScreen.For(new GameBuilder().WithOrders(1, new[] { Item(Wood, 1) }));

            screen.Orders.RequestDelivery(5);
            screen.Orders.RequestDelivery(-1);

            Assert.That(screen.Orders.Completions, Is.Empty);
            Assert.That(screen.Notices.Notices, Is.Empty);
        }
    }
}
