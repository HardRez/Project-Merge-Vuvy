using System;

namespace MergeGame.Editor.Seed
{
    [Serializable]
    internal sealed class SeedLevel
    {
        public string name;

        /// <summary>Art reference for the placeholder sprite generator (<c>Tools/PlaceholderArt</c>); not used by the game.</summary>
        public string emoji;

        /// <summary>Art reference for the asset pack importer (<c>Tools/PackArt</c>); not used by the game.</summary>
        public string source;
    }
}
