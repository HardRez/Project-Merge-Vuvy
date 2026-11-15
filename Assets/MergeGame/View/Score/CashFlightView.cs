using System.Collections;
using System.Collections.Generic;
using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Score
{
    /// <summary>
    /// The money of a completed order flying into the score pill: a few bills burst out of the order's reward and arc
    /// into the pill one after another, and the pill counts each bill's share only when it lands. The order's points are
    /// announced to the pill on delivery (<see cref="Expect"/>), before the score changes, so the pill holds them back
    /// until the bills take off (<see cref="PayOut"/>) and arrive.
    /// </summary>
    public sealed class CashFlightView : MonoBehaviour
    {
        [SerializeField] private ScoreView score;
        [SerializeField] private Sprite billSprite;
        [SerializeField] private Vector2 billSize = new Vector2(96f, 70f);

        [Header("Bills per order")]
        [SerializeField] private int pointsPerBill = 20;
        [SerializeField] private int minBills = 3;
        [SerializeField] private int maxBills = 6;

        [Header("Motion")]
        [Tooltip("How far the bills scatter around the reward before they fly to the pill.")]
        [SerializeField] private float burstRadius = 80f;

        [SerializeField] private float burstSeconds = 0.2f;
        [SerializeField] private float flightSeconds = 0.5f;

        [Tooltip("Between one bill and the next taking off, so they land one after another.")]
        [SerializeField] private float staggerSeconds = 0.07f;

        [Tooltip("How far a bill's arc bends sideways, as a share of its flight's length.")]
        [SerializeField] private float arcBend = 0.25f;

        [Tooltip("A bill's size on landing, relative to its size in flight.")]
        [SerializeField] private float landingScale = 1.2f;

        private readonly Dictionary<int, int> owed = new Dictionary<int, int>();
        private readonly List<Image> idleBills = new List<Image>();
        private readonly List<Image> flyingBills = new List<Image>();
        private int pointsInFlight;

        /// <summary>Bills on their way to the pill right now.</summary>
        public int BillsInFlight => flyingBills.Count;

        /// <summary>The points of the order in this slot will fly to the pill; call it before the score changes.</summary>
        public void Expect(int slotIndex, int points)
        {
            if (points <= 0)
                return;

            owed.TryGetValue(slotIndex, out var already);
            owed[slotIndex] = already + points;
            score.ExpectIncoming(points);
        }

        /// <summary>The bills of this slot's order take off from <paramref name="worldPosition"/>.</summary>
        public void PayOut(int slotIndex, Vector3 worldPosition)
        {
            if (!owed.TryGetValue(slotIndex, out var points))
                return;

            owed.Remove(slotIndex);
            if (!isActiveAndEnabled)
            {
                Release(points);
                return;
            }

            var count = Mathf.Clamp(Mathf.CeilToInt(points / (float)Mathf.Max(1, pointsPerBill)), minBills, maxBills);
            count = Mathf.Max(1, Mathf.Min(count, points));
            pointsInFlight += points;
            for (var i = 0; i < count; i++)
            {
                // Every bill carries an equal share; the last one also carries what does not divide evenly.
                var share = points / count + (i == count - 1 ? points % count : 0);
                StartCoroutine(Fly(TakeBill(), worldPosition, i * staggerSeconds, share));
            }
        }

        /// <summary>Hands the pill every point that has not flown yet, so a cancelled payout never leaves it short.</summary>
        public void SettleOwed()
        {
            foreach (var points in owed.Values)
                Release(points);
            owed.Clear();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            SettleOwed();
            Release(pointsInFlight);
            pointsInFlight = 0;
            foreach (var bill in flyingBills.ToArray())
                ReturnBill(bill);
        }

        private IEnumerator Fly(Image bill, Vector3 origin, float delay, int share)
        {
            if (delay > 0f)
                yield return Tween.Delay(delay);

            var rect = bill.rectTransform;
            bill.gameObject.SetActive(true);
            rect.SetAsLastSibling();

            // Out of the reward to a spot around it, mostly upwards, towards the pill.
            var scatter = Random.insideUnitCircle * burstRadius;
            scatter.y = Mathf.Abs(scatter.y) * 0.6f + burstRadius * 0.4f;
            var lossyScale = transform.lossyScale.x;
            var burstTo = origin + (Vector3)(scatter * lossyScale);
            var spin = Random.Range(-25f, 25f);
            yield return Tween.Run(burstSeconds, Ease.OutCubic, t =>
            {
                rect.position = Vector3.LerpUnclamped(origin, burstTo, t);
                rect.localScale = Vector3.one * Ease.OutBack(t);
                rect.localRotation = Quaternion.Euler(0f, 0f, spin * t);
            });

            // Then along a bent arc into the pill, speeding up as it goes.
            var start = rect.position;
            var target = score.CashTarget;
            var travel = target - start;
            var side = new Vector3(-travel.y, travel.x, 0f) * (arcBend * (Random.value < 0.5f ? -1f : 1f));
            var control = start + travel * 0.5f + side;
            yield return Tween.Run(flightSeconds, Ease.InQuad, t =>
            {
                var a = Vector3.LerpUnclamped(start, control, t);
                var b = Vector3.LerpUnclamped(control, target, t);
                rect.position = Vector3.LerpUnclamped(a, b, t);
                rect.localScale = Vector3.one * Mathf.Lerp(1f, landingScale, t);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(spin, 0f, t));
            });

            ReturnBill(bill);
            pointsInFlight -= share;
            Release(share);
        }

        // The pill may already be gone while the screen is being torn down.
        private void Release(int points)
        {
            if (score != null)
                score.ReceiveIncoming(points);
        }

        private Image TakeBill()
        {
            Image bill;
            if (idleBills.Count > 0)
            {
                bill = idleBills[idleBills.Count - 1];
                idleBills.RemoveAt(idleBills.Count - 1);
            }
            else
            {
                bill = new GameObject("Bill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                bill.transform.SetParent(transform, false);
                bill.sprite = billSprite;
                bill.preserveAspect = true;
                bill.raycastTarget = false;
                bill.rectTransform.sizeDelta = billSize;
            }

            bill.gameObject.SetActive(false);
            bill.rectTransform.localScale = Vector3.zero;
            flyingBills.Add(bill);
            return bill;
        }

        private void ReturnBill(Image bill)
        {
            flyingBills.Remove(bill);
            bill.gameObject.SetActive(false);
            idleBills.Add(bill);
        }
    }
}
