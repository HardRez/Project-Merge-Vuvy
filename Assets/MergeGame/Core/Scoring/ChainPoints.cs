using System;

namespace MergeGame.Core.Scoring
{
    /// <summary>The one place that turns table points and a chain multiplier into whole points, so merges and orders round alike.</summary>
    internal static class ChainPoints
    {
        public static int Scale(int points, float multiplier) =>
            (int)Math.Round(points * (double)multiplier, MidpointRounding.AwayFromZero);
    }
}
