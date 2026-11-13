using MergeGame.Core.Grid;

namespace MergeGame.Core.Spawning
{
    public interface ISpawnPolicy
    {
        /// <summary>Chooses what to spawn and where. Only called when the board has at least one empty cell.</summary>
        SpawnPlacement Choose(IReadOnlyBoard board, IRandom random);
    }
}
