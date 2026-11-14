namespace MergeGame.Presentation.Score
{
    public interface IScoreView
    {
        /// <summary>Shows the score as is, without animation.</summary>
        void SetScore(int score);

        void ChangeScore(int previousScore, int score);
    }
}
