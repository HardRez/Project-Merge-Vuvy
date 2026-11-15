using System.Collections;
using MergeGame.Presentation.Orders;
using MergeGame.View.Animation;
using MergeGame.View.Orders;
using Spine.Unity;
using UnityEngine;

namespace MergeGame.View.MainMenu
{
    /// <summary>
    /// The customers waiting behind the main menu's counter: each pops in with its arrival animation, one after another,
    /// then idles in its waiting loop; a tap makes one hop, as in the game.
    /// </summary>
    public sealed class MenuCustomers : MonoBehaviour
    {
        [Tooltip("The customers behind the counter, left to right.")]
        [SerializeField] private CustomerPortraitView[] customers = new CustomerPortraitView[0];

        [Tooltip("Who stands where: one character per customer, in the same order.")]
        [SerializeField] private SkeletonDataAsset[] portraits = new SkeletonDataAsset[0];

        [SerializeField] private float firstDelaySeconds = 0.35f;
        [SerializeField] private float betweenSeconds = 0.18f;

        /// <summary>The customers behind the counter, left to right.</summary>
        public CustomerPortraitView[] Customers => customers;

        private IEnumerator Start()
        {
            foreach (var customer in customers)
                customer.gameObject.SetActive(false);

            yield return Tween.Delay(firstDelaySeconds);
            for (var i = 0; i < customers.Length; i++)
            {
                customers[i].gameObject.SetActive(true);
                customers[i].Arrive(i < portraits.Length ? portraits[i] : null, null, CustomerMood.Waiting);
                yield return Tween.Delay(betweenSeconds);
            }
        }
    }
}
