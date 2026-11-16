using System;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Session
{
    /// <summary>An item the designer puts on the board before the first move.</summary>
    public readonly struct InitialPlacement
    {
        /// <param name="isHidden">Starts hidden under sand; requires <paramref name="isLocked"/>, see <see cref="BoardItem.IsHidden"/>.</param>
        /// <exception cref="ArgumentException">The placement is hidden but not locked.</exception>
        public InitialPlacement(GridPosition position, ItemId itemId, bool isLocked = false, bool isHidden = false)
        {
            if (isHidden && !isLocked)
                throw new ArgumentException($"Hidden placement {itemId} at {position} must also be locked.", nameof(isLocked));

            Position = position;
            ItemId = itemId;
            IsLocked = isLocked;
            IsHidden = isHidden;
        }

        public GridPosition Position { get; }
        public ItemId ItemId { get; }
        public bool IsLocked { get; }

        /// <summary>Always implies <see cref="IsLocked"/>.</summary>
        public bool IsHidden { get; }

        public static InitialPlacement Hidden(GridPosition position, ItemId itemId) => new InitialPlacement(position, itemId, true, true);

        public override string ToString() =>
            $"{ItemId}{(IsHidden ? " (hidden)" : IsLocked ? " (locked)" : string.Empty)} at {Position}";
    }
}
