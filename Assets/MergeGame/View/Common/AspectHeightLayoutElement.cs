using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MergeGame.View.Common
{
    /// <summary>
    /// Tells a vertical layout group how tall this rect wants to be: just tall enough for the aspect-fitted content
    /// at the current width, so on tall screens the content keeps its natural size and the leftover height goes to
    /// the group's flexible elements instead of empty bands around the content. When space is short it shrinks down
    /// to <see cref="minHeight"/>.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class AspectHeightLayoutElement : UIBehaviour, ILayoutElement
    {
        [Tooltip("The fitter whose aspect ratio (width / height) the content keeps.")]
        [SerializeField] private AspectRatioFitter content;

        [SerializeField] private float minHeight = 420f;

        private float measuredWidth = -1f;

        float ILayoutElement.minWidth => -1f;
        float ILayoutElement.preferredWidth => -1f;
        float ILayoutElement.flexibleWidth => -1f;
        float ILayoutElement.maxWidth => -1f;
        float ILayoutElement.minHeight => minHeight;
        float ILayoutElement.preferredHeight => Mathf.Max(minHeight, Width / Mathf.Max(content.aspectRatio, 0.01f));
        float ILayoutElement.flexibleHeight => 0f;
        float ILayoutElement.maxHeight => -1f;
        int ILayoutElement.layoutPriority => 1;

        private float Width => ((RectTransform)transform).rect.width;

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical() => measuredWidth = Width;

        protected override void OnRectTransformDimensionsChange()
        {
            // Only a width change alters the preferred height (reacting to height changes would loop). A change made by
            // the running layout pass is measured by that same pass, which computes heights after widths.
            if (!Mathf.Approximately(Width, measuredWidth) && !CanvasUpdateRegistry.IsRebuildingLayout())
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }
    }
}
