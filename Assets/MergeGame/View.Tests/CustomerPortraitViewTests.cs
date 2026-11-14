using MergeGame.Presentation.Orders;
using MergeGame.View.Orders;
using NUnit.Framework;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace MergeGame.View.Tests
{
    /// <summary>The customer choreography on the shipped order card prefab with a shipped character, without playing.</summary>
    public sealed class CustomerPortraitViewTests
    {
        private const string OrderCardPrefab = "Assets/MergeGame/Content/Prefabs/OrderCard.prefab";
        private const string Character = "Assets/MergeGame/Content/Characters/Alden/Alden_SkeletonData.asset";

        private OrderCardView card;
        private CustomerPortraitView customer;
        private SkeletonDataAsset skeleton;

        [SetUp]
        public void SetUp()
        {
            card = Object.Instantiate(AssetDatabase.LoadAssetAtPath<OrderCardView>(OrderCardPrefab));
            customer = card.Customer;
            skeleton = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(Character);
            Assert.That(skeleton, Is.Not.Null, Character);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(card.gameObject);

        [TestCase(CustomerMood.Waiting, "_appear")]
        [TestCase(CustomerMood.Hopeful, "_progress")]
        [TestCase(CustomerMood.Ready, "_finished")]
        public void ACustomerArrives_WithThePopInOfItsMood(CustomerMood mood, string entry)
        {
            customer.Arrive(skeleton, null, mood);

            Assert.That(customer.IsAnimated, Is.True);
            Assert.That(customer.CurrentAnimation, Does.EndWith(entry));
            Assert.That(customer.IsGlowing, Is.EqualTo(mood == CustomerMood.Ready));
        }

        [TestCase(CustomerMood.Waiting, CustomerMood.Hopeful, "_progress_repeat")]
        [TestCase(CustomerMood.Hopeful, CustomerMood.Ready, "_finished_repeat")]
        [TestCase(CustomerMood.Ready, CustomerMood.Hopeful, "_progress_repeat")]
        [TestCase(CustomerMood.Hopeful, CustomerMood.Waiting, "_appear_repeat")]
        public void AMoodChange_GoesStraightToTheNewLoop_WithoutPoppingInAgain(CustomerMood from, CustomerMood to, string loop)
        {
            customer.Arrive(skeleton, null, from);

            customer.ChangeMood(to);

            Assert.That(customer.CurrentAnimation, Does.EndWith(loop));
            Assert.That(customer.IsGlowing, Is.EqualTo(to == CustomerMood.Ready));
        }

        [Test]
        public void TheSameMoodAgain_KeepsThePopInPlaying()
        {
            customer.Arrive(skeleton, null, CustomerMood.Hopeful);

            customer.ChangeMood(CustomerMood.Hopeful);

            Assert.That(customer.CurrentAnimation, Does.EndWith("_progress"));
        }

        [Test]
        public void ALeavingCustomer_PlaysLeave_AndStopsGlowing()
        {
            customer.Arrive(skeleton, null, CustomerMood.Ready);

            customer.Leave();

            Assert.That(customer.CurrentAnimation, Does.EndWith("_leave"));
            Assert.That(customer.IsGlowing, Is.False);
        }

        [Test]
        public void ACustomerWithoutPortrait_ShowsItsAvatar()
        {
            var still = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f);

            customer.Arrive(null, still, CustomerMood.Waiting);

            Assert.That(customer.IsAnimated, Is.False);
            Assert.That(customer.ShowsAvatar, Is.True);
            Assert.That(customer.CurrentAnimation, Is.Null);
            Object.DestroyImmediate(still);
        }
    }
}
