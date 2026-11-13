using System;

namespace MergeGame.Core.Spawning
{
    public sealed class SpawnResult
    {
        private readonly SpawnPlacement placement;

        private SpawnResult(bool succeeded, SpawnPlacement placement, SpawnFailureReason failureReason)
        {
            Succeeded = succeeded;
            this.placement = placement;
            FailureReason = failureReason;
        }

        public bool Succeeded { get; }
        public SpawnFailureReason FailureReason { get; }

        /// <exception cref="InvalidOperationException">The spawn failed.</exception>
        public SpawnPlacement Placement =>
            Succeeded ? placement : throw new InvalidOperationException($"A failed spawn ({FailureReason}) has no placement.");

        public static SpawnResult Success(SpawnPlacement placement) => new SpawnResult(true, placement, SpawnFailureReason.None);

        public static SpawnResult Failure(SpawnFailureReason reason)
        {
            if (reason == SpawnFailureReason.None)
                throw new ArgumentException("A failed spawn needs a reason.", nameof(reason));
            return new SpawnResult(false, default, reason);
        }

        public override string ToString() => Succeeded ? $"Spawned {placement}" : $"Spawn failed ({FailureReason})";
    }
}
