using System;
using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Presentation.Board;

namespace MergeGame.Presentation.Tests.Support
{
    /// <summary>Records calls and keeps a model of the items it shows, so tests can compare it with the session's board.</summary>
    internal sealed class FakeBoardView : IBoardView
    {
        private readonly ViewLog log;

        public FakeBoardView(ViewLog log)
        {
            this.log = log;
        }

        public event Action<GridPosition> DragStarted;
        public event Action<GridPosition?> DragMoved;
        public event Action<GridPosition?> DragReleased;

        public BoardSize? GridSize { get; private set; }
        public Dictionary<GridPosition, ShownItem> Items { get; } = new Dictionary<GridPosition, ShownItem>();
        public GridPosition? DraggedItem { get; private set; }
        public GridPosition? HighlightedCell { get; private set; }
        public DropHighlight? Highlight { get; private set; }
        public List<GridPosition> RefusedDrags { get; } = new List<GridPosition>();
        public List<GridPosition> ReturnedItems { get; } = new List<GridPosition>();
        public List<(GridPosition cell, int points)> PointsPopups { get; } = new List<(GridPosition, int)>();

        public List<(GridPosition from, GridPosition to, ItemId result, bool unlockedTarget)> Merges { get; } =
            new List<(GridPosition, GridPosition, ItemId, bool)>();

        public bool HasInputListeners => DragStarted != null || DragMoved != null || DragReleased != null;

        public void StartDrag(int x, int y) => DragStarted?.Invoke(new GridPosition(x, y));
        public void MoveFingerTo(int x, int y) => DragMoved?.Invoke(new GridPosition(x, y));
        public void MoveFingerOffBoard() => DragMoved?.Invoke(null);
        public void Release(int x, int y) => DragReleased?.Invoke(new GridPosition(x, y));
        public void ReleaseOffBoard() => DragReleased?.Invoke(null);

        public void DragAndDrop(int fromX, int fromY, int toX, int toY)
        {
            StartDrag(fromX, fromY);
            MoveFingerTo(toX, toY);
            Release(toX, toY);
        }

        public void CreateGrid(BoardSize size)
        {
            GridSize = size;
            log.Add($"Board.CreateGrid {size}");
        }

        public void PlaceItem(GridPosition position, ItemId itemId, bool isLocked)
        {
            Items.Add(position, new ShownItem(itemId, isLocked));
            log.Add($"Board.PlaceItem {itemId} at {position}{(isLocked ? " locked" : string.Empty)}");
        }

        public void SpawnItem(GridPosition position, ItemId itemId)
        {
            Items.Add(position, new ShownItem(itemId, false));
            log.Add($"Board.SpawnItem {itemId} at {position}");
        }

        public void BeginDrag(GridPosition origin)
        {
            DraggedItem = origin;
            log.Add($"Board.BeginDrag {origin}");
        }

        public void RefuseDrag(GridPosition origin)
        {
            RefusedDrags.Add(origin);
            log.Add($"Board.RefuseDrag {origin}");
        }

        public void ShowDropHighlight(GridPosition cell, DropHighlight highlight)
        {
            HighlightedCell = cell;
            Highlight = highlight;
            log.Add($"Board.ShowDropHighlight {highlight} at {cell}");
        }

        public void ClearDropHighlight()
        {
            HighlightedCell = null;
            Highlight = null;
            log.Add("Board.ClearDropHighlight");
        }

        public void ReturnItem(GridPosition origin)
        {
            DraggedItem = null;
            ReturnedItems.Add(origin);
            log.Add($"Board.ReturnItem {origin}");
        }

        public void MoveItem(GridPosition from, GridPosition to)
        {
            DraggedItem = null;
            var item = Items[from];
            Items.Remove(from);
            Items.Add(to, item);
            log.Add($"Board.MoveItem {from} -> {to}");
        }

        public void MergeItems(GridPosition from, GridPosition to, ItemId resultItemId, bool unlockedTarget)
        {
            DraggedItem = null;
            Items.Remove(from);
            Items[to] = new ShownItem(resultItemId, false);
            Merges.Add((from, to, resultItemId, unlockedTarget));
            log.Add($"Board.MergeItems {from} -> {to} = {resultItemId}{(unlockedTarget ? " unlocked" : string.Empty)}");
        }

        public void ShowPointsPopup(GridPosition cell, int points)
        {
            PointsPopups.Add((cell, points));
            log.Add($"Board.ShowPointsPopup +{points} at {cell}");
        }

        public void DiscardItem(GridPosition position)
        {
            RemoveShown(position);
            log.Add($"Board.DiscardItem {position}");
        }

        public void DeliverItems(int orderSlotIndex, IReadOnlyList<GridPosition> positions)
        {
            foreach (var position in positions)
                RemoveShown(position);
            log.Add($"Board.DeliverItems to slot {orderSlotIndex}: {string.Join(", ", positions)}");
        }

        private void RemoveShown(GridPosition position)
        {
            if (DraggedItem == position)
                DraggedItem = null;
            Items.Remove(position);
        }
    }
}
