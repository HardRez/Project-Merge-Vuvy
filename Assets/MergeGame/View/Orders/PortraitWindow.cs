using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Orders
{
    /// <summary>
    /// The shape a customer portrait is masked to (the graphic of its uGUI <see cref="Mask"/>): the lower half of a circle
    /// as wide as the rect, sitting on the rect's bottom edge, with the rect's full width open above its centre. The
    /// customer's shoulders stay inside the round frame while its head may rise above it.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PortraitWindow : MaskableGraphic
    {
        [Tooltip("Segments of the half circle; more gives a smoother stencil edge.")]
        [Range(8, 128)]
        [SerializeField] private int segments = 48;

        /// <summary>The centre of the circle in the rect's local space.</summary>
        public Vector2 CircleCenter
        {
            get
            {
                var rect = rectTransform.rect;
                return new Vector2(rect.center.x, rect.yMin + rect.width * 0.5f);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            var radius = rect.width * 0.5f;
            var center = new Vector2(rect.center.x, rect.yMin + radius);
            var top = Mathf.Max(rect.yMax, center.y);

            AddVertex(vh, center);
            // From the left end of the diameter round the bottom to the right end, then up and across the open top.
            for (var i = 0; i <= segments; i++)
            {
                var angle = Mathf.PI + Mathf.PI * i / segments;
                AddVertex(vh, center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            AddVertex(vh, new Vector2(rect.xMax, top));
            AddVertex(vh, new Vector2(rect.xMin, top));
            AddVertex(vh, new Vector2(rect.xMin, center.y));

            var outline = vh.currentVertCount - 1;
            for (var i = 1; i < outline; i++)
                vh.AddTriangle(0, i + 1, i);
        }

        private void AddVertex(VertexHelper vh, Vector2 position) => vh.AddVert(position, color, Vector4.zero);
    }
}
