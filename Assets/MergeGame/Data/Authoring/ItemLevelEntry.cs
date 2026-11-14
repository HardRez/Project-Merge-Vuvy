using System;
using UnityEngine;

namespace MergeGame.Data.Authoring
{
    /// <summary>One level of an <see cref="ItemChainAsset"/>; its level number is its position in the chain's list.</summary>
    [Serializable]
    public sealed class ItemLevelEntry
    {
        [SerializeField] private string displayName = string.Empty;

        [Tooltip("Shown on the board, in orders and in the merge feedback. Must be imported as a Sprite.")]
        [SerializeField] private Sprite icon;

        public ItemLevelEntry(string displayName, Sprite icon)
        {
            this.displayName = displayName;
            this.icon = icon;
        }

        public string DisplayName => displayName;
        public Sprite Icon => icon;
    }
}
