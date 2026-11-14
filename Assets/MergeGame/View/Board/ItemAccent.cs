using System.Collections;
using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// The attention layer of an item, between its root and its body: a short shine after a spawn, a gentle lean toward
    /// the partner of a suggested merge, or the eager glow, pulse and shine of a merge target under the dragged item.
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
        [SerializeField] private Image sparkles;

        [Header("Spawn shine")]
        [SerializeField] private Color spawnGlowColor = new Color(1f, 0.96f, 0.78f, 1f);
        [SerializeField] private float spawnShineSeconds = 1f;

        [Header("Merge hint")]
        [SerializeField] private Color hintGlowColor = new Color(1f, 0.88f, 0.42f, 1f);

        [Tooltip("How far each item leans toward its partner, as a fraction of the cell width.")]
        [SerializeField] private float hintLean = 0.16f;

        [SerializeField] private float hintSeconds = 1.1f;

        [Header("Merge target")]
        [SerializeField] private Color targetGlowColor = new Color(1f, 0.86f, 0.25f, 1f);
        [SerializeField] private float targetScale = 1.12f;
        [SerializeField] private float targetEnterSeconds = 0.18f;
        [SerializeField] private float targetCycleSeconds = 0.8f;

        private RectTransform rectTransform;
        private TweenChannel loop;

        public AccentKind Current { get; private set; }

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            loop = new TweenChannel(this);
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
            sparkles.gameObject.SetActive(true);
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
            loop.Play(Tween.Loop(() => HintCycle(lean)));
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
            sparkles.gameObject.SetActive(true);
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
            sparkles.gameObject.SetActive(false);
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
                sparkles.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 30f * t);
                sparkles.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.1f, Ease.OutCubic(t));
                SetAlpha(sparkles, Ease.Bump(t));
            });

            // Ends on its own; the channel needs no stopping from inside its own coroutine.
            Current = AccentKind.None;
            SetRest();
        }

        /// <summary>Lean in and back out, continuously; both items of a hint start together and stay in sync.</summary>
        private IEnumerator HintCycle(Vector2 lean)
        {
            // Tips toward a partner on the left or right; a partner above or below gets a straight nod.
            var tilt = lean.normalized.x * 6f;
            return Tween.Run(hintSeconds, Ease.Linear, t =>
            {
                var nudge = Ease.Bump(t);
                rectTransform.anchoredPosition = lean * nudge;
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, -tilt * nudge);
                rectTransform.localScale = Vector3.one * (1f + 0.08f * nudge);
                SetAlpha(glow, 0.5f + 0.45f * nudge);
                SetAlpha(hintRing, 0.65f + 0.35f * nudge);
            });
        }

        private IEnumerator TargetLoop()
        {
            yield return Tween.Run(targetEnterSeconds, Ease.OutBack, t =>
            {
                rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(1f, targetScale, t);
                SetAlpha(glow, Mathf.Clamp01(t));
            });
            yield return Tween.Loop(TargetCycle);
        }

        /// <summary>An eager pulse while a shine sweeps across the item and sparkles twinkle around it.</summary>
        private IEnumerator TargetCycle()
        {
            var sweep = shineMask.rectTransform.rect.width;
            return Tween.Run(targetCycleSeconds, Ease.Linear, t =>
            {
                var pulse = Ease.Bump(t);
                rectTransform.localScale = Vector3.one * (targetScale + 0.05f * pulse);
                SetAlpha(glow, 0.75f + 0.25f * pulse);
                shine.anchoredPosition = new Vector2(Mathf.Lerp(-sweep, sweep, Ease.InOutQuad(Mathf.Min(1f, t * 1.4f))), 0f);
                sparkles.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 25f * t);
                sparkles.rectTransform.localScale = Vector3.one * (0.9f + 0.2f * pulse);
                SetAlpha(sparkles, 0.6f + 0.4f * pulse);
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
