using System.Collections.Generic;
using MergeGame.Core.Items;
using MergeGame.Core.Spawning;

namespace MergeGame.Core.Orders
{
    /// <summary>Decides what the next order asks for. The session picks the customer, assigns the id and computes the reward.</summary>
    public interface IOrderGenerator
    {
        /// <summary>
        /// Returns the requirements of the next order. The session rejects requirements outside its
        /// <see cref="OrderSettings"/> (items per order, requested levels) or naming items missing from the catalog.
        /// </summary>
        IReadOnlyList<ItemId> Generate(IRandom random);
    }
}
