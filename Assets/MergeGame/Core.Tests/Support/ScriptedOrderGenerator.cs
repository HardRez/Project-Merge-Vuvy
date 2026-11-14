using System.Collections.Generic;
using MergeGame.Core.Items;
using MergeGame.Core.Orders;
using MergeGame.Core.Spawning;

namespace MergeGame.Core.Tests.Support
{
    /// <summary>
    /// Returns the given requirement lists in order, repeating the last one once they run out. Never touches the random
    /// source, so every scripted roll in a test goes to the session's customer choice.
    /// </summary>
    internal sealed class ScriptedOrderGenerator : IOrderGenerator
    {
        private readonly Queue<ItemId[]> orders;
        private ItemId[] last;

        public ScriptedOrderGenerator(params ItemId[][] orders)
        {
            this.orders = new Queue<ItemId[]>(orders);
        }

        public int GenerateCount { get; private set; }

        public static ItemId[] Wants(params ItemId[] requirements) => requirements;

        public IReadOnlyList<ItemId> Generate(IRandom random)
        {
            GenerateCount++;
            if (orders.Count > 0)
                last = orders.Dequeue();
            return last;
        }
    }
}
