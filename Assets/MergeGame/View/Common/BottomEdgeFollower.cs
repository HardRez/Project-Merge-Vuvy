using UnityEngine;

namespace MergeGame.View.Common
{
    /// <summary>
    /// Keeps the rect's bottom edge level with a height inside another rect, so a backdrop drawn behind the whole screen
    /// (the shop wall) reaches down exactly to a strip that the game column lays out (the counter), on every screen.
    /// The rect should stretch over its parent vertically; only its bottom offset is changed.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class BottomEdgeFollower : MonoBehaviour
    {
        [SerializeField] private RectTransform target;

        [Tooltip("Where in the target the bottom edge goes: 0 is the target's bottom, 1 its top.")]
        [Range(0f, 1f)]
        [SerializeField] private float heightInTarget = 0.5f;

        private void OnEnable() => Follow();

        // The target moves with the column's layout, which tells this rect nothing, so it is checked every frame.
        private void LateUpdate() => Follow();

        private void Follow()
        {
            if (target == null || !(transform.parent is RectTransform parent))
                return;

            var targetRect = target.rect;
            var level = target.TransformPoint(new Vector3(0f, Mathf.Lerp(targetRect.yMin, targetRect.yMax, heightInTarget), 0f));
            var bottom = parent.InverseTransformPoint(level).y - Mathf.Lerp(parent.rect.yMin, parent.rect.yMax, ((RectTransform)transform).anchorMin.y);

            var rect = (RectTransform)transform;
            if (Mathf.Abs(rect.offsetMin.y - bottom) < 0.01f)
                return;
            rect.offsetMin = new Vector2(rect.offsetMin.x, bottom);
        }
    }
}
