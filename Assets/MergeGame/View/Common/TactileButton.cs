using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Common
{
    /// <summary>
    /// A button that feels physical: it sinks while pressed and springs back, and turns grey with dimmed content when
    /// it is not interactable. Use it with <see cref="Selectable.Transition.None"/>; it drives its own visuals.
    /// </summary>
    public sealed class TactileButton : Button
    {
        [SerializeField] private Graphic background;
        [SerializeField] private Color enabledColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.74f, 0.71f, 0.67f, 1f);

        [Tooltip("Icon and label; dimmed while the button is disabled.")]
        [SerializeField] private CanvasGroup content;

        [Range(0f, 1f)]
        [SerializeField] private float disabledContentAlpha = 0.7f;

        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float pressSeconds = 0.07f;
        [SerializeField] private float releaseSeconds = 0.22f;

        private TweenChannel scaleTween;

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            var isDisabled = state == SelectionState.Disabled;
            if (background != null)
                background.color = isDisabled ? disabledColor : enabledColor;
            if (content != null)
                content.alpha = isDisabled ? disabledContentAlpha : 1f;

            var isPressed = state == SelectionState.Pressed;
            AnimateScale(isPressed ? pressedScale : 1f, instant || !Application.isPlaying, isPressed);
        }

        private void AnimateScale(float target, bool instant, bool isPressing)
        {
            scaleTween ??= new TweenChannel(this);
            if (instant)
            {
                scaleTween.Stop();
                transform.localScale = Vector3.one * target;
                return;
            }

            var start = transform.localScale.x;
            var duration = isPressing ? pressSeconds : releaseSeconds;
            EaseFunction ease = isPressing ? Ease.OutQuad : Ease.OutBack;
            scaleTween.Play(Tween.Run(duration, ease, t => transform.localScale = Vector3.one * Mathf.LerpUnclamped(start, target, t)));
        }
    }
}
