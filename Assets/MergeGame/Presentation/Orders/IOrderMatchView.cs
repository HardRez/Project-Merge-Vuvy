using System.Collections.Generic;
using MergeGame.Core.Grid;

namespace MergeGame.Presentation.Orders
{
    /// <summary>Marks the board items that currently count toward an active order (e.g. a green check).</summary>
    public interface IOrderMatchView
    {
        /// <summary>The complete set of marked cells in row-major order; it replaces the previous set.</summary>
        void ShowOrderMatches(IReadOnlyCollection<GridPosition> cells);
    }
}
