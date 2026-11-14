using UnityEngine;
using UnityEngine.EventSystems;

namespace MergeGame.View.Common
{
    /// <summary>
    /// Stretches the rect over its parent but caps its width, centred, so wide screens (tablets) keep phone-like
    /// proportions instead of stretching every bar.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MaxWidthLimiter : UIBehaviour
    {
        [SerializeField] private float maxWidth = 1180f;

        private bool isApplying;

        protected override void OnEnable()
        {
            base.OnEnable();
            Apply();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            // Changing the offsets below raises this callback again.
            if (!isApplying)
                Apply();
        }

        private void Apply()
        {
            if (!(transform.parent is RectTransform parent))
                return;

            isApplying = true;
            try
            {
                Fit(parent);
            }
            finally
            {
                isApplying = false;
            }
        }

        private void Fit(RectTransform parent)
        {
            var rectTransform = (RectTransform)transform;
            var margin = Mathf.Max(0f, (parent.rect.width - maxWidth) * 0.5f);
            var offsetMin = new Vector2(margin, 0f);
            var offsetMax = new Vector2(-margin, 0f);
            if (rectTransform.anchorMin == Vector2.zero && rectTransform.anchorMax == Vector2.one
                && rectTransform.offsetMin == offsetMin && rectTransform.offsetMax == offsetMax)
                return;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
