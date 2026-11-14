using MergeGame.Presentation.Orders;
using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;
using static MergeGame.Presentation.Tests.Support.TestContent;

namespace MergeGame.Presentation.Tests
{
    public sealed class OrderCardMoodTests
    {
        [Test]
        public void NothingOnTheBoard_IsWaiting()
        {
            var card = Card(canDeliver: false, false, false);

            Assert.That(card.Mood, Is.EqualTo(CustomerMood.Waiting));
        }

        [Test]
        public void PartOfTheOrderOnTheBoard_IsHopeful()
        {
            var card = Card(canDeliver: false, false, true);

            Assert.That(card.Mood, Is.EqualTo(CustomerMood.Hopeful));
        }

        [Test]
        public void DeliverableOrder_IsReady()
        {
            var card = Card(canDeliver: true, true, true);

            Assert.That(card.Mood, Is.EqualTo(CustomerMood.Ready));
        }

        [Test]
        public void EveryRequirementOnTheBoardButNotDeliverable_StaysHopeful()
        {
            // Two orders can count the same board item; only one of them can take it.
            var card = Card(canDeliver: false, true, true);

            Assert.That(card.Mood, Is.EqualTo(CustomerMood.Hopeful));
        }

        [Test]
        public void ShownCards_CarryTheMoodOfWhatTheBoardHolds()
        {
            using var screen = GameScreen.For(new GameBuilder()
                .WithItem(0, 0, Item(Wood, 1))
                .WithOrders(3, new[] { Item(Wood, 1), Item(Stone, 2) }, new[] { Item(Wood, 1) }, new[] { Item(Stone, 2) }));

            Assert.That(screen.Orders.Cards[0].Mood, Is.EqualTo(CustomerMood.Hopeful));
            Assert.That(screen.Orders.Cards[1].Mood, Is.EqualTo(CustomerMood.Ready));
            Assert.That(screen.Orders.Cards[2].Mood, Is.EqualTo(CustomerMood.Waiting));
        }

        private static OrderCard Card(bool canDeliver, params bool[] fulfilled)
        {
            var requirements = new RequirementState[fulfilled.Length];
            for (var i = 0; i < fulfilled.Length; i++)
                requirements[i] = new RequirementState(Item(Wood, i + 1), fulfilled[i]);
            return new OrderCard(0, 0, requirements, 10, canDeliver);
        }
    }
}
