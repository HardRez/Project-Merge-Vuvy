using System;
using System.Collections;
using System.Collections.Generic;
using MergeGame.Data.Visuals;
using MergeGame.Presentation.Orders;
using MergeGame.View.Animation;
using MergeGame.View.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Orders
{
    /// <summary>
    /// One customer's order. A completed order first celebrates (once the delivered items have arrived) and only then
    /// shows the next order; cards that arrive meanwhile wait until the celebration is over.
    /// </summary>
    public sealed class OrderCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform body;
        [SerializeField] private CanvasGroup content;
        [SerializeField] private Image avatar;
        [SerializeField] private RectTransform requirementContainer;
        [SerializeField] private RequirementSlotView requirementPrefab;
        [SerializeField] private TMP_Text rewardLabel;
        [SerializeField] private string rewardFormat = "+{0}";
        [SerializeField] private TactileButton deliverButton;

        [Tooltip("Gently pulses while the order can be delivered.")]
        [SerializeField] private RectTransform deliverPulse;

        [SerializeField] private Image celebrationGlow;
        [SerializeField] private TMP_Text celebrationLabel;

        [Header("Timing (seconds)")]
        [Tooltip("Lets the delivered items reach the customer before the celebration starts.")]
        [SerializeField] private float celebrationDelaySeconds = 0.5f;

        [SerializeField] private float celebrationSeconds = 0.9f;
        [SerializeField] private float swapSeconds = 0.3f;
        [SerializeField] private float pulseSeconds = 0.9f;

        private readonly List<RequirementSlotView> requirementSlots = new List<RequirementSlotView>();
        private IItemVisuals visuals;
        private TweenChannel celebration;
        private TweenChannel pulse;
        private OrderCard pendingCard;
        private bool isCelebrating;

        public event Action DeliverClicked;

        public OrderCard Card { get; private set; }
        public bool CanDeliver => deliverButton.interactable;
        public Vector3 DeliveryTarget => avatar.rectTransform.TransformPoint(avatar.rectTransform.rect.center);

        private void Awake()
        {
            celebration = new TweenChannel(this);
            pulse = new TweenChannel(this);
            deliverButton.onClick.AddListener(() => DeliverClicked?.Invoke());
            celebrationGlow.gameObject.SetActive(false);
            celebrationLabel.gameObject.SetActive(false);
        }

        public void Initialize(IItemVisuals itemVisuals) => visuals = itemVisuals;

        public void Show(OrderCard card)
        {
            if (isCelebrating)
                pendingCard = card;
            else
                Apply(card);
        }

        public void Celebrate(int awardedPoints)
        {
            isCelebrating = true;
            pendingCard = null;
            SetDeliverable(false);
            if (!celebration.Play(PlayCelebration(awardedPoints)))
                FinishCelebration();
        }

        private void Apply(OrderCard card)
        {
            Card = card;
            avatar.sprite = visuals.CustomerCount > 0 ? visuals.GetCustomerAvatar(card.CustomerIndex % visuals.CustomerCount) : null;
            rewardLabel.text = string.Format(rewardFormat, card.RewardPoints);

            for (var i = 0; i < card.Requirements.Count; i++)
            {
                var requirement = card.Requirements[i];
                RequirementSlot(i).Show(visuals.GetIcon(requirement.ItemId), requirement.IsFulfilled);
            }

            for (var i = 0; i < requirementSlots.Count; i++)
                requirementSlots[i].gameObject.SetActive(i < card.Requirements.Count);

            SetDeliverable(card.CanDeliver);
        }

        private RequirementSlotView RequirementSlot(int index)
        {
            while (requirementSlots.Count <= index)
                requirementSlots.Add(Instantiate(requirementPrefab, requirementContainer));
            requirementSlots[index].gameObject.SetActive(true);
            return requirementSlots[index];
        }

        private void SetDeliverable(bool canDeliver)
        {
            deliverButton.interactable = canDeliver;
            deliverPulse.localScale = Vector3.one;
            if (canDeliver)
                pulse.Play(Tween.Loop(Pulse));
            else
                pulse.Stop();
        }

        private IEnumerator Pulse() =>
            Tween.Run(pulseSeconds, Ease.Linear, t => deliverPulse.localScale = Vector3.one * (1f + 0.06f * Ease.Bump(t)));

        private IEnumerator PlayCelebration(int awardedPoints)
        {
            yield return Tween.Delay(celebrationDelaySeconds);

            celebrationGlow.gameObject.SetActive(true);
            celebrationLabel.gameObject.SetActive(awardedPoints > 0);
            celebrationLabel.text = string.Format(rewardFormat, awardedPoints);
            var labelTransform = celebrationLabel.rectTransform;
            var labelStart = labelTransform.anchoredPosition;
            yield return Tween.Run(celebrationSeconds, Ease.Linear, t =>
            {
                body.localScale = Vector3.one * (1f + 0.08f * Ease.Bump(Mathf.Min(1f, t * 2.5f)));
                content.alpha = Mathf.Lerp(1f, 0.3f, Ease.OutQuad(Mathf.Min(1f, t * 3f)));
                celebrationGlow.color = WithAlpha(celebrationGlow.color, 0.75f * Ease.Bump(t));
                celebrationGlow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.25f, Ease.OutQuad(t));
                labelTransform.anchoredPosition = labelStart + Vector2.up * (90f * Ease.OutCubic(t));
                labelTransform.localScale = Vector3.one * Ease.OutBack(Mathf.Min(1f, t * 3f));
                celebrationLabel.alpha = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
            });
            labelTransform.anchoredPosition = labelStart;

            var dimmed = content.alpha;
            yield return Tween.Run(swapSeconds * 0.5f, Ease.InQuad, t => content.alpha = dimmed * (1f - t));
            FinishCelebration();
            yield return Tween.Run(swapSeconds, Ease.Linear, t =>
            {
                content.alpha = Ease.OutQuad(t);
                body.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, Ease.OutBack(t));
            });
        }

        private void FinishCelebration()
        {
            isCelebrating = false;
            celebrationGlow.gameObject.SetActive(false);
            celebrationLabel.gameObject.SetActive(false);
            body.localScale = Vector3.one;
            content.alpha = 1f;
            if (pendingCard != null)
                Apply(pendingCard);
            pendingCard = null;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
