using System;
using System.Collections.Generic;
using MergeGame.Core.Spawning;

namespace MergeGame.Core.Tests.Support
{
    /// <summary>Returns the given rolls in order; defaults to 0 once they run out.</summary>
    internal sealed class ScriptedRandom : IRandom
    {
        private readonly Queue<int> rolls;

        public ScriptedRandom(params int[] rolls)
        {
            this.rolls = new Queue<int>(rolls);
        }

        public int Next(int maxExclusive)
        {
            var roll = rolls.Count > 0 ? rolls.Dequeue() : 0;
            if (roll < 0 || roll >= maxExclusive)
                throw new InvalidOperationException($"Scripted roll {roll} is outside [0, {maxExclusive}).");
            return roll;
        }
    }
}
