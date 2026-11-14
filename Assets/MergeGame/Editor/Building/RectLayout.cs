using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.Editor.Building
{
    /// <summary>Fluent anchoring helpers, so screen code reads as "where" rather than as anchor arithmetic.</summary>
    internal static class RectLayout
    {
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 Top = new Vector2(0.5f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 Left = new Vector2(0f, 0.5f);
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 Right = new Vector2(1f, 0.5f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        /// <summary>Fills the parent, inset by the given margins (negative margins overhang).</summary>
        public static RectTransform Stretch(this RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = Center;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>Fills the parent, inset by the same margin on every side.</summary>
        public static RectTransform Inset(this RectTransform rect, float margin) => rect.Stretch(margin, margin, margin, margin);

        /// <summary>Fills a normalised area of the parent (e.g. 0.1..0.9) with no margins.</summary>
        public static RectTransform Span(this RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = Center;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>A fixed-size rect pinned to <paramref name="anchor"/>; the pivot defaults to the anchor, so the offset is from that edge.</summary>
        public static RectTransform Pin(this RectTransform rect, Vector2 anchor, Vector2 size, Vector2 offset = default, Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = offset;
            return rect;
        }

        /// <summary>Spans the parent's width at a fixed height along its top (<paramref name="fromTop"/> true) or bottom edge.</summary>
        public static RectTransform Band(this RectTransform rect, bool fromTop, float height, float offset = 0f, float sideMargin = 0f)
        {
            var edge = fromTop ? 1f : 0f;
            rect.anchorMin = new Vector2(0f, edge);
            rect.anchorMax = new Vector2(1f, edge);
            rect.pivot = new Vector2(0.5f, edge);
            rect.sizeDelta = new Vector2(-2f * sideMargin, height);
            rect.anchoredPosition = new Vector2(0f, fromTop ? -offset : offset);
            return rect;
        }

        /// <summary>Sizes the rect inside a layout group.</summary>
        public static LayoutElement Prefer(this Component component, float width = -1f, float height = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            var element = component.GetComponent<LayoutElement>();
            if (element == null)
                element = component.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.minWidth = width;
            element.minHeight = height;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }
    }
}
