namespace MergeGame.Presentation.Cheats
{
    /// <summary>The player's saved game, kept between launches by the app.</summary>
    public interface IGameProgress
    {
        /// <summary>
        /// Forgets the saved game and stops saving the one being played, so the next game that opens starts from the
        /// beginning. The player's settings are kept.
        /// </summary>
        void ResetProgress();
    }
}
