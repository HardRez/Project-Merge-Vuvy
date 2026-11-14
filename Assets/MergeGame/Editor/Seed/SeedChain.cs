using System;

namespace MergeGame.Editor.Seed
{
    [Serializable]
    internal sealed class SeedChain
    {
        public string id;
        public string displayName;
        public int spawnWeight;
        public float scoreMultiplier;
        public SeedLevel[] levels;
    }
}
