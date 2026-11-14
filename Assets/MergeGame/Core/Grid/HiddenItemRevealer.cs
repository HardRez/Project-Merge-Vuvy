using System.Collections.Generic;

namespace MergeGame.Core.Grid
{
    /// <summary>
    /// Opens the boxes next to a freed cell: every hidden item among its four orthogonal neighbours becomes a locked,
    /// visible item. Revealed items stay locked, so nothing opens further until they are freed by a merge themselves.
    /// </summary>
    internal sealed class HiddenItemRevealer
    {
        /// <summary>Row-major order: above, left, right, below.</summary>
        private static readonly (int dx, int dy)[] NeighbourOffsets = { (0, -1), (-1, 0), (1, 0), (0, 1) };

        /// <returns>The revealed cells in row-major order; empty when nothing was hidden next to <paramref name="freedCell"/>.</returns>
        public List<GridPosition> RevealAround(Board board, GridPosition freedCell)
        {
            var revealed = new List<GridPosition>();
            foreach (var (dx, dy) in NeighbourOffsets)
            {
                var neighbour = new GridPosition(freedCell.X + dx, freedCell.Y + dy);
                if (!board.IsInside(neighbour) || !board.TryGetItem(neighbour, out var item) || !item.IsHidden)
                    continue;

                board.Replace(neighbour, new BoardItem(item.ItemId, isLocked: true));
                revealed.Add(neighbour);
            }

            return revealed;
        }
    }
}
