using MergeGame.Core.Grid;
using UnityEngine;

namespace MergeGame.View.Board
{
    /// <summary>
    /// Maps grid cells to normalised rects of the grid area and back. Row y = 0 is the top row, while uGUI's
    /// normalised space grows upwards, hence the flipped y.
    /// </summary>
    public readonly struct BoardGeometry
    {
        public BoardGeometry(BoardSize size)
        {
            Size = size;
        }

        public BoardSize Size { get; }

        /// <summary>Width over height of the whole grid when cells are square.</summary>
        public float AspectRatio => (float)Size.Width / Size.Height;

        public CellRect GetCell(GridPosition position)
        {
            var cellSize = new Vector2(1f / Size.Width, 1f / Size.Height);
            var min = new Vector2(position.X * cellSize.x, 1f - (position.Y + 1) * cellSize.y);
            return new CellRect(min, min + cellSize);
        }

        /// <returns>The cell containing the normalised point, or null when the point is outside the grid.</returns>
        public GridPosition? FindCell(Vector2 normalizedPoint)
        {
            if (normalizedPoint.x < 0f || normalizedPoint.x >= 1f || normalizedPoint.y <= 0f || normalizedPoint.y > 1f)
                return null;

            var x = Mathf.Min(Mathf.FloorToInt(normalizedPoint.x * Size.Width), Size.Width - 1);
            var y = Mathf.Min(Mathf.FloorToInt((1f - normalizedPoint.y) * Size.Height), Size.Height - 1);
            return new GridPosition(x, y);
        }
    }
}
