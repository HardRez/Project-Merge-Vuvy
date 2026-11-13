using System;

namespace MergeGame.Core.Items
{
    public sealed class ItemLevelDefinition
    {
        public ItemLevelDefinition(int level, string displayName)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level), level, "Item levels start at 1.");
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException($"Level {level} needs a display name.", nameof(displayName));

            Level = level;
            DisplayName = displayName;
        }

        public int Level { get; }
        public string DisplayName { get; }
    }
}
