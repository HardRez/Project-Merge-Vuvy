using System.Globalization;
using MergeGame.Presentation.Score;
using MergeGame.View.Animation;
using TMPro;
using UnityEngine;

namespace MergeGame.View.Score
{
    /// <summary>
    /// The score pill: counts up to a new score while the pill and its icon punch. Points can be announced as on their
    /// way (<see cref="ExpectIncoming"/>, the bills of a completed order): the pill leaves them out until they arrive
    /// (<see cref="ReceiveIncoming"/>), so it counts them only when the money lands in it.
    /// </summary>
    public sealed class ScoreView : MonoBehaviour, IScoreView
    {
        private static readonly NumberFormatInfo ThousandsWithDots = new NumberFormatInfo { NumberGroupSeparator = ".", NumberDecimalDigits = 0 };

        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform pill;
        [SerializeField] private RectTransform icon;
        [SerializeField] private float countSeconds = 0.6f;
        [SerializeField] private float punchSeconds = 0.35f;
        [SerializeField] private float punchScale = 0.14f;

        private TweenChannel counting;
        private TweenChannel punch;
        private int score;
        private int incoming;

        public int DisplayedScore { get; private set; }

        /// <summary>Points announced as on their way that have not arrived yet; the pill does not show them.</summary>
        public int IncomingPoints => incoming;

        /// <summary>Where money flying into the pill should land: its bills icon.</summary>
        public Vector3 CashTarget => icon.position;

        private void Awake()
        {
            counting = new TweenChannel(this);
            punch = new TweenChannel(this);
        }

        public void SetScore(int score)
        {
            this.score = score;
            incoming = 0;
            counting.Stop();
            Display(score);
        }

        public void ChangeScore(int previousScore, int score)
        {
            this.score = score;
            // Points still on their way are left out; when the whole change is, the pill waits for them to land.
            if (score - incoming != DisplayedScore)
                CountAndPunch();
        }

        /// <summary>Announces points that will arrive later; call it before the score change that awards them.</summary>
        public void ExpectIncoming(int points)
        {
            if (points > 0)
                incoming += points;
        }

        /// <summary>Some of the announced points arrived: the pill counts them and punches.</summary>
        public void ReceiveIncoming(int points)
        {
            points = Mathf.Clamp(points, 0, incoming);
            if (points == 0)
                return;

            incoming -= points;
            CountAndPunch();
        }

        private void CountAndPunch()
        {
            // Continue from what is on screen, so a change during a count never jumps backwards.
            var from = DisplayedScore;
            var to = score - incoming;
            if (!counting.Play(Tween.Run(countSeconds, Ease.OutCubic, t => Display(Mathf.RoundToInt(Mathf.Lerp(from, to, t))))))
                Display(to);

            punch.Play(Tween.Run(punchSeconds, Ease.Linear, t =>
            {
                var bump = Ease.Bump(t);
                pill.localScale = Vector3.one * (1f + punchScale * bump);
                icon.localScale = Vector3.one * (1f + 2f * punchScale * bump);
                icon.localRotation = Quaternion.Euler(0f, 0f, -25f * bump);
            }));
        }

        private void Display(int score)
        {
            DisplayedScore = score;
            label.text = score.ToString("N0", ThousandsWithDots);
        }
    }
}
