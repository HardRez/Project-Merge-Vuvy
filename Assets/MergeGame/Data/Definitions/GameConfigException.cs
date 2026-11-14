using System;

namespace MergeGame.Data.Definitions
{
    /// <summary>A game config asset cannot be turned into a playable definition; the message names the asset and the problem.</summary>
    public sealed class GameConfigException : Exception
    {
        public GameConfigException(string message)
            : base(message)
        {
        }

        public GameConfigException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
