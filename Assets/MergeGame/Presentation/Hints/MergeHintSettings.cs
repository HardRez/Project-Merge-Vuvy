using System;

namespace MergeGame.Presentation.Hints
{
    public sealed class MergeHintSettings
    {
        /// <param name="idleSeconds">How long the player must be idle before the first hint plays.</param>
        /// <param name="repeatSeconds">How long after each check the next one happens while the player stays idle.</param>
        public MergeHintSettings(float idleSeconds, float repeatSeconds)
        {
            IdleSeconds = RequirePositive(idleSeconds, nameof(idleSeconds));
            RepeatSeconds = RequirePositive(repeatSeconds, nameof(repeatSeconds));
        }

        public float IdleSeconds { get; }
        public float RepeatSeconds { get; }

        private static float RequirePositive(float seconds, string parameterName)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f)
                throw new ArgumentOutOfRangeException(parameterName, seconds, "Merge hint timings must be a positive number of seconds.");
            return seconds;
        }
    }
}
