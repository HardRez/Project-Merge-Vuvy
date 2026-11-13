namespace MergeGame.Core.Spawning
{
    /// <summary>The only source of randomness in Core, so games can be replayed and tested deterministically.</summary>
    public interface IRandom
    {
        /// <returns>A value in [0, <paramref name="maxExclusive"/>).</returns>
        int Next(int maxExclusive);
    }
}
