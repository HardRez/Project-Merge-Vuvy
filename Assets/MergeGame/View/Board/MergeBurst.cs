using System.Collections.Generic;
using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// The burst of a merge, the moment the new item appears: a white-gold flash, a fan of rays thrown open and turning, a
    /// shock wave ring racing outwards (all behind the item) and stars flying out of it (over the item). The item drives
    /// it through <see cref="Apply"/> on its own merge clock, so it starts, runs and ends with the item's pop.
    /// </summary>
    public sealed class MergeBurst : MonoBehaviour
    {
        [SerializeField] private Image flash;
        [SerializeField] private Image rays;
        [SerializeField] private Image shockRing;
        [SerializeField] private Image[] stars;

        [Tooltip("How far the stars fly, as a share of the item's width; every other star flies a little less far.")]
        [SerializeField] private float starReach = 1.05f;

        [Tooltip("The size the shock wave reaches, as a multiple of the item.")]
        [SerializeField] private float shockSpread = 2.5f;

        public bool IsPlaying => flash.gameObject.activeSelf;
        public Image ShockRing => shockRing;
        public IReadOnlyList<Image> Stars => stars;

        public void Begin()
        {
            SetActive(true);
            Apply(0f);
        }

        /// <param name="t">Progress through the merge pop, 0 to 1.</param>
        public void Apply(float t)
        {
            t = Mathf.Clamp01(t);
            flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.5f, Ease.OutCubic(t));
            SetAlpha(flash, Ease.Bump(Mathf.Min(1f, t * 2.2f)));

            rays.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.3f, Ease.OutCubic(t));
            rays.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -55f * t);
            SetAlpha(rays, Mathf.Min(1f, t * 8f) * (1f - Ease.InQuad(t)));

            var wave = Mathf.Min(1f, t * 1.4f);
            shockRing.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, shockSpread, Ease.OutCubic(wave));
            SetAlpha(shockRing, 1f - Ease.OutQuad(wave));

            var width = ((RectTransform)transform).rect.width;
            for (var i = 0; i < stars.Length; i++)
            {
                var degrees = 20f + 360f * i / stars.Length;
                var direction = new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
                var reach = starReach * (i % 2 == 0 ? 1f : 0.78f);
                var star = stars[i];
                star.rectTransform.anchoredPosition = direction * (reach * width * Ease.OutCubic(t));
                star.rectTransform.localScale = Vector3.one * (Mathf.Lerp(0.3f, 1f, Mathf.Min(1f, t * 4f)) * (1f - 0.45f * t));
                SetAlpha(star, 1f - Ease.InQuad(t));
            }
        }

        /// <summary>Hides every part, back at its rest pose.</summary>
        public void Rest()
        {
            Apply(0f);
            SetActive(false);
        }

        private void SetActive(bool active)
        {
            flash.gameObject.SetActive(active);
            rays.gameObject.SetActive(active);
            shockRing.gameObject.SetActive(active);
            foreach (var star in stars)
                star.gameObject.SetActive(active);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
