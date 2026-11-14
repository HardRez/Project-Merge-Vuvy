using UnityEngine;

namespace MergeGame.View.Common
{
    public static class RectTransformExtensions
    {
        /// <summary>Stretches the rect over a normalised area of its parent, with no margins.</summary>
        public static void StretchOver(this RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Moves the rect under <paramref name="parent"/> as a free-floating rect (centre anchors) of the same size and at
        /// the same world position, so it can travel anywhere without the parent's layout pulling it.
        /// </summary>
        public static void DetachInto(this RectTransform rectTransform, RectTransform parent)
        {
            var size = rectTransform.rect.size;
            var worldPosition = rectTransform.position;
            rectTransform.SetParent(parent, worldPositionStays: true);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            rectTransform.position = worldPosition;
            rectTransform.SetAsLastSibling();
        }

        /// <returns>The point in the rect's normalised space (0..1 inside, but not clamped), or null if it is not on the rect's plane.</returns>
        public static Vector2? ScreenToNormalized(this RectTransform rectTransform, Vector2 screenPoint, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var local))
                return null;

            var rect = rectTransform.rect;
            return new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
        }

        public static Vector3 NormalizedToWorld(this RectTransform rectTransform, Vector2 normalized)
        {
            var rect = rectTransform.rect;
            return rectTransform.TransformPoint(new Vector3(rect.xMin + normalized.x * rect.width, rect.yMin + normalized.y * rect.height, 0f));
        }
    }
}
