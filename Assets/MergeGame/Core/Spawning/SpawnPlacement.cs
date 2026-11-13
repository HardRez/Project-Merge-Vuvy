using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Spawning
{
    public readonly struct SpawnPlacement
    {
        public SpawnPlacement(ItemId itemId, GridPosition position)
        {
            ItemId = itemId;
            Position = position;
        }

        public ItemId ItemId { get; }
        public GridPosition Position { get; }

        public override string ToString() => $"{ItemId} at {Position}";
    }
}
