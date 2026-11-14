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

        public SeedCustomer[] customers;
    }
}
