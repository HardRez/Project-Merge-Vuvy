using System.Globalization;
using MergeGame.Presentation.Score;
using MergeGame.View.Animation;
using TMPro;
using UnityEngine;

namespace MergeGame.View.Score
{
    /// <summary>The score pill: counts up to a new score while the pill and its star punch.</summary>
    public sealed class ScoreView : MonoBehaviour, IScoreView
    {
        private static readonly NumberFormatInfo ThousandsWithDots = new NumberFormatInfo { NumberGroupSeparator = ".", NumberDecimalDigits = 0 };

        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform pill;
        [SerializeField] private RectTransform star;
        [SerializeField] private float countSeconds = 0.6f;
        [SerializeField] private float punchSeconds = 0.35f;
        [SerializeField] private float punchScale = 0.14f;

        private TweenChannel counting;
        private TweenChannel punch;

        public int DisplayedScore { get; private set; }

        private void Awake()
        {
            counting = new TweenChannel(this);
            punch = new TweenChannel(this);
        }

        public void SetScore(int score)
        {
            counting.Stop();
            Display(score);
        }

        public void ChangeScore(int previousScore, int score)
        {
            // Continue from what is on screen, so a change during a count never jumps backwards.
            var from = DisplayedScore;
            if (!counting.Play(Tween.Run(countSeconds, Ease.OutCubic, t => Display(Mathf.RoundToInt(Mathf.Lerp(from, score, t))))))
                Display(score);

            punch.Play(Tween.Run(punchSeconds, Ease.Linear, t =>
            {
                var bump = Ease.Bump(t);
                pill.localScale = Vector3.one * (1f + punchScale * bump);
                star.localScale = Vector3.one * (1f + 2f * punchScale * bump);
                star.localRotation = Quaternion.Euler(0f, 0f, -25f * bump);
            }));
        }

        private void Display(int score)
        {
            DisplayedScore = score;
            label.text = score.ToString("N0", ThousandsWithDots);
        }
    }
}
