using System;

namespace MergeGame.Editor.Seed
{
    /// <summary>Shape of <c>seed_content.json</c>, read with <see cref="UnityEngine.JsonUtility"/>; field names are the JSON keys.</summary>
    [Serializable]
    internal sealed class SeedContent
    {
        public SeedBoard board;
        public int[] scoreTable;
        public SeedChain[] chains;
        public SeedOrders orders;
        public SeedPlacement[] initialLayout;
    }
}
