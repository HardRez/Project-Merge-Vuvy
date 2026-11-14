using System;
using System.Collections;
using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// Sand burying a closed cell. It belongs to the board's cell layer rather than to an item, so it fills the whole
    /// cell like the cell tile itself and can never be mistaken for an item; the hidden item underneath stays invisible
    /// until the sand is blown away.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class CellCover : MonoBehaviour
    {
        [SerializeField] private Image sand;

        [Tooltip("Looks picked per cell, so neighbouring covers do not look stamped.")]
        [SerializeField] private Sprite[] variants = Array.Empty<Sprite>();

        [Tooltip("Sand puffs that fly out when the cover blows away.")]
        [SerializeField] private Image[] puffs = Array.Empty<Image>();

        [SerializeField] private float refuseSeconds = 0.35f;
        [SerializeField] private float blowSeconds = 0.55f;

        [Tooltip("How far the puffs fly, as a fraction of the cell width.")]
        [SerializeField] private float puffReach = 0.75f;

        private RectTransform rectTransform;
        private TweenChannel motion;

        private RectTransform RectTransform => rectTransform ??= (RectTransform)transform;
        private TweenChannel Motion => motion ??= new TweenChannel(this);

        /// <param name="variantSeed">Any stable number for the cell, e.g. derived from its coordinates.</param>
        public void Show(int variantSeed)
        {
            if (variants.Length > 0)
                sand.sprite = variants[Mathf.Abs(variantSeed) % variants.Length];
            SetRest();
        }

        /// <summary>A short shake: the sand does not move.</summary>
        public void PlayRefuse()
        {
            SetRest();
            var sandTransform = sand.rectTransform;
            Motion.Play(Tween.Run(refuseSeconds, Ease.Linear, t =>
            {
                var swing = Ease.Shake(t, 5);
                sandTransform.anchoredPosition = new Vector2(swing * 8f, 0f);
                sandTransform.localRotation = Quaternion.Euler(0f, 0f, swing * 3f);
            }));
        }

        /// <summary>
        /// After <paramref name="delay"/> the sand bursts into puffs and fades away, then <paramref name="blownAway"/> is
        /// called (also at once if the cover cannot animate).
        /// </summary>
        public void BlowAway(float delay, Action blownAway)
        {
            SetRest();
            if (!Motion.Play(PlayBlowAway(delay, blownAway)))
                blownAway?.Invoke();
        }

        private IEnumerator PlayBlowAway(float delay, Action blownAway)
        {
            yield return Tween.Delay(delay);

            var reach = RectTransform.rect.width * puffReach;
            foreach (var puff in puffs)
                puff.gameObject.SetActive(true);

            yield return Tween.Run(blowSeconds, Ease.Linear, t =>
            {
                var burst = Ease.OutCubic(t);
                sand.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.75f, Ease.InQuad(t));
                SetAlpha(sand, 1f - Ease.InQuad(t));
                for (var i = 0; i < puffs.Length; i++)
                {
                    var angle = (i + 0.5f) / puffs.Length * 2f * Mathf.PI;
                    puffs[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (reach * burst);
                    puffs[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.6f, burst);
                    SetAlpha(puffs[i], 1f - t);
                }
            });

            blownAway?.Invoke();
        }

        private void SetRest()
        {
            Motion.Stop();
            sand.rectTransform.anchoredPosition = Vector2.zero;
            sand.rectTransform.localRotation = Quaternion.identity;
            sand.rectTransform.localScale = Vector3.one;
            SetAlpha(sand, 1f);
            foreach (var puff in puffs)
            {
                puff.gameObject.SetActive(false);
                puff.rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
