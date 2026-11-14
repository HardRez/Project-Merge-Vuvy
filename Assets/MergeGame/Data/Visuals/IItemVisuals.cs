using MergeGame.Core.Items;
using Spine.Unity;
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

        /// <summary>The customer's still image; null when it only has a portrait.</summary>
        /// <param name="customerIndex">An <c>Order.CustomerIndex</c>, in [0, <see cref="CustomerCount"/>).</param>
        /// <exception cref="System.ArgumentOutOfRangeException">No customer has this index.</exception>
        Sprite GetCustomerAvatar(int customerIndex);

        /// <summary>The customer's animated Spine portrait (see <see cref="CustomerAnimationSet"/>); null when it only has an avatar.</summary>
        /// <param name="customerIndex">An <c>Order.CustomerIndex</c>, in [0, <see cref="CustomerCount"/>).</param>
        /// <exception cref="System.ArgumentOutOfRangeException">No customer has this index.</exception>
        SkeletonDataAsset GetCustomerPortrait(int customerIndex);
    }
}
