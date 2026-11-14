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

        [Tooltip("A closed box: its item stays unknown until a webbed item orthogonally next to it is freed, then it appears " +
                 "webbed. A box is always locked, whatever the Locked box says, and must touch a webbed item (directly or " +
                 "through other boxes).")]
        [SerializeField] private bool hidden;

        public InitialPlacementEntry(int x, int y, ItemChainAsset chain, int level, bool locked, bool hidden = false)
        {
            this.x = x;
            this.y = y;
            this.chain = chain;
            this.level = level;
            this.locked = locked;
            this.hidden = hidden;
        }

        public int X => x;
        public int Y => y;
        public ItemChainAsset Chain => chain;
        public int Level => level;
        /// <summary>True for webbed items and for boxes: a box is always locked.</summary>
        public bool Locked => locked || hidden;

        public bool Hidden => hidden;
    }
}
