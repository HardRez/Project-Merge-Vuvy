using System.Collections.Generic;
using MergeGame.Presentation.Score;

namespace MergeGame.Presentation.Tests.Support
{
    internal sealed class FakeScoreView : IScoreView
    {
        private readonly ViewLog log;

        public FakeScoreView(ViewLog log)
        {
            this.log = log;
        }

        public int? Score { get; private set; }
        public List<(int previous, int current)> Changes { get; } = new List<(int, int)>();

        public void SetScore(int score)
        {
            Score = score;
            log.Add($"Score.SetScore {score}");
        }

        public void ChangeScore(int previousScore, int score)
        {
            Score = score;
            Changes.Add((previousScore, score));
            log.Add($"Score.ChangeScore {previousScore} -> {score}");
        }
    }
}
