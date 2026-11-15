using UnityEngine;

namespace MergeGame.View.Common
{
    /// <summary>
    /// Widens the rect sideways to the edges of its canvas, keeping its height and vertical place, so a strip laid out
    /// inside the capped game column (the shop counter) still runs from one edge of the screen to the other.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CanvasWidthFitter : MonoBehaviour
    {
        private readonly Vector3[] corners = new Vector3[4];

        private void OnEnable() => Fit();

        // The column's layout moves this rect's parent without telling it, so the edges are checked every frame.
        private void LateUpdate() => Fit();

        private void Fit()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || !(transform.parent is RectTransform parent))
                return;

            ((RectTransform)canvas.rootCanvas.transform).GetWorldCorners(corners);
            var left = parent.InverseTransformPoint(corners[0]).x - parent.rect.xMin;
            var right = parent.InverseTransformPoint(corners[2]).x - parent.rect.xMax;

            var rect = (RectTransform)transform;
            if (Mathf.Approximately(rect.anchorMin.x, 0f) && Mathf.Approximately(rect.anchorMax.x, 1f)
                && Mathf.Abs(rect.offsetMin.x - left) < 0.01f && Mathf.Abs(rect.offsetMax.x - right) < 0.01f)
                return;

            rect.anchorMin = new Vector2(0f, rect.anchorMin.y);
            rect.anchorMax = new Vector2(1f, rect.anchorMax.y);
            rect.offsetMin = new Vector2(left, rect.offsetMin.y);
            rect.offsetMax = new Vector2(right, rect.offsetMax.y);
        }
    }
}
