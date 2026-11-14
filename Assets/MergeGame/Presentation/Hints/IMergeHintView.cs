using MergeGame.Core.Grid;

namespace MergeGame.Presentation.Hints
{
    public interface IMergeHintView
    {
        /// <summary>
        /// Plays the hint once on these two items (suggesting to drag <paramref name="from"/> onto <paramref name="to"/>)
        /// and returns them to rest by itself.
        /// </summary>
        void ShowMergeHint(GridPosition from, GridPosition to);

        /// <summary>Cancels a hint that may still be playing; does nothing when none is.</summary>
        void HideMergeHint();
    }
}
