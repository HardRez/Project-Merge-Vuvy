using System;

namespace MergeGame.Presentation.Spawning
{
    public interface ISpawnView
    {
        event Action SpawnRequested;

        void SetSpawnAvailable(bool isAvailable);
    }
}
