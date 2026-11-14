using System;

namespace MergeGame.Presentation.Spawning
{
    /// <summary>The button that produces a new item.</summary>
    public interface ISpawnView
    {
        event Action SpawnRequested;

        void SetSpawnAvailable(bool isAvailable);
    }
}
