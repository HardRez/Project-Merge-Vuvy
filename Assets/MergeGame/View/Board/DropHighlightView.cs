using System.Collections;
using MergeGame.Presentation.Board;
using MergeGame.View.Animation;
using MergeGame.View.Common;
using MergeGame.View.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// Marks the hovered cell during a drag in the colour of what a drop would do; a possible merge glows and pulses
    /// to invite the drop.
    /// </summary>
    public sealed class DropHighlightView : MonoBehaviour
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private RectTransform pulsing;
        [SerializeField] private Image fill;
        [SerializeField] private Image outline;
        [SerializeField] private Image glow;

        [Range(0f, 1f)]
        [SerializeField] private float fillAlpha = 0.5f;

        [SerializeField] private float pulseScale = 1.1f;
        [SerializeField] private float pulseSeconds = 0.7f;

        private TweenChannel pulse;

        public bool IsShown => gameObject.activeSelf;

        // Created on first use: the board may hide this view before its own Awake has run.
        private TweenChannel Pulse => pulse ??= new TweenChannel(this);

        public void Show(CellRect cell, DropHighlight highlight)
        {
            gameObject.SetActive(true);
            ((RectTransform)transform).StretchOver(cell.Min, cell.Max);

            var color = theme.GetDropHighlightColor(highlight);
            fill.color = new Color(color.r, color.g, color.b, fillAlpha * color.a);
            outline.color = color;
            glow.color = color;

            var invitesDrop = highlight == DropHighlight.Merge;
            glow.gameObject.SetActive(invitesDrop);
            pulsing.localScale = Vector3.one;
            if (invitesDrop)
                Pulse.Play(Tween.Loop(PulseOnce));
            else
                Pulse.Stop();
        }

        public void Hide()
        {
            Pulse.Stop();
            gameObject.SetActive(false);
        }

        private IEnumerator PulseOnce() =>
            Tween.Run(pulseSeconds, Ease.Linear, t => pulsing.localScale = Vector3.one * Mathf.Lerp(1f, pulseScale, Ease.Bump(t)));
    }
}
