using System;
using Spine.Unity;
using UnityEngine;

namespace MergeGame.Data.Authoring
{
    /// <summary>One customer who places orders, shown as an animated Spine portrait, a still avatar, or both.</summary>
    [Serializable]
    public sealed class CustomerEntry
    {
        [Tooltip("Animated portrait (Spine 3.8). Its animations are found by name suffix; see CustomerAnimationSet.")]
        [SerializeField] private SkeletonDataAsset portrait;

        [Tooltip("Still image, shown when the customer has no portrait. Must be imported as a Sprite.")]
        [SerializeField] private Sprite avatar;

        public CustomerEntry(SkeletonDataAsset portrait, Sprite avatar)
        {
            this.portrait = portrait;
            this.avatar = avatar;
        }

        public SkeletonDataAsset Portrait => portrait;
        public Sprite Avatar => avatar;
    }
}
