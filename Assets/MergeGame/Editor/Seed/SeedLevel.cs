using System;

namespace MergeGame.Editor.Seed
{
    [Serializable]
    internal sealed class SeedLevel
    {
        public string name;

        /// <summary>Art reference for the placeholder sprite generator; not used by the game.</summary>
        public string emoji;
    }
}
