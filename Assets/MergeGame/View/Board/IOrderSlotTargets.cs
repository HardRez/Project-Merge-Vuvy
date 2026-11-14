using UnityEngine;

namespace MergeGame.View.Board
{
    /// <summary>Where delivered board items fly to; implemented by the orders strip.</summary>
    public interface IOrderSlotTargets
    {
        /// <returns>False when the slot has no visible card (e.g. the game has no orders).</returns>
        bool TryGetDeliveryTarget(int slotIndex, out Vector3 worldPosition);
    }
}
