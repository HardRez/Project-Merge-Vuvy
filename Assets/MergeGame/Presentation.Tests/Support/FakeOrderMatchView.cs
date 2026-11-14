using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Presentation.Orders;

namespace MergeGame.Presentation.Tests.Support
{
    /// <summary>
    /// Records every set it receives. It keeps its own record instead of writing to the shared <see cref="ViewLog"/>:
    /// the marks are an overlay independent of the other views, and the full-log assertions stay about those views.
    /// </summary>
    internal sealed class FakeOrderMatchView : IOrderMatchView
    {
        public List<GridPosition[]> SentSets { get; } = new List<GridPosition[]>();

        /// <summary>The last set received, or null before the first.</summary>
        public GridPosition[] Marked => SentSets.Count == 0 ? null : SentSets[SentSets.Count - 1];

        public void ShowOrderMatches(IReadOnlyCollection<GridPosition> cells) => SentSets.Add(new List<GridPosition>(cells).ToArray());
    }
}
