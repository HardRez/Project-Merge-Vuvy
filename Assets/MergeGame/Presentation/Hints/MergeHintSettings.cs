using System;

namespace MergeGame.Presentation.Hints
{
    public sealed class MergeHintSettings
    {
        /// <param name="idleSeconds">How long the player must be idle before a merge is suggested.</param>
        public MergeHintSettings(float idleSeconds)
        {
            if (float.IsNaN(idleSeconds) || float.IsInfinity(idleSeconds) || idleSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(idleSeconds), idleSeconds, "The idle delay before a merge hint must be a positive number of seconds.");

            IdleSeconds = idleSeconds;
        }

        public float IdleSeconds { get; }
    }
}
