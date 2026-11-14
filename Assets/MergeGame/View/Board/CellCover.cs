using System;
using System.Collections;
using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// Sand burying a closed cell. It belongs to the board rather than to an item, so it fills the whole cell like the
    /// cell tile itself and can never be mistaken for an item. When the cell opens, the sand drains down and out through
    /// the bottom of the cell: after a short crumble the whole sand body sinks ever faster under an uneven surface, with
    /// loose grains trickling after it, all clipped to the cell's rounded shape by the mask on this object.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Mask))]
    public sealed class CellCover : MonoBehaviour
    {
        [Tooltip("The sand that sinks: the sand tile, with the surface strip on top of it.")]
        [SerializeField] private RectTransform body;

        [SerializeField] private Image sand;

        [Tooltip("Uneven sand surface standing on the tile's top edge; out of sight (above the cell) until the sand sinks.")]
        [SerializeField] private RectTransform surface;

        [Tooltip("Looks picked per cell, so neighbouring covers do not look stamped.")]
        [SerializeField] private Sprite[] variants = Array.Empty<Sprite>();

        [Tooltip("Loose grains and streaks falling with the sand; their number is the number of grains.")]
        [SerializeField] private Image[] grains = Array.Empty<Image>();

        [Tooltip("A faint wisp of dust at the bottom edge as the last sand leaves.")]
        [SerializeField] private Image dust;

        [Header("Drain")]
        [SerializeField] private float drainSeconds = 0.85f;

        [Tooltip("Share of the drain spent crumbling (a shiver and a first trickle) before the sand starts to flow.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float crumbleShare = 0.14f;

        [Tooltip("How far the sand has sunk when the crumble ends, as a share of the whole way.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float crumbleSink = 0.05f;

        [Tooltip("Share of the drain at which the sand body is gone; grains still trickle out after it.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float bodyGoneShare = 0.86f;

        [SerializeField] private float shiverWidth = 2f;

        [Tooltip("How fast a loose grain falls, in cell heights per second squared.")]
        [SerializeField] private float grainGravity = 9f;

        [Header("Refuse")]
        [SerializeField] private float refuseSeconds = 0.35f;

        private TweenChannel motion;
        private Action drained;

        private TweenChannel Motion => motion ??= new TweenChannel(this);

        public float DrainSeconds => drainSeconds;
        public bool IsDraining => drained != null;

        /// <param name="variantSeed">Any stable number for the cell, e.g. derived from its coordinates.</param>
        public void Show(int variantSeed)
        {
            if (variants.Length > 0)
                sand.sprite = variants[Mathf.Abs(variantSeed) % variants.Length];
            Motion.Stop();
            SetRest();
        }

        /// <summary>A short shake: the sand does not move.</summary>
        public void PlayRefuse()
        {
            Motion.Stop();
            SetRest();
            Motion.Play(Tween.Run(refuseSeconds, Ease.Linear, t =>
                body.anchoredPosition = new Vector2(Ease.Shake(t, 5) * 8f, 0f)));
        }

        /// <summary>
        /// After <paramref name="delay"/> the sand drains out of the cell; then, or when <see cref="Finish"/> cuts it short,
        /// <paramref name="onDrained"/> is called once.
        /// </summary>
        public void Drain(float delay, Action onDrained)
        {
            drained = onDrained;
            SetRest();
            if (!Motion.Play(PlayDrain(delay)))
                Finish();
        }

        /// <summary>Shows the drain at <paramref name="progress"/> (0..1) and holds it there; for previews and captures.</summary>
        public void ShowDrainFrame(float progress)
        {
            Motion.Stop();
            ApplyDrain(Mathf.Clamp01(progress));
        }

        /// <summary>Ends the drain at once: the sand is gone.</summary>
        public void Finish()
        {
            Motion.Stop();
            var onDrained = drained;
            drained = null;
            onDrained?.Invoke();
        }

        private IEnumerator PlayDrain(float delay)
        {
            yield return Tween.Delay(delay);
            yield return Tween.Run(drainSeconds, Ease.Linear, ApplyDrain);
            Finish();
        }

        /// <summary>The whole effect as a function of normalised time, so every frame is reproducible.</summary>
        private void ApplyDrain(float t)
        {
            surface.gameObject.SetActive(true);
            var sink = SinkAt(t);
            var travel = SurfaceTop;
            body.anchorMin = new Vector2(0f, -sink * travel);
            body.anchorMax = new Vector2(1f, 1f - sink * travel);
            body.anchoredPosition = new Vector2(ShiverAt(t), 0f);

            for (var i = 0; i < grains.Length; i++)
                PlaceGrain(grains[i], i, t);

            var wisp = Ease.Bump(Mathf.InverseLerp(bodyGoneShare - 0.2f, 1f, t));
            dust.gameObject.SetActive(wisp > 0f);
            SetAlpha(dust, 0.55f * wisp);
        }

        /// <summary>The surface strip's top, in the body's normalised space: how far the body must sink to empty the cell.</summary>
        private float SurfaceTop => surface.anchorMax.y;

        /// <summary>The wavy surface line, in the body's normalised space (it sits in the strip's upper part).</summary>
        private float SurfaceLine => Mathf.Lerp(surface.anchorMin.y, surface.anchorMax.y, 0.6f);

        /// <summary>0 at rest, 1 when the sand is out of the cell: a crumble, then a gravity-like ease-in.</summary>
        private float SinkAt(float t)
        {
            if (t < crumbleShare)
                return crumbleSink * Ease.InQuad(t / crumbleShare);
            return Mathf.Lerp(crumbleSink, 1f, Ease.InQuad(Mathf.Clamp01((t - crumbleShare) / (bodyGoneShare - crumbleShare))));
        }

        private float ShiverAt(float t) =>
            t < crumbleShare ? Ease.Shake(t / crumbleShare, 7) * shiverWidth : 0f;

        /// <summary>Where the sand surface is in the cell (0 bottom, 1 top) at normalised time <paramref name="t"/>.</summary>
        private float SurfaceInCellAt(float t) => Mathf.Clamp01(SurfaceLine - SinkAt(t) * SurfaceTop);

        /// <summary>
        /// Grain <paramref name="index"/> breaks off the surface at its own moment and falls from there; its column and
        /// timing come from its index, so the pattern is the same every time.
        /// </summary>
        private void PlaceGrain(Image grain, int index, float t)
        {
            var start = Mathf.Lerp(crumbleShare * 0.5f, bodyGoneShare + 0.08f, Hash(index, 1));
            // Grains breaking off after the body is gone are the last ones clinging higher up.
            var origin = start < bodyGoneShare ? SurfaceInCellAt(start) : Mathf.Lerp(0.4f, 0.9f, Hash(index, 3));
            var elapsed = (t - start) * drainSeconds;
            var height = origin - 0.5f * grainGravity * elapsed * elapsed;
            var visible = elapsed > 0f && height > -0.1f;
            grain.gameObject.SetActive(visible);
            if (!visible)
                return;

            var column = Mathf.Lerp(0.12f, 0.88f, Hash(index, 2));
            grain.rectTransform.anchorMin = grain.rectTransform.anchorMax = new Vector2(column, height);
            // Fast grains smear into streaks.
            var speed = grainGravity * elapsed;
            grain.rectTransform.localScale = new Vector3(1f, 1f + Mathf.Min(3f, speed * 0.6f), 1f);
            SetAlpha(grain, Mathf.Clamp01(height * 4f + 0.4f));
        }

        private void SetRest()
        {
            surface.gameObject.SetActive(false);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.anchoredPosition = Vector2.zero;
            foreach (var grain in grains)
                grain.gameObject.SetActive(false);
            dust.gameObject.SetActive(false);
        }

        /// <summary>A stable pseudo-random number in [0, 1) for a grain and a property.</summary>
        private static float Hash(int index, int salt)
        {
            var value = Mathf.Sin(index * 12.9898f + salt * 78.233f) * 43758.5453f;
            return value - Mathf.Floor(value);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
