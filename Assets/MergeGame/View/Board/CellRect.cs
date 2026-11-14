using UnityEngine;

namespace MergeGame.View.Board
{
    /// <summary>A cell's area in the normalised space of the grid rect (0..1, origin bottom-left as in uGUI).</summary>
    public readonly struct CellRect
    {
        public CellRect(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        public Vector2 Min { get; }
        public Vector2 Max { get; }
        public Vector2 Center => (Min + Max) * 0.5f;
    }
}
