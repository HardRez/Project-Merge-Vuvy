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
    /// One customer's order, in front of the counter: the requested items on a tray, the reward and the deliver button.
    /// Its customer stands behind the counter in a row of its own (see <see cref="OrdersView"/>). A completed order first
    /// celebrates once the delivered items have arrived: the customer leaves and fades out while the tray fades out
    /// completely, and only when both are gone do the next order and its customer come in, so the two never overlap.
    /// Cards that arrive meanwhile wait until the celebration is over.
    /// </summary>
    public sealed class OrderCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform body;
        [SerializeField] private CanvasGroup content;

        [Tooltip("The tray the requested items stand on; delivered items fly onto it.")]
        [SerializeField] private RectTransform requirementContainer;

        [SerializeField] private RequirementSlotView requirementPrefab;
        [SerializeField] private TMP_Text rewardLabel;
        [SerializeField] private string rewardFormat = "+{0}";

        [Tooltip("Where the order's money takes off from when it is paid: the reward pill.")]
        [SerializeField] private RectTransform payoutOrigin;
        [SerializeField] private TactileButton deliverButton;

        [Tooltip("Holds the deliver button: shown only while the order can be delivered, popping in and then gently pulsing.")]
        [SerializeField] private RectTransform deliverPulse;

        [SerializeField] private Image celebrationGlow;
        [SerializeField] private TMP_Text celebrationLabel;

        [Header("Timing (seconds)")]
        [Tooltip("Lets the delivered items reach the tray before the celebration starts.")]
        [SerializeField] private float celebrationDelaySeconds = 0.5f;

        [Tooltip("From the customer starting to leave until the next order comes in; the points rise and the glow flashes meanwhile.")]
        [SerializeField] private float celebrationSeconds = 0.6f;

        [Tooltip("How fast the tray fades out completely once the customer starts to leave; shorter than the celebration, so " +
                 "nothing of it is left when the next order comes in.")]
        [SerializeField] private float trayFadeSeconds = 0.35f;

        [SerializeField] private float swapSeconds = 0.3f;
        [SerializeField] private float pulseSeconds = 0.9f;
        [SerializeField] private float deliverPopInSeconds = 0.32f;
        [SerializeField] private float deliverPopOutSeconds = 0.15f;

        private readonly List<RequirementSlotView> requirementSlots = new List<RequirementSlotView>();
        private IItemVisuals visuals;
        private CustomerPortraitView customer;
        private TweenChannel celebration;
        private TweenChannel pulse;
        private OrderCard pendingCard;
        private bool isCelebrating;
        private bool customerLeft = true;
        private bool deliverShown;

        public event Action DeliverClicked;

        /// <summary>The customer pays as it starts to leave: the money takes off from this world position.</summary>
        public event Action<Vector3> PaidOut;

        /// <summary>A new customer came in behind the counter for this card's order.</summary>
        public event Action CustomerArrived;

        public OrderCard Card { get; private set; }
        public bool CanDeliver => deliverButton.interactable;

        /// <summary>True while the deliver button is (or is popping) on screen: only while the order can be delivered.</summary>
        public bool IsDeliverButtonShown => deliverShown;
        public Vector3 DeliveryTarget => requirementContainer.TransformPoint(requirementContainer.rect.center);

        /// <summary>The centre of the reward pill, where the money of a completed order takes off.</summary>
        public Vector3 PayoutOrigin
        {
            get
            {
                var origin = payoutOrigin != null ? payoutOrigin : body;
                return origin.TransformPoint(origin.rect.center);
            }
        }

        public CustomerPortraitView Customer => customer;

        /// <summary>How visible the tray, reward and deliver button are (they fade out on delivery and in for the next order).</summary>
        public float ContentAlpha => content.alpha;

        private void Awake()
        {
            celebration = new TweenChannel(this);
            pulse = new TweenChannel(this);
            deliverButton.onClick.AddListener(() => DeliverClicked?.Invoke());
            celebrationGlow.gameObject.SetActive(false);
            celebrationLabel.gameObject.SetActive(false);
            deliverPulse.gameObject.SetActive(false);
        }

        /// <param name="slotCustomer">The customer standing behind the counter above this card.</param>
        public void Initialize(IItemVisuals itemVisuals, CustomerPortraitView slotCustomer)
        {
            visuals = itemVisuals;
            customer = slotCustomer;
        }

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
            ShowCustomer(card);
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

        private void ShowCustomer(OrderCard card)
        {
            if (!customerLeft)
            {
                customer.ChangeMood(card.Mood);
                return;
            }

            customerLeft = false;
            if (visuals.CustomerCount == 0)
            {
                customer.Arrive(null, null, card.Mood);
            }
            else
            {
                var index = card.CustomerIndex % visuals.CustomerCount;
                customer.Arrive(visuals.GetCustomerPortrait(index), visuals.GetCustomerAvatar(index), card.Mood);
            }

            CustomerArrived?.Invoke();
        }

        private RequirementSlotView RequirementSlot(int index)
        {
            while (requirementSlots.Count <= index)
                requirementSlots.Add(Instantiate(requirementPrefab, requirementContainer));
            requirementSlots[index].gameObject.SetActive(true);
            return requirementSlots[index];
        }

        /// <summary>
        /// The deliver button is only on screen while the order can be delivered: it pops in when the order becomes
        /// deliverable, then pulses, and shrinks away when it no longer is (or once it was delivered).
        /// </summary>
        private void SetDeliverable(bool canDeliver)
        {
            deliverButton.interactable = canDeliver;
            if (canDeliver == deliverShown)
                return;

            deliverShown = canDeliver;
            if (canDeliver)
            {
                deliverPulse.gameObject.SetActive(true);
                deliverPulse.localScale = Vector3.zero;
                if (!pulse.Play(PopInAndPulse()))
                    deliverPulse.localScale = Vector3.one;
            }
            else if (!pulse.Play(PopOut()))
            {
                HideDeliverButton();
            }
        }

        private IEnumerator PopInAndPulse()
        {
            yield return Tween.Run(deliverPopInSeconds, Ease.OutBack, t => deliverPulse.localScale = Vector3.one * t);
            yield return Tween.Loop(Pulse);
        }

        private IEnumerator PopOut()
        {
            var from = deliverPulse.localScale.x;
            yield return Tween.Run(deliverPopOutSeconds, Ease.InQuad, t => deliverPulse.localScale = Vector3.one * (from * (1f - t)));
            HideDeliverButton();
        }

        private void HideDeliverButton()
        {
            deliverPulse.localScale = Vector3.one;
            deliverPulse.gameObject.SetActive(false);
        }

        private IEnumerator Pulse() =>
            Tween.Run(pulseSeconds, Ease.Linear, t => deliverPulse.localScale = Vector3.one * (1f + 0.06f * Ease.Bump(t)));

        private IEnumerator PlayCelebration(int awardedPoints)
        {
            yield return Tween.Delay(celebrationDelaySeconds);

            customer.Leave();
            PaidOut?.Invoke(PayoutOrigin);
            celebrationGlow.gameObject.SetActive(true);
            celebrationLabel.gameObject.SetActive(awardedPoints > 0);
            celebrationLabel.text = string.Format(rewardFormat, awardedPoints);
            var labelTransform = celebrationLabel.rectTransform;
            var labelStart = labelTransform.anchoredPosition;
            var trayFadeShare = Mathf.Clamp01(trayFadeSeconds / Mathf.Max(0.01f, celebrationSeconds));
            yield return Tween.Run(celebrationSeconds, Ease.Linear, t =>
            {
                body.localScale = Vector3.one * (1f + 0.08f * Ease.Bump(Mathf.Min(1f, t * 2.5f)));
                content.alpha = t >= trayFadeShare ? 0f : 1f - Ease.OutQuad(t / trayFadeShare);
                celebrationGlow.color = WithAlpha(celebrationGlow.color, 0.75f * Ease.Bump(t));
                celebrationGlow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.25f, Ease.OutQuad(t));
                labelTransform.anchoredPosition = labelStart + Vector2.up * (90f * Ease.OutCubic(t));
                labelTransform.localScale = Vector3.one * Ease.OutBack(Mathf.Min(1f, t * 3f));
                celebrationLabel.alpha = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
            });
            labelTransform.anchoredPosition = labelStart;

            content.alpha = 0f;
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
            customerLeft = true;
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
