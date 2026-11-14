using MergeGame.Core.Grid;

namespace MergeGame.Presentation.Hints
{
    public interface IMergeHintView
    {
        /// <summary>Suggests dragging the item on <paramref name="from"/> onto <paramref name="to"/> until <see cref="HideMergeHint"/>.</summary>
        void ShowMergeHint(GridPosition from, GridPosition to);

        void HideMergeHint();
    }
}
