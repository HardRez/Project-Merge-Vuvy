using System;
using UnityEngine;

namespace MergeGame.Data.Authoring
{
    /// <summary>One item of a <see cref="BoardLayoutAsset"/>.</summary>
    [Serializable]
    public sealed class InitialPlacementEntry
    {
        [Tooltip("0-based column.")]
        [Min(0)]
        [SerializeField] private int x;

        [Tooltip("0-based row; row 0 is the first row of the board (row-major order).")]
        [Min(0)]
        [SerializeField] private int y;

        [Tooltip("Must be one of the chains listed in the game config.")]
        [SerializeField] private ItemChainAsset chain;

        [Tooltip("1-based level within the chain.")]
        [Min(1)]
        [SerializeField] private int level = 1;

        [Tooltip("A locked (webbed) item cannot be moved, discarded or delivered; merging a matching free item onto it frees it.")]
        [SerializeField] private bool locked;

        public InitialPlacementEntry(int x, int y, ItemChainAsset chain, int level, bool locked)
        {
            this.x = x;
            this.y = y;
            this.chain = chain;
            this.level = level;
            this.locked = locked;
        }

        public int X => x;
        public int Y => y;
        public ItemChainAsset Chain => chain;
        public int Level => level;
        public bool Locked => locked;
    }
}
