using System.Collections.Generic;
using System.Linq;
using MergeGame.Core.Grid;
using MergeGame.Data.Authoring;
using MergeGame.Data.Definitions;

namespace MergeGame.Data.Validation
{
    /// <summary>
    /// Lists everything wrong with authored content in designer terms, without throwing. Covers what only the assets can
    /// get wrong (empty slots, missing sprites, duplicate ids) plus every rule Core enforces when the definition is built.
    /// </summary>
    public static class GameConfigValidator
    {
        /// <returns>Human-readable problems; empty when the config is ready to play.</returns>
        public static IReadOnlyList<string> Validate(GameConfigAsset config)
        {
            if (config == null)
                return new[] { "No game config is assigned." };

            var problems = new List<string>();
            CheckBoard(config, problems);
            CheckChains(config, problems);
            if (config.ScoreTable == null)
                problems.Add("No score table is assigned.");
            CheckOrders(config.Orders, problems);
            CheckLayout(config, problems);

            // Building after an asset-level problem would only repeat it in Core's words.
            if (problems.Count == 0)
                CheckBuildsDefinition(config, problems);

            CheckIcons(config, problems);
            return problems;
        }

        /// <returns>Problems of a single chain, independent of any config.</returns>
        public static IReadOnlyList<string> ValidateChain(ItemChainAsset chain)
        {
            if (chain == null)
                return new[] { "No chain is assigned." };

            var problems = new List<string>();
            CheckChainStructure(chain, problems);
            CheckChainIcons(chain, problems);
            return problems;
        }

        private static void CheckBoard(GameConfigAsset config, List<string> problems)
        {
            if (config.BoardWidth < 1)
                problems.Add($"Board width must be at least 1 (is {config.BoardWidth}).");
            if (config.BoardHeight < 1)
                problems.Add($"Board height must be at least 1 (is {config.BoardHeight}).");
        }

        private static void CheckChains(GameConfigAsset config, List<string> problems)
        {
            if (config.Chains.Count == 0)
                problems.Add("The config lists no item chains.");

            var slotsById = new Dictionary<string, int>();
            for (var i = 0; i < config.Chains.Count; i++)
            {
                var chain = config.Chains[i];
                if (chain == null)
                {
                    problems.Add($"Chain slot {i} is empty.");
                    continue;
                }

                CheckChainStructure(chain, problems);
                if (string.IsNullOrWhiteSpace(chain.Id))
                    continue;
                if (slotsById.TryGetValue(chain.Id, out var earlier))
                    problems.Add($"Chain slots {earlier} and {i} both use the chain id '{chain.Id}'; ids must be unique.");
                else
                    slotsById.Add(chain.Id, i);
            }
        }

        private static void CheckChainStructure(ItemChainAsset chain, List<string> problems)
        {
            var label = AssetLabels.Chain(chain);
            if (string.IsNullOrWhiteSpace(chain.Id))
                problems.Add($"{label} has no id.");
            if (chain.SpawnWeight < 0)
                problems.Add($"{label} has a negative spawn weight ({chain.SpawnWeight}).");
            if (!(chain.ScoreMultiplier > 0f) || float.IsInfinity(chain.ScoreMultiplier))
                problems.Add($"{label} needs a positive score multiplier (is {chain.ScoreMultiplier}).");
            if (chain.Levels.Count == 0)
                problems.Add($"{label} has no levels.");

            for (var i = 0; i < chain.Levels.Count; i++)
            {
                var entry = chain.Levels[i];
                if (entry == null)
                    problems.Add($"{label} level {i + 1} is empty.");
                else if (string.IsNullOrWhiteSpace(entry.DisplayName))
                    problems.Add($"{label} level {i + 1} has no display name.");
            }
        }

        private static void CheckOrders(OrderSettingsAsset orders, List<string> problems)
        {
            if (orders != null && orders.CustomerCount == 0)
                problems.Add($"Order settings '{orders.name}' have no customer avatars; the number of customers is the number of avatars.");
        }

