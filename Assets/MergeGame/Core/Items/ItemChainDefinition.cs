using System;
using System.Collections.Generic;

namespace MergeGame.Core.Items
{
    public sealed class ItemChainDefinition
    {
        private readonly ItemLevelDefinition[] levels;

        /// <param name="spawnWeight">Relative chance of being picked by the spawn button; 0 means never spawned.</param>
        /// <param name="scoreMultiplier">Applied to the score table points of every merge in this chain.</param>
        public ItemChainDefinition(string id, IEnumerable<ItemLevelDefinition> levels, int spawnWeight, float scoreMultiplier)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Chain id must not be empty.", nameof(id));
            if (levels == null)
                throw new ArgumentNullException(nameof(levels));
            if (spawnWeight < 0)
                throw new ArgumentOutOfRangeException(nameof(spawnWeight), spawnWeight, $"Chain '{id}' has a negative spawn weight.");
            if (float.IsNaN(scoreMultiplier) || float.IsInfinity(scoreMultiplier) || scoreMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(scoreMultiplier), scoreMultiplier, $"Chain '{id}' needs a positive score multiplier.");

            Id = id;
            SpawnWeight = spawnWeight;
            ScoreMultiplier = scoreMultiplier;
            this.levels = SortAndValidateLevels(id, levels);
        }

        public string Id { get; }
        public int MaxLevel => levels.Length;
        public IReadOnlyList<ItemLevelDefinition> Levels => levels;
        public int SpawnWeight { get; }
        public bool IsSpawnable => SpawnWeight > 0;
        public float ScoreMultiplier { get; }

        public bool HasLevel(int level) => level >= 1 && level <= MaxLevel;

        public ItemLevelDefinition GetLevel(int level)
        {
            if (!HasLevel(level))
                throw new ArgumentOutOfRangeException(nameof(level), level, $"Chain '{Id}' has levels 1..{MaxLevel}.");
            return levels[level - 1];
        }

        public ItemId ItemAtLevel(int level)
        {
            if (!HasLevel(level))
                throw new ArgumentOutOfRangeException(nameof(level), level, $"Chain '{Id}' has levels 1..{MaxLevel}.");
            return new ItemId(Id, level);
        }

        private static ItemLevelDefinition[] SortAndValidateLevels(string chainId, IEnumerable<ItemLevelDefinition> source)
        {
            var sorted = new List<ItemLevelDefinition>();
            foreach (var level in source)
            {
                if (level == null)
                    throw new ArgumentException($"Chain '{chainId}' contains a null level.", nameof(source));
                sorted.Add(level);
            }

            if (sorted.Count == 0)
                throw new ArgumentException($"Chain '{chainId}' must define at least one level.", "levels");

            sorted.Sort((a, b) => a.Level.CompareTo(b.Level));
            for (var i = 0; i < sorted.Count; i++)
            {
                var expected = i + 1;
                if (sorted[i].Level != expected)
                    throw new ArgumentException(
                        $"Chain '{chainId}' levels must be contiguous from 1; expected level {expected} but found {sorted[i].Level}.", "levels");
            }

            return sorted.ToArray();
        }
    }
}
