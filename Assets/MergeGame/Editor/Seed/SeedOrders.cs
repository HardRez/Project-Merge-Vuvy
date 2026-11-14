using System;

namespace MergeGame.Editor.Seed
{
    [Serializable]
    internal sealed class SeedOrders
    {
        public int maxActiveOrders;
        public int minItemsPerOrder;
        public int maxItemsPerOrder;
        public int minRequestedLevel;
        public int maxRequestedLevel;
        public int[] rewardPointsByLevel;

        /// <summary>Avatars are <c>Icons/customer_01.png</c> .. <c>customer_NN.png</c>.</summary>
        public int customerCount;
    }
}