        private static void CheckLayout(GameConfigAsset config, List<string> problems)
        {
            var layout = config.InitialLayout;
            if (layout == null)
                return;

            var placementsByCell = new Dictionary<GridPosition, int>();
            for (var i = 0; i < layout.Placements.Count; i++)
            {
                var entry = layout.Placements[i];
                var label = $"Initial layout '{layout.name}' placement {i}";
                if (entry == null)
                {
                    problems.Add($"{label} is empty.");
                    continue;
                }

                label += $" at {AssetLabels.Cell(entry)}";
                if (entry.X < 0 || entry.X >= config.BoardWidth || entry.Y < 0 || entry.Y >= config.BoardHeight)
                    problems.Add($"{label} is outside the {config.BoardWidth}x{config.BoardHeight} board.");
                var cell = new GridPosition(entry.X, entry.Y);
                if (placementsByCell.TryGetValue(cell, out var earlier))
                    problems.Add($"{label} uses the same cell as placement {earlier}.");
                else
                    placementsByCell.Add(cell, i);

                if (entry.Chain == null)
                    problems.Add($"{label} has no chain.");
                else if (!config.Chains.Contains(entry.Chain))
                    problems.Add($"{label} uses {AssetLabels.Chain(entry.Chain)}, which is not in the config's chain list.");
                else if (entry.Level < 1 || entry.Level > entry.Chain.MaxLevel)
                    problems.Add($"{label} asks for level {entry.Level}, but {AssetLabels.Chain(entry.Chain)} has levels 1..{entry.Chain.MaxLevel}.");
            }

            CheckBoxesCanOpen(layout, problems);
        }

        /// <summary>
        /// A box opens when a webbed item next to it is freed, and opening reveals another webbed item, so a group of
        /// touching boxes needs at least one webbed (locked, not boxed) neighbour. Same rule as Core's.
        /// </summary>
        private static void CheckBoxesCanOpen(BoardLayoutAsset layout, List<string> problems)
        {
            var byCell = new Dictionary<GridPosition, InitialPlacementEntry>();
            foreach (var entry in layout.Placements)
            {
                if (entry != null && !byCell.ContainsKey(new GridPosition(entry.X, entry.Y)))
                    byCell.Add(new GridPosition(entry.X, entry.Y), entry);
            }

            var visited = new HashSet<GridPosition>();
            foreach (var start in byCell.Where(pair => pair.Value.Hidden).Select(pair => pair.Key))
            {
                if (!visited.Add(start))
                    continue;

                var group = new List<GridPosition> { start };
                var touchesWeb = false;
                for (var i = 0; i < group.Count; i++)
                {
                    foreach (var neighbour in OrthogonalNeighbours(group[i]))
                    {
                        if (!byCell.TryGetValue(neighbour, out var other))
                            continue;
                        if (other.Hidden && visited.Add(neighbour))
                            group.Add(neighbour);
                        else if (other.Locked && !other.Hidden)
                            touchesWeb = true;
                    }
                }

                if (!touchesWeb)
                    problems.Add($"Initial layout '{layout.name}': the box{(group.Count > 1 ? "es" : string.Empty)} at " +
                                 $"{string.Join(", ", group.Select(cell => $"({cell.X}, {cell.Y})"))} can never open; a box opens only " +
                                 "when a webbed item next to it (or next to a box it touches) is freed.");
            }
        }

        private static IEnumerable<GridPosition> OrthogonalNeighbours(GridPosition cell)
        {
            yield return new GridPosition(cell.X, cell.Y - 1);
            yield return new GridPosition(cell.X - 1, cell.Y);
            yield return new GridPosition(cell.X + 1, cell.Y);
            yield return new GridPosition(cell.X, cell.Y + 1);
        }

        private static void CheckBuildsDefinition(GameConfigAsset config, List<string> problems)
        {
            try
            {
                new GameDefinitionFactory().Create(config);
            }
            catch (GameConfigException exception)
            {
                problems.Add(exception.Message);
            }
        }

        private static void CheckIcons(GameConfigAsset config, List<string> problems)
        {
            foreach (var chain in config.Chains)
            {
                if (chain != null)
                    CheckChainIcons(chain, problems);
            }

            var orders = config.Orders;
            if (orders == null)
                return;
            for (var i = 0; i < orders.CustomerAvatars.Count; i++)
            {
                if (orders.CustomerAvatars[i] == null)
                    problems.Add($"Order settings '{orders.name}' customer {i} has no avatar sprite.");
            }
        }

        private static void CheckChainIcons(ItemChainAsset chain, List<string> problems)
        {
            for (var i = 0; i < chain.Levels.Count; i++)
            {
                var entry = chain.Levels[i];
                if (entry != null && entry.Icon == null)
                    problems.Add($"{AssetLabels.Chain(chain)} level {i + 1} has no icon sprite.");
            }
        }
    }
}
