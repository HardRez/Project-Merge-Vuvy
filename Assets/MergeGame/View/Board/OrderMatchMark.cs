using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// Says "a customer wants this": a check badge in the item's corner with a soft halo behind the item. Calm on
    /// purpose: it pops in and fades out but never moves while shown, so it does not compete with hints and shines.
    /// It is part of the item, so it travels with it when dragged and moved.
    /// </summary>
    public sealed class OrderMatchMark : MonoBehaviour
    {
        [Tooltip("Behind the icon.")]
        [SerializeField] private Image halo;

        [Tooltip("In a corner, in front of the icon.")]
        [SerializeField] private Image badge;

        [Range(0f, 1f)]
        [SerializeField] private float haloAlpha = 0.6f;

        [SerializeField] private float appearSeconds = 0.3f;
        [SerializeField] private float disappearSeconds = 0.2f;

        private TweenChannel transition;

        public bool IsShown { get; private set; }

        private TweenChannel Transition => transition ??= new TweenChannel(this);

        public void Show()
        {
            if (IsShown)
                return;

            IsShown = true;
            SetVisible(true);
            var badgeTransform = badge.rectTransform;
            if (!Transition.Play(Tween.Run(appearSeconds, Ease.Linear, t =>
                {
                    badgeTransform.localScale = Vector3.one * Ease.OutBack(t);
                    SetAlpha(halo, haloAlpha * Ease.OutQuad(t));
                })))
                SetRest(true);
        }

        public void Hide()
        {
            if (!IsShown)
                return;

            IsShown = false;
            var badgeTransform = badge.rectTransform;
            if (!Transition.Play(Tween.Run(disappearSeconds, Ease.InQuad, t =>
                {
                    badgeTransform.localScale = Vector3.one * (1f - t);
                    SetAlpha(halo, haloAlpha * (1f - t));
                    if (t >= 1f)
                        SetVisible(false);
                })))
                SetRest(false);
        }

        /// <summary>Shows or hides at once, without a transition; used when the item returns to the pool.</summary>
        public void SetRest(bool shown)
        {
            Transition.Stop();
            IsShown = shown;
            SetVisible(shown);
            badge.rectTransform.localScale = Vector3.one;
            SetAlpha(halo, shown ? haloAlpha : 0f);
        }

        private void SetVisible(bool visible)
        {
            halo.gameObject.SetActive(visible);
            badge.gameObject.SetActive(visible);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
