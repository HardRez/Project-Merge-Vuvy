namespace MergeGame.Core.Events
{
    public sealed class BoardFullChanged : GameEvent
    {
        public BoardFullChanged(bool isFull)
        {
            IsFull = isFull;
        }

        public bool IsFull { get; }

        public override string ToString() => $"{nameof(BoardFullChanged)} {IsFull}";
    }
}
