using System.Collections;
using System.Collections.Generic;
using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// The attention layer of an item, between its root and its body: a short shine after a spawn, a one-shot pulse
    /// toward the partner of a suggested merge, or the eager glow, pulse and shine of a merge target under the dragged item
    /// (the board's <see cref="MergeTargetGlow"/> lights up round it, below every item).
    /// One accent at a time, by the priority of <see cref="AccentKind"/>. It only moves its own transform and visuals,
    /// so it composes with the item's other animations, and <see cref="Stop()"/> returns it to its rest pose exactly.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ItemAccent : MonoBehaviour
    {
        [SerializeField] private Image glow;

        [Tooltip("Frames each item of a merge hint, so the pair reads even in a still frame.")]
        [SerializeField] private Image hintRing;

        [Tooltip("Masked by the item's icon shape, so the shine sweeps over the item itself.")]
        [SerializeField] private Image shineMask;

        [SerializeField] private RectTransform shine;

        [Tooltip("Fades the whole sparkle cluster in and out; its stars twinkle inside it on a clock of their own.")]
        [SerializeField] private CanvasGroup sparkles;

        [Tooltip("The stars of the sparkle cluster; each twinkles with its own period and phase.")]
        [SerializeField] private Image[] twinkles;

        [Header("Sparkles")]
        [Tooltip("One twinkle of the first star; the others are a little slower, so the cluster never pulses in step.")]
        [Min(0.1f)]
        [SerializeField] private float twinkleSeconds = 1.1f;

        [Tooltip("How long the cluster takes to fade fully in or out.")]
        [Min(0.01f)]
        [SerializeField] private float sparkleFadeSeconds = 0.2f;

        [Header("Spawn shine")]
        [SerializeField] private Color spawnGlowColor = new Color(1f, 0.96f, 0.78f, 1f);
        [SerializeField] private float spawnShineSeconds = 1f;

        [Header("Merge hint")]
        [SerializeField] private Color hintGlowColor = new Color(1f, 0.88f, 0.42f, 1f);

        [Tooltip("How far each item leans toward its partner, as a fraction of the cell width.")]
        [SerializeField] private float hintLean = 0.16f;

        [Tooltip("Length of one hint pulse; keep it shorter than the hint's repeat interval.")]
        [SerializeField] private float hintSeconds = 1.3f;

        [Tooltip("Leans toward the partner within one hint pulse.")]
        [Min(1)]
        [SerializeField] private int hintNudges = 2;

        [Header("Merge target")]
        [SerializeField] private Color targetGlowColor = new Color(1f, 0.86f, 0.25f, 1f);
        [SerializeField] private float targetScale = 1.16f;
        [SerializeField] private float targetEnterSeconds = 0.18f;
        [SerializeField] private float targetCycleSeconds = 0.8f;

        private RectTransform rectTransform;
        private TweenChannel loop;
        private TweenChannel twinkle;
        private float sparkleTarget;

        public AccentKind Current { get; private set; }

        /// <summary>The sparkle cluster and its stars, for tests that follow the animation frame by frame.</summary>
        public CanvasGroup Sparkles => sparkles;

        public IReadOnlyList<Image> Twinkles => twinkles;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            loop = new TweenChannel(this);
            twinkle = new TweenChannel(this);
            SetRest();
        }

        /// <summary>A bright burst and one sweep of light, then the item settles; ignored while a stronger accent plays.</summary>
        /// <param name="iconSprite">The item's icon, which shapes the shine.</param>
        public void PlaySpawnShine(Sprite iconSprite)
        {
            if (Current > AccentKind.SpawnShine)
                return;

            Stop();
            Current = AccentKind.SpawnShine;
            glow.color = spawnGlowColor;
            glow.gameObject.SetActive(true);
            ShowShine(iconSprite);
            ShowSparkles();
            loop.Play(SpawnShine());
        }

        /// <param name="towardPartner">Direction to the other item, in the space of this layer's parent.</param>
        public void PlayMergeHint(Vector2 towardPartner)
        {
            if (Current > AccentKind.MergeHint)
                return;

            Stop();
            Current = AccentKind.MergeHint;
            glow.color = hintGlowColor;
            glow.gameObject.SetActive(true);
            hintRing.color = hintGlowColor;
            hintRing.gameObject.SetActive(true);
            var lean = towardPartner.normalized * (rectTransform.rect.width * hintLean);
            loop.Play(HintPulse(lean));
        }

        /// <param name="iconSprite">The item's icon, which shapes the shine.</param>
        public void PlayMergeTarget(Sprite iconSprite)
        {
            if (Current == AccentKind.MergeTarget)
                return;

            Stop();
            Current = AccentKind.MergeTarget;
            glow.color = targetGlowColor;
            glow.gameObject.SetActive(true);
            ShowShine(iconSprite);
            loop.Play(TargetLoop());
        }

        /// <summary>Stops only an accent of <paramref name="kind"/>, so one owner never cancels another owner's accent.</summary>
        public void Stop(AccentKind kind)
        {
            if (Current == kind)
                Stop();
        }

        public void Stop()
        {
            loop?.Stop();
            Current = AccentKind.None;
            SetRest();
        }

        private void SetRest()
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            glow.gameObject.SetActive(false);
            hintRing.gameObject.SetActive(false);
            shineMask.gameObject.SetActive(false);
            twinkle?.Stop();
            sparkles.alpha = 0f;
            sparkles.gameObject.SetActive(false);
        }

        /// <summary>
        /// Starts the cluster from invisible and lets it fade in, so starting or restarting an accent never pops it in.
        /// </summary>
        private void ShowSparkles()
        {
            sparkles.alpha = 0f;
            sparkleTarget = 1f;
            sparkles.gameObject.SetActive(true);
            twinkle.Play(Twinkle());
        }

        /// <summary>
        /// Runs until the accent stops, on its own clock rather than the accent's cycle: every value is a smooth periodic
        /// function of that clock, so nothing jumps when the accent's cycle starts over.
        /// </summary>
        private IEnumerator Twinkle()
        {
            for (var clock = 0f; ; clock += Time.unscaledDeltaTime)
            {
                sparkles.alpha = Mathf.MoveTowards(sparkles.alpha, sparkleTarget, Time.unscaledDeltaTime / sparkleFadeSeconds);
                for (var i = 0; i < twinkles.Length; i++)
                {
                    var period = twinkleSeconds * (1f + 0.27f * i);
                    var phase = 0.37f * i;
                    var cycle = 2f * Mathf.PI * (clock / period + phase);
                    var bright = 0.5f - 0.5f * Mathf.Cos(cycle);
                    var star = twinkles[i];
                    star.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1f, bright);
                    star.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 14f * Mathf.Sin(cycle * 0.5f));
                    SetAlpha(star, Mathf.Lerp(0.25f, 1f, bright));
                }

                yield return null;
            }
        }

        private void ShowShine(Sprite iconSprite)
        {
            shineMask.sprite = iconSprite;
            shineMask.gameObject.SetActive(iconSprite != null);
            shine.anchoredPosition = new Vector2(-shineMask.rectTransform.rect.width, 0f);
        }

        private IEnumerator SpawnShine()
        {
            var sweep = shineMask.rectTransform.rect.width;
            yield return Tween.Run(spawnShineSeconds, Ease.Linear, t =>
            {
                var burst = Ease.Bump(Mathf.Min(1f, t * 1.6f));
                SetAlpha(glow, burst);
                shine.anchoredPosition = new Vector2(Mathf.Lerp(-sweep, sweep, Ease.InOutQuad(Mathf.Clamp01((t - 0.15f) * 1.6f))), 0f);
                // Fades out in time to be gone when the shine ends.
                sparkleTarget = t < 1f - sparkleFadeSeconds / spawnShineSeconds ? 1f : 0f;
            });

            // Ends on its own; the channel needs no stopping from inside its own coroutine.
            Current = AccentKind.None;
            SetRest();
        }

        /// <summary>
        /// One pulse: the glow and ring fade in, the item leans toward its partner <see cref="hintNudges"/> times, then
        /// everything fades and the item is back at rest. Both items of a hint start together and stay in sync.
        /// </summary>
        private IEnumerator HintPulse(Vector2 lean)
        {
            // Tips toward a partner on the left or right; a partner above or below gets a straight nod.
            var tilt = lean.normalized.x * 6f;
            yield return Tween.Run(hintSeconds, Ease.Linear, t =>
            {
                var nudge = Ease.Bump(t * hintNudges % 1f);
                var presence = Ease.Bump(t);
                rectTransform.anchoredPosition = lean * nudge;
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, -tilt * nudge);
                rectTransform.localScale = Vector3.one * (1f + 0.08f * nudge);
                SetAlpha(glow, presence * (0.6f + 0.35f * nudge));
                SetAlpha(hintRing, Mathf.Min(1f, presence * 1.6f));
            });

            Current = AccentKind.None;
            SetRest();
        }

        private IEnumerator TargetLoop()
        {
            yield return Tween.Run(targetEnterSeconds, Ease.OutBack, t =>
            {
                rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(1f, targetScale, t);
                // Ends where the cycle starts (its pulse is 0 at both ends), so the loop never jumps.
                SetAlpha(glow, TargetGlowRest * Mathf.Clamp01(t));
            });
            yield return Tween.Loop(TargetCycle);
        }

        private const float TargetGlowRest = 0.75f;

        /// <summary>
        /// An eager pulse while a shine sweeps across the item. Every value is the same at the end of a cycle as at its
        /// start (the shine is outside its mask at both ends).
        /// </summary>
        private IEnumerator TargetCycle()
        {
            var sweep = shineMask.rectTransform.rect.width;
            return Tween.Run(targetCycleSeconds, Ease.Linear, t =>
            {
                var pulse = Ease.Bump(t);
                rectTransform.localScale = Vector3.one * (targetScale + 0.05f * pulse);
                SetAlpha(glow, TargetGlowRest + (1f - TargetGlowRest) * pulse);
                shine.anchoredPosition = new Vector2(Mathf.Lerp(-sweep, sweep, Ease.InOutQuad(Mathf.Min(1f, t * 1.4f))), 0f);
            });
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
