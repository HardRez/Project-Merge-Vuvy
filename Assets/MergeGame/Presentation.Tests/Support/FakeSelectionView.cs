using System;
using MergeGame.Core.Grid;
using MergeGame.Presentation.Selection;

namespace MergeGame.Presentation.Tests.Support
{
    internal sealed class FakeSelectionView : IBoardSelectionView
    {
        private readonly ViewLog log;

        public FakeSelectionView(ViewLog log)
        {
            this.log = log;
        }

        public event Action<GridPosition> CellTapped;

        public GridPosition? Marker { get; private set; }
        public bool HasInputListeners => CellTapped != null;

        public void Tap(int x, int y) => CellTapped?.Invoke(new GridPosition(x, y));

        public void ShowSelectionMarker(GridPosition position)
        {
            Marker = position;
            log.Add($"Selection.ShowSelectionMarker {position}");
        }

        public void HideSelectionMarker()
        {
            Marker = null;
            log.Add("Selection.HideSelectionMarker");
        }
    }
}
