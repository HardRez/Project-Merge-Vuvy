using System;
using MergeGame.Presentation.Spawning;

namespace MergeGame.Presentation.Tests.Support
{
    internal sealed class FakeSpawnView : ISpawnView
    {
        private readonly ViewLog log;

        public FakeSpawnView(ViewLog log)
        {
            this.log = log;
        }

        public event Action SpawnRequested;

        public bool? IsAvailable { get; private set; }
        public bool HasInputListeners => SpawnRequested != null;

        public void PressSpawn() => SpawnRequested?.Invoke();

        public void SetSpawnAvailable(bool isAvailable)
        {
            IsAvailable = isAvailable;
            log.Add($"Spawn.SetSpawnAvailable {isAvailable}");
        }
    }
}
