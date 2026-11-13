using System;
using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Spawning
{
    /// <summary>Picks a level-1 item of a chain chosen by <see cref="ItemChainDefinition.SpawnWeight"/>, placed in a uniformly random empty cell.</summary>
    public sealed class WeightedRandomSpawnPolicy : ISpawnPolicy
    {
        private readonly ItemChainDefinition[] spawnableChains;
        private readonly int totalWeight;
        private readonly List<GridPosition> emptyCellsBuffer = new List<GridPosition>();

        public WeightedRandomSpawnPolicy(ItemCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            var spawnable = new List<ItemChainDefinition>();
            foreach (var chain in catalog.Chains)
            {
                if (!chain.IsSpawnable)
                    continue;
                spawnable.Add(chain);
                totalWeight += chain.SpawnWeight;
            }

            if (spawnable.Count == 0)
                throw new ArgumentException("At least one chain needs a positive spawn weight to be spawnable.", nameof(catalog));

            spawnableChains = spawnable.ToArray();
        }

        public SpawnPlacement Choose(IReadOnlyBoard board, IRandom random)
        {
            var item = ChooseChain(random).ItemAtLevel(1);
            return new SpawnPlacement(item, ChooseEmptyCell(board, random));
        }

        private ItemChainDefinition ChooseChain(IRandom random)
        {
            var roll = random.Next(totalWeight);
            foreach (var chain in spawnableChains)
            {
                if (roll < chain.SpawnWeight)
                    return chain;
                roll -= chain.SpawnWeight;
            }

            throw new InvalidOperationException($"Random roll exceeded the total spawn weight {totalWeight}.");
        }

        private GridPosition ChooseEmptyCell(IReadOnlyBoard board, IRandom random)
        {
            emptyCellsBuffer.Clear();
            foreach (var position in board.Positions)
            {
                if (board.IsEmpty(position))
                    emptyCellsBuffer.Add(position);
            }

            if (emptyCellsBuffer.Count == 0)
                throw new InvalidOperationException("Cannot choose a spawn cell on a full board.");

            return emptyCellsBuffer[random.Next(emptyCellsBuffer.Count)];
        }
    }
}
