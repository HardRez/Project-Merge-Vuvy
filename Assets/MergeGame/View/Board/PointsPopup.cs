using System;
using System.Collections;
using MergeGame.View.Animation;
using TMPro;
using UnityEngine;
using Zenject;

namespace MergeGame.View.Board
{
    /// <summary>"+25" that pops up at a merge and floats away.</summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class PointsPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string format = "+{0}";
        [SerializeField] private float rise = 170f;
        [SerializeField] private float seconds = 1f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private TweenChannel motion;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvasGroup = GetComponent<CanvasGroup>();
            motion = new TweenChannel(this);
        }

        public void Play(Vector3 worldPosition, int points, Action finished)
        {
            label.text = string.Format(format, points);
            rectTransform.position = worldPosition;
            if (!motion.Play(Float(rectTransform.anchoredPosition, finished)))
                finished?.Invoke();
        }

        private IEnumerator Float(Vector2 start, Action finished)
        {
            yield return Tween.Run(seconds, Ease.Linear, t =>
            {
                rectTransform.anchoredPosition = start + Vector2.up * (rise * Ease.OutCubic(t));
                rectTransform.localScale = Vector3.one * Ease.OutBack(Mathf.Min(1f, t * 4f));
                canvasGroup.alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            });

            finished?.Invoke();
        }

        public sealed class Pool : MonoMemoryPool<PointsPopup>
        {
        }
    }
}
