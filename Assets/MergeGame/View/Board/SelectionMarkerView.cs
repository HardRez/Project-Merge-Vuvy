using System.Collections;
using MergeGame.View.Animation;
using MergeGame.View.Common;
using UnityEngine;

namespace MergeGame.View.Board
{
    /// <summary>The ring around the selected item: pops in on selection, then breathes gently.</summary>
    public sealed class SelectionMarkerView : MonoBehaviour
    {
        [SerializeField] private RectTransform ring;
        [SerializeField] private float popSeconds = 0.25f;
        [SerializeField] private float breatheScale = 1.05f;
        [SerializeField] private float breatheSeconds = 1.1f;

        private TweenChannel motion;

        public bool IsShown => gameObject.activeSelf;

        // Created on first use: the board may hide this view before its own Awake has run.
        private TweenChannel Motion => motion ??= new TweenChannel(this);

        public void Show(CellRect cell)
        {
            gameObject.SetActive(true);
            ((RectTransform)transform).StretchOver(cell.Min, cell.Max);
            Motion.Play(PopThenBreathe());
        }

        public void Hide()
        {
            Motion.Stop();
            gameObject.SetActive(false);
        }

        private IEnumerator PopThenBreathe()
        {
            yield return Tween.Run(popSeconds, Ease.OutBack, t => ring.localScale = Vector3.one * Mathf.LerpUnclamped(1.3f, 1f, t));
            yield return Tween.Loop(Breathe);
        }

        private IEnumerator Breathe() =>
            Tween.Run(breatheSeconds, Ease.Linear, t => ring.localScale = Vector3.one * Mathf.Lerp(1f, breatheScale, Ease.Bump(t)));
    }
}
