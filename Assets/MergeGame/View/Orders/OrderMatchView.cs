using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Presentation.Orders;
using MergeGame.View.Board;
using UnityEngine;

namespace MergeGame.View.Orders
{
    /// <summary>
    /// Marks the board items a customer is waiting for. The mark belongs to the item view, so a moved or dragged item
    /// keeps it without a flicker; each new set only switches marks on items whose status changed.
    /// </summary>
    public sealed class OrderMatchView : MonoBehaviour, IOrderMatchView
    {
        [SerializeField] private BoardView board;

        private readonly HashSet<ItemView> marked = new HashSet<ItemView>();
        private readonly HashSet<ItemView> wanted = new HashSet<ItemView>();

        public void ShowOrderMatches(IReadOnlyCollection<GridPosition> cells)
        {
            wanted.Clear();
            foreach (var cell in cells)
            {
                if (board.TryGetItem(cell, out var item))
                    wanted.Add(item);
            }

            // Marked items that left the board (merged away, delivered, discarded) are no longer wanted either.
            foreach (var item in marked)
            {
                if (!wanted.Contains(item))
                    item.OrderMark.Hide();
            }

            foreach (var item in wanted)
                item.OrderMark.Show();

            marked.Clear();
            marked.UnionWith(wanted);
        }
    }
}
