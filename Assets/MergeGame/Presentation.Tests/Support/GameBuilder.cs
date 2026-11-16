using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Session;

namespace MergeGame.Presentation.Tests.Support
{
    /// <summary>Builds real sessions through the public Core API: a wood and a stone chain of five levels each.</summary>
    internal sealed class GameBuilder
    {
        private readonly List<InitialPlacement> placements = new List<InitialPlacement>();
        private readonly List<ItemId[]> orders = new List<ItemId[]>();
        private int width = 5;
        private int height = 5;
        private int orderSlots;
        private int[] rolls = new int[0];

        public GameBuilder WithSize(int boardWidth, int boardHeight)
        {
            width = boardWidth;
            height = boardHeight;
            return this;
        }

        public GameBuilder WithItem(int x, int y, ItemId item, bool isLocked = false)
        {
            placements.Add(new InitialPlacement(new GridPosition(x, y), item, isLocked));
            return this;
        }

        /// <summary>A hidden item; the definition requires a locked, visible item next to it (or to its group of hidden items).</summary>
        public GameBuilder WithHiddenItem(int x, int y, ItemId item)
        {
            placements.Add(InitialPlacement.Hidden(new GridPosition(x, y), item));
            return this;
        }

        public GameBuilder WithLockedItem(int x, int y, ItemId item) => WithItem(x, y, item, isLocked: true);

        public GameBuilder WithFullBoardOf(ItemId item)
        {
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    WithItem(x, y, item);
            return this;
        }

        /// <summary>
        /// One slot per order list given; later lists (and then the last one, repeated) become the replacements.
        /// Customers are chosen with the default roll 0, so slot i shows customer i.
        /// </summary>
        public GameBuilder WithOrders(int slotCount, params ItemId[][] requirementLists)
        {
            orderSlots = slotCount;
            orders.AddRange(requirementLists);
            return this;
        }

        public GameBuilder WithRolls(params int[] values)
        {
            rolls = values;
            return this;
        }

        public MergeGameSession Build()
        {
            var hasOrders = orderSlots > 0;
            var definition = new GameDefinition(
                TestContent.Catalog(),
                new BoardSize(width, height),
                TestContent.MergeScoreTable(),
                initialPlacements: placements,
                orderSettings: hasOrders ? TestContent.OrderSetup(orderSlots) : null,
                orderGenerator: hasOrders ? new ScriptedOrderGenerator(orders) : null);
            return new MergeGameSession(definition, new ScriptedRandom(rolls));
        }
    }
}
