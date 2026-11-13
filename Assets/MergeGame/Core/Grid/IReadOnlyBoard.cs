using System.Collections.Generic;

namespace MergeGame.Core.Grid
{
    public interface IReadOnlyBoard
    {
        BoardSize Size { get; }
        int EmptyCellCount { get; }
        bool IsFull { get; }

        /// <summary>Every cell position in row-major order (y outer, x inner).</summary>
        IReadOnlyList<GridPosition> Positions { get; }

        bool IsInside(GridPosition position);
        bool IsEmpty(GridPosition position);

        /// <exception cref="System.InvalidOperationException">The cell is empty; use <see cref="TryGetItem"/> when that is expected.</exception>
        BoardItem GetItem(GridPosition position);

        bool TryGetItem(GridPosition position, out BoardItem item);
    }
}
