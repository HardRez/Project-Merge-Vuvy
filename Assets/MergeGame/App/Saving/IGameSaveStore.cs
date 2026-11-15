using MergeGame.Core.Session;

namespace MergeGame.App.Saving
{
    /// <summary>Where the saved game lives between launches.</summary>
    public interface IGameSaveStore
    {
        /// <summary>False when there is no save or it cannot be read; the game then starts from the beginning.</summary>
        bool TryLoad(out GameSnapshot snapshot);

        /// <summary>Replaces the save. A failed write is logged, not thrown, and the previous save is kept.</summary>
        void Save(GameSnapshot snapshot);

        /// <summary>Removes the save, so the next game starts from the beginning.</summary>
        void Delete();
    }
}
