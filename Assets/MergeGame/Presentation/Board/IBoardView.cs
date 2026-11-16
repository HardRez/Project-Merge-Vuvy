using System;
using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Presentation.Board
{
    /// <summary>
    /// The grid of cells and the items on it. Pointer input arrives already converted to cells; a null cell means the
    /// pointer is outside the board. A drag only lifts the item once <see cref="BeginDrag"/> is called.
    /// </summary>
    public interface IBoardView
    {
        event Action<GridPosition> DragStarted;

        /// <summary>The cell under the finger changed during a drag.</summary>
        event Action<GridPosition?> DragMoved;

        event Action<GridPosition?> DragReleased;

        void CreateGrid(BoardSize size);
        void PlaceItem(GridPosition position, ItemId itemId, bool isLocked);

        /// <summary>A sand-covered cell at the start; what is inside stays unknown until <see cref="RevealItem"/>.</summary>
        void PlaceHiddenItem(GridPosition position);

        /// <summary>The sand clears and a locked item appears, after the merge that freed its neighbour was shown.</summary>
        void RevealItem(GridPosition position, ItemId itemId);

        void SpawnItem(GridPosition position, ItemId itemId);

        void BeginDrag(GridPosition origin);

        /// <summary>The item cannot be picked up (it is locked); e.g. shake it.</summary>
        void RefuseDrag(GridPosition origin);

        void ShowDropHighlight(GridPosition cell, DropHighlight highlight);
        void ClearDropHighlight();

        /// <summary>The dragged item goes back to the cell it was picked up from.</summary>
        void ReturnItem(GridPosition origin);

        void MoveItem(GridPosition from, GridPosition to);

        /// <summary>Both items are consumed and <paramref name="resultItemId"/> appears, unlocked, on <paramref name="to"/>.</summary>
        /// <param name="unlockedTarget">The item on <paramref name="to"/> was locked; play the unlock effect.</param>
        void MergeItems(GridPosition from, GridPosition to, ItemId resultItemId, bool unlockedTarget);

        void ShowPointsPopup(GridPosition cell, int points);
        void DiscardItem(GridPosition position);

        /// <summary>The items leave the board towards the customer of the order slot. May include the item being dragged.</summary>
        void DeliverItems(int orderSlotIndex, IReadOnlyList<GridPosition> positions);
    }
}
