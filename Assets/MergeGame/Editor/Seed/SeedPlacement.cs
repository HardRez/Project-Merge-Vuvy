using System;

namespace MergeGame.Editor.Seed
{
    [Serializable]
    internal sealed class SeedPlacement
    {
        public int x;
        public int y;
        public string chain;
        public int level;
        public bool locked;
    }
}
