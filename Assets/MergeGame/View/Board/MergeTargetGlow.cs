using System.Collections;
using System.Collections.Generic;
using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// The pack's merge glow under the item a drop would merge into, below every item: a soft golden halo, a fan of
    /// rays turning slowly, a bright ring beating like a pulse and a few stars circling the item and twinkling. It fades
    /// in rather than popping, and every value is a smooth periodic function of its own clock, so nothing ever jumps.
    /// </summary>
    public sealed class MergeTargetGlow : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image halo;
        [SerializeField] private RectTransform rays;
        [SerializeField] private Image ring;
        [SerializeField] private Image[] stars;

        [Tooltip("How long the glow takes to fade fully in.")]
        [Min(0.01f)]
        [SerializeField] private float fadeSeconds = 0.18f;

        [Tooltip("One beat of the halo and the ring.")]
        [Min(0.1f)]
        [SerializeField] private float beatSeconds = 0.8f;

        [SerializeField] private float raysDegreesPerSecond = 26f;
        [SerializeField] private float orbitDegreesPerSecond = 40f;

        [Tooltip("Radius of the stars' orbit, as a share of the cell's width.")]
        [SerializeField] private float orbitRadius = 0.8f;

        [Tooltip("One twinkle of the first star; the others are a little slower, so they never twinkle in step.")]
        [Min(0.1f)]
        [SerializeField] private float twinkleSeconds = 0.9f;

        private TweenChannel loop;

        public bool IsShown => gameObject.activeSelf;
        public float Alpha => group.alpha;
        public RectTransform Rays => rays;
        public Image Ring => ring;
        public IReadOnlyList<Image> Stars => stars;

        // Created on first use: the highlight may hide this glow before its own Awake has run.
        private TweenChannel Loop => loop ??= new TweenChannel(this);

        /// <summary>Starts from invisible and fades in; does nothing while already shown.</summary>
        public void Show()
        {
            if (IsShown)
                return;

            group.alpha = 0f;
            gameObject.SetActive(true);
            Apply(0f);
            Loop.Play(Glow());
        }

        public void Hide()
        {
            Loop.Stop();
            gameObject.SetActive(false);
        }

        private IEnumerator Glow()
        {
            for (var clock = 0f; ; clock += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime / fadeSeconds);
                Apply(clock);
                yield return null;
            }
        }

        private void Apply(float clock)
        {
            var beat = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * clock / beatSeconds);
            rays.localRotation = Quaternion.Euler(0f, 0f, -raysDegreesPerSecond * clock);
            rays.localScale = Vector3.one * (1f + 0.06f * beat);
            SetAlpha(halo, Mathf.Lerp(0.7f, 1f, beat));
            ring.rectTransform.localScale = Vector3.one * (1f + 0.07f * beat);
            SetAlpha(ring, Mathf.Lerp(0.55f, 1f, beat));

            var radius = ((RectTransform)transform).rect.width * orbitRadius;
            for (var i = 0; i < stars.Length; i++)
            {
                var degrees = orbitDegreesPerSecond * clock + 360f * i / stars.Length + 30f;
                var direction = new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
                var cycle = 2f * Mathf.PI * (clock / (twinkleSeconds * (1f + 0.23f * i)) + 0.31f * i);
                var bright = 0.5f - 0.5f * Mathf.Cos(cycle);
                var star = stars[i];
                star.rectTransform.anchoredPosition = direction * radius * (1f + 0.06f * Mathf.Sin(cycle * 0.5f));
                star.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.45f, 1f, bright);
                SetAlpha(star, Mathf.Lerp(0.3f, 1f, bright));
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
