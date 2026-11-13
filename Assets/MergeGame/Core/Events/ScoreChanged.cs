namespace MergeGame.Core.Events
{
    public sealed class ScoreChanged : GameEvent
    {
        public ScoreChanged(int previousScore, int score)
        {
            PreviousScore = previousScore;
            Score = score;
        }

        public int PreviousScore { get; }
        public int Score { get; }
        public int Delta => Score - PreviousScore;

        public override string ToString() => $"{nameof(ScoreChanged)} {PreviousScore} -> {Score}";
    }
}
