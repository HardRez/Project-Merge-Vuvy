using System;

namespace MergeGame.Core.Grid
{
    public readonly struct BoardSize : IEquatable<BoardSize>
    {
        public BoardSize(int width, int height)
        {
            if (width < 1)
                throw new ArgumentOutOfRangeException(nameof(width), width, "Board width must be at least 1.");
            if (height < 1)
                throw new ArgumentOutOfRangeException(nameof(height), height, "Board height must be at least 1.");

            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }
        public int CellCount => Width * Height;

        public bool Contains(GridPosition position) =>
            position.X >= 0 && position.X < Width && position.Y >= 0 && position.Y < Height;

        public bool Equals(BoardSize other) => Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is BoardSize other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Width, Height);
        public override string ToString() => $"{Width}x{Height}";

        public static bool operator ==(BoardSize left, BoardSize right) => left.Equals(right);
        public static bool operator !=(BoardSize left, BoardSize right) => !left.Equals(right);
    }
}
