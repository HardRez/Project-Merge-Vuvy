using System;
using System.Collections.Generic;
using MergeGame.Core.Items;

namespace MergeGame.Core.Scoring
{
    /// <summary>Points for producing an item of a given level. Entry 0 is level 1, entry 1 is level 2, and so on.</summary>
    public sealed class ScoreTable
    {
        private readonly int[] pointsByLevel;

        public ScoreTable(IEnumerable<int> pointsByLevel)
        {
            if (pointsByLevel == null)
                throw new ArgumentNullException(nameof(pointsByLevel));

            this.pointsByLevel = new List<int>(pointsByLevel).ToArray();
            if (this.pointsByLevel.Length == 0)
                throw new ArgumentException("Score table must define points for at least one level.", nameof(pointsByLevel));

            for (var i = 0; i < this.pointsByLevel.Length; i++)
            {
                var level = i + 1;
                if (this.pointsByLevel[i] < 0)
                    throw new ArgumentException($"Score table has negative points for level {level}.", nameof(pointsByLevel));
                if (i > 0 && this.pointsByLevel[i] < this.pointsByLevel[i - 1])
                    throw new ArgumentException(
                        $"Score table points must not decrease with level; level {level} gives less than level {level - 1}.", nameof(pointsByLevel));
            }
        }

        public int HighestLevel => pointsByLevel.Length;

        public int GetPoints(int level)
        {
            if (level < 1 || level > HighestLevel)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"Score table defines levels 1..{HighestLevel}.");
            return pointsByLevel[level - 1];
        }

        internal void EnsureCovers(ItemCatalog catalog)
        {
            if (catalog.HighestLevel > HighestLevel)
                throw new ArgumentException(
                    $"Score table defines points up to level {HighestLevel}, but the item catalog reaches level {catalog.HighestLevel}.");
        }
    }
}
