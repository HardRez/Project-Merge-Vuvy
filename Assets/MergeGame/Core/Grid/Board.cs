using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MergeGame.Core.Grid
{
    /// <summary>
    /// The single source of truth for grid state. Internal so that only the session (and the rules it applies)
    /// can mutate it; everything outside Core sees it through <see cref="IReadOnlyBoard"/>.
    /// </summary>
    internal sealed class Board : IReadOnlyBoard
    {
        private readonly BoardItem[] cells;
        private readonly ReadOnlyCollection<GridPosition> positions;

        public Board(BoardSize size)
        {
            Size = size;
            cells = new BoardItem[size.CellCount];
            EmptyCellCount = size.CellCount;

            var all = new GridPosition[size.CellCount];
            for (var y = 0; y < size.Height; y++)
                for (var x = 0; x < size.Width; x++)
                    all[ToIndex(x, y)] = new GridPosition(x, y);
            positions = Array.AsReadOnly(all);
        }

        public BoardSize Size { get; }
        public int EmptyCellCount { get; private set; }
        public bool IsFull => EmptyCellCount == 0;
        public IReadOnlyList<GridPosition> Positions => positions;

        public bool IsInside(GridPosition position) => Size.Contains(position);

        public bool IsEmpty(GridPosition position) => cells[IndexOf(position)] == null;

        public BoardItem GetItem(GridPosition position)
        {
            var item = cells[IndexOf(position)];
            if (item == null)
                throw new InvalidOperationException($"Cell {position} is empty.");
            return item;
        }

        public bool TryGetItem(GridPosition position, out BoardItem item)
        {
            item = cells[IndexOf(position)];
            return item != null;
        }

        public void Place(GridPosition position, BoardItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var index = IndexOf(position);
            if (cells[index] != null)
                throw new InvalidOperationException($"Cell {position} is already occupied by {cells[index]}.");

            cells[index] = item;
            EmptyCellCount--;
        }

        public BoardItem Remove(GridPosition position)
        {
            var item = GetItem(position);
            cells[IndexOf(position)] = null;
            EmptyCellCount++;
            return item;
        }

        public void Replace(GridPosition position, BoardItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            GetItem(position);
            cells[IndexOf(position)] = item;
        }

        public void Move(GridPosition from, GridPosition to)
        {
            if (!IsEmpty(to))
                throw new InvalidOperationException($"Cannot move onto occupied cell {to}.");

            Place(to, Remove(from));
        }

        private int IndexOf(GridPosition position)
        {
            if (!Size.Contains(position))
                throw new ArgumentOutOfRangeException(nameof(position), position, $"Position is outside the {Size} board.");
            return ToIndex(position.X, position.Y);
        }

        private int ToIndex(int x, int y) => y * Size.Width + x;
    }
}
