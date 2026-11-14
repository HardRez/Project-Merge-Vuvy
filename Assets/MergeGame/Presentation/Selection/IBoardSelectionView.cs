using System;
using MergeGame.Core.Grid;

namespace MergeGame.Presentation.Selection
{
    /// <summary>Taps on the grid and the selection marker; usually implemented by the same component as the board view.</summary>
    public interface IBoardSelectionView
    {
        /// <summary>A press and release on one cell without dragging.</summary>
        event Action<GridPosition> CellTapped;

        void ShowSelectionMarker(GridPosition position);
        void HideSelectionMarker();
    }
}
