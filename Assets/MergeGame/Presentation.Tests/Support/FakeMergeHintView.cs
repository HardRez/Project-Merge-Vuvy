using MergeGame.Core.Grid;
using MergeGame.Presentation.Hints;

namespace MergeGame.Presentation.Tests.Support
{
    internal sealed class FakeMergeHintView : IMergeHintView
    {
        private readonly ViewLog log;

        public FakeMergeHintView(ViewLog log)
        {
            this.log = log;
        }

        public (GridPosition from, GridPosition to)? Shown { get; private set; }

        public int ShowCount { get; private set; }
        public int HideCount { get; private set; }

        public void ShowMergeHint(GridPosition from, GridPosition to)
        {
            Shown = (from, to);
            ShowCount++;
            log.Add($"Hint.ShowMergeHint {from} -> {to}");
        }

        public void HideMergeHint()
        {
            Shown = null;
            HideCount++;
            log.Add("Hint.HideMergeHint");
        }
    }
}
