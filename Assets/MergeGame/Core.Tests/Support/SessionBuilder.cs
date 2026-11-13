using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Session;
using MergeGame.Core.Spawning;

namespace MergeGame.Core.Tests.Support
{
    internal sealed class SessionBuilder
    {
        private readonly List<(GridPosition position, ItemId item)> placements = new List<(GridPosition, ItemId)>();
        private GameDefinition definition = TestContent.Definition();
        private IRandom random = new ScriptedRandom();

        public SessionBuilder WithDefinition(GameDefinition value)
        {
            definition = value;
            return this;
        }

        public SessionBuilder WithRandom(IRandom value)
        {
            random = value;
            return this;
        }

        public SessionBuilder WithItem(int x, int y, ItemId item)
        {
            placements.Add((new GridPosition(x, y), item));
            return this;
        }

        public SessionBuilder WithFullBoardOf(ItemId item)
        {
            var size = definition.BoardSize;
            for (var y = 0; y < size.Height; y++)
                for (var x = 0; x < size.Width; x++)
                    WithItem(x, y, item);
            return this;
        }

        public MergeGameSession Build()
        {
            var board = new Board(definition.BoardSize);
            foreach (var (position, item) in placements)
                board.Place(position, new BoardItem(item));
            return new MergeGameSession(definition, random, board);
        }
    }
}
