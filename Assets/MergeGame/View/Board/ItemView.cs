using System;
using System.Collections;
using MergeGame.Core.Items;
using MergeGame.View.Animation;
using MergeGame.View.Common;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MergeGame.View.Board
{
    /// <summary>
    /// One item on the board, or the invisible item of a closed cell. Its identity (<see cref="ItemId"/>, lock and hidden
    /// state) changes the moment a command arrives;
    /// animations only decide how the change looks. Every command first interrupts the running animation and reveals
    /// the current identity, then animates from the current pose to a rest pose, so overlapping commands can never
    /// leave the item with a stale icon, scale or position.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class ItemView : MonoBehaviour
    {
        [Tooltip("Scaled and rotated by the animations; holds every visual of the item.")]
        [SerializeField] private RectTransform body;

        [Tooltip("Spawn shine, hint and merge-target highlights; sits between the root and the body.")]
        [SerializeField] private ItemAccent accent;

        [SerializeField] private OrderMatchMark orderMark;

        [SerializeField] private Image icon;
        [SerializeField] private Image lockOverlay;

        [Tooltip("The bubble bursting when a merge frees the item.")]
        [SerializeField] private BubblePop bubblePop;

        [Tooltip("Multiplies the icon of a locked item, so it reads as held inside its bubble.")]
        [SerializeField] private Color lockedIconTint = new Color(0.76f, 0.76f, 0.8f, 1f);

        [Tooltip("How much the bubble swells while the other item flies into it, just before it bursts.")]
        [SerializeField] private float bubbleSwell = 0.1f;

        [SerializeField] private GameObject maxLevelBadge;
        [SerializeField] private Image burstGlow;
        [SerializeField] private Image burstSparkles;

        [Tooltip("A ring that spreads out from a merge, behind the new item.")]
        [SerializeField] private Image mergeRing;

        [Tooltip("The flash, rays, shock wave and flying stars of a merge.")]
        [SerializeField] private MergeBurst mergeBurst;

        [Header("Look")]

        [SerializeField] private float liftedScale = 1.2f;

        [Header("Timing (seconds)")]
        [SerializeField] private float spawnSeconds = 0.32f;
        [SerializeField] private float liftSeconds = 0.14f;
        [SerializeField] private float refuseSeconds = 0.4f;
        [SerializeField] private float mergeSeconds = 0.55f;

        [Header("Merge pop")]
        [Tooltip("How far the merged item squashes while the other item flies into it.")]
        [SerializeField] private float impactSquash = 0.1f;

        [Tooltip("Scale the new item grows from.")]
        [SerializeField] private float popFromScale = 0.5f;

        [Tooltip("Scale the new item overshoots to before it settles.")]
        [SerializeField] private float popPeakScale = 1.28f;

        [Tooltip("Share of the merge time spent growing to the peak; the rest is a damped wobble back to rest.")]
        [Range(0.1f, 0.6f)]
        [SerializeField] private float popRiseShare = 0.24f;

        [Tooltip("Stretch along one axis and squeeze along the other while it pops (0 keeps it uniform).")]
        [SerializeField] private float popStretch = 0.12f;

        [Tooltip("Wobbles before it rests.")]
        [SerializeField] private float popWobbles = 1.25f;

        [SerializeField] private Color mergeRingColor = new Color(1f, 0.9f, 0.55f, 1f);
        [SerializeField] private float mergeRingSeconds = 0.45f;
        [SerializeField] private float vanishSeconds = 0.24f;
        [Tooltip("The small settle when the sand that covered the item has poured out.")]
        [SerializeField] private float revealSettleSeconds = 0.25f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private TweenChannel motion;
        private Sprite iconSprite;

        public ItemId ItemId { get; private set; }
        public bool IsLocked { get; private set; }

        /// <summary>In a closed cell: <see cref="ItemId"/> is unknown to the view and nothing is drawn until it is revealed.</summary>
        public bool IsHidden { get; private set; }
        public bool IsMaxLevel { get; private set; }
        public bool IsLifted { get; private set; }

        /// <summary>The icon currently on screen; differs from the identity only while a merge is about to reveal it.</summary>
        public Sprite DisplayedIcon => icon.sprite;

        public Color DisplayedIconColor => icon.color;

        public Vector3 WorldCenter => rectTransform.TransformPoint(rectTransform.rect.center);

        public ItemAccent Accent => accent;

        /// <summary>The part the animations scale; for tests that follow an animation frame by frame.</summary>
        public Transform Body => body;

        public Image MergeRing => mergeRing;

        public MergeBurst MergeBurst => mergeBurst;

        public BubblePop BubblePop => bubblePop;

        public Image LockOverlay => lockOverlay;
        public OrderMatchMark OrderMark => orderMark;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvasGroup = GetComponent<CanvasGroup>();
            motion = new TweenChannel(this);
        }

        public void Show(ItemId itemId, Sprite sprite, bool isLocked, bool isMaxLevel)
        {
            SetIdentity(itemId, sprite, isLocked, isMaxLevel);
            Interrupt();
        }

        /// <summary>Draws nothing: the board's cover buries the cell, and nothing about the content may show.</summary>
        public void ShowHidden()
        {
            SetIdentity(default, null, isLocked: true, isMaxLevel: false);
            IsHidden = true;
            Interrupt();
        }

        /// <summary>
        /// The closed cell opens into a locked item. It appears after <paramref name="delay"/> seconds, still under the
        /// pouring sand, which uncovers it; interrupting shows it immediately.
        /// </summary>
        public void Reveal(ItemId itemId, Sprite sprite, bool isMaxLevel, float delay)
        {
            Interrupt();
            SetIdentity(itemId, sprite, isLocked: true, isMaxLevel);
            if (!motion.Play(PlayReveal(delay)))
                RevealIdentity();
        }

        /// <summary>A small, restrained settle once the sand that covered the item is gone.</summary>
        public void PlaySettle()
        {
            Interrupt();
            motion.Play(Tween.Run(revealSettleSeconds, Ease.Linear, t => body.localScale = Vector3.one * (1f + 0.05f * Ease.Bump(t))));
        }

        /// <summary>Snaps the item into the cell, at rest.</summary>
        public void Place(RectTransform layer, CellRect cell)
        {
            Interrupt();
            rectTransform.SetParent(layer, worldPositionStays: false);
            rectTransform.StretchOver(cell.Min, cell.Max);
            SetRestPose();
        }

        public void PlaySpawn()
        {
            Interrupt();
            body.localScale = Vector3.zero;
            motion.Play(Tween.Run(spawnSeconds, Ease.Linear, t =>
            {
                body.localScale = Vector3.one * Ease.OutBack(t);
                SetAlpha(burstGlow, Ease.Bump(t) * 0.6f);
            }));
        }

        /// <summary>Picks the item up: it moves to the drag layer and grows.</summary>
        public void Lift(RectTransform dragLayer)
        {
            Interrupt();
            accent.Stop();
            IsLifted = true;
            rectTransform.DetachInto(dragLayer);

            var startScale = body.localScale.x;
            motion.Play(Tween.Run(liftSeconds, Ease.OutBack,
                t => body.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, liftedScale, t)));
        }

        public void DragTo(Vector3 worldPosition) => rectTransform.position = worldPosition;

        /// <summary>Glides from wherever the item is (lifted or not) into the cell and settles there.</summary>
        public void Land(RectTransform layer, CellRect cell, float seconds)
        {
            Interrupt();
            IsLifted = false;

            var worldPosition = rectTransform.position;
            rectTransform.SetParent(layer, worldPositionStays: true);
            rectTransform.StretchOver(cell.Min, cell.Max);
            rectTransform.position = worldPosition;

            var startOffset = rectTransform.anchoredPosition;
            var startScale = body.localScale.x;
            motion.Play(Tween.Run(seconds, Ease.Linear, t =>
            {
                rectTransform.anchoredPosition = Vector2.LerpUnclamped(startOffset, Vector2.zero, Ease.OutCubic(t));
                body.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, Ease.OutBack(t));
            }));
        }

        /// <summary>A shake that says "no" (e.g. a locked item was grabbed).</summary>
        public void PlayRefuse()
        {
            Interrupt();
            var startScale = body.localScale.x;
            motion.Play(Tween.Run(refuseSeconds, Ease.Linear, t =>
            {
                var swing = Ease.Shake(t, 6);
                body.localRotation = Quaternion.Euler(0f, 0f, swing * 12f);
                body.anchoredPosition = new Vector2(swing * 10f, 0f);
                body.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, t);
            }));
        }

        /// <summary>
        /// Becomes the merge result now; the new icon is revealed with a pop once the consumed item has arrived, after
        /// <paramref name="revealDelay"/> seconds. Interrupting reveals it immediately.
        /// </summary>
        public void MergeInto(ItemId resultId, Sprite resultSprite, bool isMaxLevel, bool unlocked, float revealDelay)
        {
            Interrupt();
            accent.Stop();
            SetIdentity(resultId, resultSprite, isLocked: false, isMaxLevel);
            if (!motion.Play(PlayMerge(unlocked, revealDelay)))
                RevealIdentity();
        }

        /// <summary>
        /// Leaves the board: flies to <paramref name="worldTarget"/> above everything else in <paramref name="layer"/>,
        /// shrinking to <paramref name="endScale"/>, then calls <paramref name="arrived"/>.
        /// </summary>
        public void FlyAway(RectTransform layer, Vector3 worldTarget, float seconds, float delay, float endScale, Action arrived)
        {
            Interrupt();
            accent.Stop();
            IsLifted = false;
            rectTransform.DetachInto(layer);
            if (!motion.Play(PlayFlight(worldTarget, seconds, delay, endScale, arrived)))
                arrived?.Invoke();
        }

        /// <summary>Shrinks and fades out, then calls <paramref name="vanished"/>.</summary>
        public void Vanish(Action vanished)
        {
            Interrupt();
            accent.Stop();
            IsLifted = false;
            if (!motion.Play(PlayVanish(body.localScale.x, vanished)))
                vanished?.Invoke();
        }

        /// <summary>Clears every trace of the previous use; called by the pool.</summary>
        public void ResetForReuse()
        {
            motion.Stop();
            accent.Stop();
            orderMark.SetRest(false);
            IsLifted = false;
            SetRestPose();
            ResetEffects();
        }

        private void SetIdentity(ItemId itemId, Sprite sprite, bool isLocked, bool isMaxLevel)
        {
            ItemId = itemId;
            iconSprite = sprite;
            IsLocked = isLocked;
            IsMaxLevel = isMaxLevel;
            IsHidden = false;
        }

        private void Interrupt()
        {
            motion.Stop();
            RevealIdentity();
            ResetEffects();
            body.anchoredPosition = Vector2.zero;
            body.localRotation = Quaternion.identity;
            canvasGroup.alpha = 1f;
        }

        private void RevealIdentity()
        {
            icon.sprite = iconSprite;
            icon.enabled = !IsHidden;
            icon.color = IsLocked ? lockedIconTint : Color.white;
            lockOverlay.gameObject.SetActive(IsLocked && !IsHidden);
            maxLevelBadge.SetActive(IsMaxLevel && !IsHidden);
        }

        private void ResetEffects()
        {
            SetAlpha(lockOverlay, 1f);
            lockOverlay.rectTransform.localScale = Vector3.one;
            bubblePop.Stop();
            SetAlpha(burstGlow, 0f);
            burstGlow.rectTransform.localScale = Vector3.one;
            burstSparkles.gameObject.SetActive(false);
            mergeRing.gameObject.SetActive(false);
            mergeRing.rectTransform.localScale = Vector3.one;
            mergeBurst.Rest();
        }

        private void SetRestPose()
        {
            rectTransform.anchoredPosition = Vector2.zero;
            body.anchoredPosition = Vector2.zero;
            body.localScale = Vector3.one;
            body.localRotation = Quaternion.identity;
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// The merge as a hit: the old item squashes while the other one flies into it, then the new item pops out of it,
        /// stretching and squeezing, overshoots and wobbles back to rest, while a ring spreads out behind it and the
        /// <see cref="MergeBurst"/> flashes, throws its rays open and sends its stars flying.
        /// </summary>
        private IEnumerator PlayMerge(bool unlocked, float revealDelay)
        {
            // The previous icon (and bubble) stay visible until the consumed item lands on them, braced for the hit.
            // A bubble swells against the hit; the body squashing under it keeps the bubble's own size about the same.
            var startScale = body.localScale.x;
            yield return Tween.Run(revealDelay, Ease.Linear, t =>
            {
                body.localScale = Squashed(Mathf.Lerp(startScale, 1f - impactSquash * 0.5f, Ease.OutQuad(t)), impactSquash * Ease.OutQuad(t));
                if (unlocked)
                    lockOverlay.rectTransform.localScale = Vector3.one * (1f + bubbleSwell * Ease.InQuad(t));
            });

            // The bubble bursts the moment the new item appears; the item comes out in its own colours.
            RevealIdentity();
            if (unlocked)
                bubblePop.Play();
            burstSparkles.gameObject.SetActive(true);
            mergeBurst.Begin();
            mergeRing.gameObject.SetActive(true);
            mergeRing.color = mergeRingColor;
            var ringShare = Mathf.Min(1f, mergeRingSeconds / mergeSeconds);
            yield return Tween.Run(mergeSeconds, Ease.Linear, t =>
            {
                body.localScale = PopScale(t);
                var ring = Mathf.Clamp01(t / ringShare);
                mergeRing.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.9f, Ease.OutCubic(ring));
                SetAlpha(mergeRing, 0.95f * (1f - Ease.OutQuad(ring)));
                SetAlpha(burstGlow, Ease.Bump(t));
                burstGlow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.6f, Ease.OutQuad(t));
                SetAlpha(burstSparkles, Ease.Bump(Mathf.Min(1f, t * 1.3f)));
                burstSparkles.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.35f, Ease.OutCubic(t));
                burstSparkles.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 40f);
                mergeBurst.Apply(t);
            });

            body.localScale = Vector3.one;
            RevealIdentity();
            ResetEffects();
        }

        /// <summary>
        /// Grows from <see cref="popFromScale"/> to <see cref="popPeakScale"/> stretched upwards, then wobbles back to 1
        /// on a damped spring, squeezing in step; continuous throughout and exactly at rest at the end.
        /// </summary>
        private Vector3 PopScale(float t)
        {
            if (t < popRiseShare)
            {
                var rise = t / popRiseShare;
                // Tall and thin while it shoots up, round again as it reaches the peak.
                return Squashed(Mathf.Lerp(popFromScale, popPeakScale, Ease.OutQuad(rise)), -popStretch * (1f - rise));
            }

            var settle = (t - popRiseShare) / (1f - popRiseShare);
            var damping = Mathf.Exp(-4.5f * settle) * (1f - settle);
            var phase = 2f * Mathf.PI * popWobbles * settle;
            var scale = 1f + (popPeakScale - 1f) * damping * Mathf.Cos(phase);
            return Squashed(scale, popStretch * damping * Mathf.Sin(phase));
        }

        /// <summary>Positive <paramref name="squash"/> widens and flattens, negative narrows and stretches upwards.</summary>
        private static Vector3 Squashed(float scale, float squash) => new Vector3(scale * (1f + squash), scale * (1f - squash), 1f);

        private IEnumerator PlayReveal(float delay)
        {
            // Still invisible (the identity is not revealed yet) while the merge next to it plays.
            yield return Tween.Delay(delay);
            RevealIdentity();
        }

        private IEnumerator PlayFlight(Vector3 worldTarget, float seconds, float delay, float endScale, Action arrived)
        {
            yield return Tween.Delay(delay);

            var start = rectTransform.position;
            var startScale = body.localScale.x;
            // A slight sideways curve reads as a throw rather than a slide.
            var arc = Vector3.Cross(worldTarget - start, Vector3.forward) * 0.2f;
            yield return Tween.Run(seconds, Ease.Linear, t =>
            {
                var travelled = Ease.InOutQuad(t);
                rectTransform.position = Vector3.LerpUnclamped(start, worldTarget, travelled) + arc * Ease.Bump(t);
                body.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, travelled);
            });

            arrived?.Invoke();
        }

        private IEnumerator PlayVanish(float startScale, Action vanished)
        {
            yield return Tween.Run(vanishSeconds, Ease.InBack, t =>
            {
                body.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 0f, t);
                canvasGroup.alpha = 1f - Mathf.Clamp01(t);
            });

            vanished?.Invoke();
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        /// <summary>Board items are pooled: merges, deliveries and discards return them, spawns take them back out.</summary>
        public sealed class Pool : MonoMemoryPool<ItemView>
        {
            protected override void Reinitialize(ItemView item) => item.ResetForReuse();
        }
    }
}
