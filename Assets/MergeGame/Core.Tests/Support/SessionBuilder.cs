using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Session;
using MergeGame.Core.Spawning;

namespace MergeGame.Core.Tests.Support
{
    internal sealed class SessionBuilder
    {
        private readonly List<(GridPosition position, BoardItem item)> placements = new List<(GridPosition, BoardItem)>();
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

        public SessionBuilder WithItem(int x, int y, ItemId item, bool isLocked = false)
        {
            placements.Add((new GridPosition(x, y), new BoardItem(item, isLocked)));
            return this;
        }

        public SessionBuilder WithLockedItem(int x, int y, ItemId item) => WithItem(x, y, item, isLocked: true);

        /// <summary>The board is built directly, so unlike a definition it accepts boxes nothing can open.</summary>
        public SessionBuilder WithHiddenItem(int x, int y, ItemId item)
        {
            placements.Add((new GridPosition(x, y), BoardItem.Hidden(item)));
            return this;
        }

        public SessionBuilder WithFullBoardOf(ItemId item, bool isLocked = false)
        {
            var size = definition.BoardSize;
            for (var y = 0; y < size.Height; y++)
                for (var x = 0; x < size.Width; x++)
                    WithItem(x, y, item, isLocked);
            return this;
        }

        /// <summary>The board is built directly; the definition's initial placements are not applied.</summary>
        public MergeGameSession Build()
        {
            var board = new Board(definition.BoardSize);
            foreach (var (position, item) in placements)
                board.Place(position, item);
            return new MergeGameSession(definition, random, board);
        }
    }
}
