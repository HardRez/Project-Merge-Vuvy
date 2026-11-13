using System;

namespace MergeGame.Core.Spawning
{
    public sealed class SystemRandom : IRandom
    {
        private readonly Random random;

        public SystemRandom(int seed)
        {
            random = new Random(seed);
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive < 1)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Upper bound must be at least 1.");
            return random.Next(maxExclusive);
        }
    }
}
