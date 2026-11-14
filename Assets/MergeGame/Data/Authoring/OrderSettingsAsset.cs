using System.Collections.Generic;
using UnityEngine;

namespace MergeGame.Data.Authoring
{
    /// <summary>Customer orders: how many, what they ask for, what they pay and who asks.</summary>
    [CreateAssetMenu(menuName = "Merge Game/Order Settings", fileName = "OrderSettings")]
    public sealed class OrderSettingsAsset : ScriptableObject
    {
        [Tooltip("Orders shown at the same time. A delivered order is replaced immediately.")]
        [Min(1)]
        [SerializeField] private int maxActiveOrders = 2;

        [Min(1)]
        [SerializeField] private int minItemsPerOrder = 1;

        [Min(1)]
        [SerializeField] private int maxItemsPerOrder = 2;

        [Tooltip("Lowest item level a customer asks for.")]
        [Min(1)]
        [SerializeField] private int minRequestedLevel = 2;

        [Tooltip("Highest item level a customer asks for; capped at each chain's max level.")]
        [Min(1)]
        [SerializeField] private int maxRequestedLevel = 4;

        [Tooltip("Points per delivered item by its level (element 0 = level 1), scaled by the chain's score multiplier. " +
                 "Must not decrease and must reach the max requested level.")]
        [SerializeField] private List<int> rewardPointsByLevel = new List<int>();

        [Tooltip("One avatar per customer; the number of customers is the length of this list.")]
        [SerializeField] private List<Sprite> customerAvatars = new List<Sprite>();

        public int MaxActiveOrders
        {
            get => maxActiveOrders;
            internal set => maxActiveOrders = value;
        }

        public int MinItemsPerOrder
        {
            get => minItemsPerOrder;
            internal set => minItemsPerOrder = value;
        }

        public int MaxItemsPerOrder
        {
            get => maxItemsPerOrder;
            internal set => maxItemsPerOrder = value;
        }

        public int MinRequestedLevel
        {
            get => minRequestedLevel;
            internal set => minRequestedLevel = value;
        }

        public int MaxRequestedLevel
        {
            get => maxRequestedLevel;
            internal set => maxRequestedLevel = value;
        }

        /// <summary>Index 0 is level 1.</summary>
        public IReadOnlyList<int> RewardPointsByLevel
        {
            get => rewardPointsByLevel;
            internal set => rewardPointsByLevel = new List<int>(value);
        }

        /// <summary>Index i is the avatar of customer i (see <c>Order.CustomerIndex</c>).</summary>
        public IReadOnlyList<Sprite> CustomerAvatars
        {
            get => customerAvatars;
            internal set => customerAvatars = new List<Sprite>(value);
        }

        public int CustomerCount => customerAvatars.Count;
    }
}
