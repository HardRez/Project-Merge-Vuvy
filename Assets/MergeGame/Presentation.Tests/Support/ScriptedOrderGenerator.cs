using System.Collections.Generic;
using MergeGame.Core.Items;
using MergeGame.Core.Orders;
using MergeGame.Core.Spawning;

namespace MergeGame.Presentation.Tests.Support
{
    /// <summary>Returns the given requirement lists in order, repeating the last one once they run out.</summary>
    internal sealed class ScriptedOrderGenerator : IOrderGenerator
    {
        private readonly Queue<ItemId[]> orders;
        private ItemId[] last;

        public ScriptedOrderGenerator(IEnumerable<ItemId[]> orders)
        {
            this.orders = new Queue<ItemId[]>(orders);
        }

        public IReadOnlyList<ItemId> Generate(IRandom random)
        {
            if (orders.Count > 0)
                last = orders.Dequeue();
            return last;
        }
    }
}
