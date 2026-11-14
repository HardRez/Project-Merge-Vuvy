using System;

namespace MergeGame.Editor.Seed
{
    /// <summary>A customer needs a portrait, an avatar, or both.</summary>
    [Serializable]
    internal sealed class SeedCustomer
    {
        /// <summary>A character folder: <c>Content/Characters/&lt;portrait&gt;/&lt;portrait&gt;_SkeletonData.asset</c>.</summary>
        public string portrait;

        /// <summary>An icon file name without extension: <c>Sprites/Icons/&lt;avatar&gt;.png</c>.</summary>
        public string avatar;
    }
}
