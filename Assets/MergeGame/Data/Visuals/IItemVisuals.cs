using MergeGame.Core.Items;
using UnityEngine;

namespace MergeGame.Data.Visuals
{
    /// <summary>Presentation data for Core ids, so views never look into authoring assets themselves.</summary>
    public interface IItemVisuals
    {
        /// <exception cref="System.ArgumentException">The item is not part of the configured content.</exception>
        Sprite GetIcon(ItemId item);

        /// <exception cref="System.ArgumentException">The item is not part of the configured content.</exception>
        string GetDisplayName(ItemId item);

        /// <exception cref="System.ArgumentException">No chain has this id.</exception>
        string GetChainDisplayName(string chainId);

        /// <summary>0 when the game has no orders.</summary>
        int CustomerCount { get; }

        /// <param name="customerIndex">An <c>Order.CustomerIndex</c>, in [0, <see cref="CustomerCount"/>).</param>
        /// <exception cref="System.ArgumentOutOfRangeException">No customer has this index.</exception>
        Sprite GetCustomerAvatar(int customerIndex);
    }
}
