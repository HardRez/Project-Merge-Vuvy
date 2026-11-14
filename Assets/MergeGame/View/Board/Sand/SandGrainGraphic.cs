using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board.Sand
{
    /// <summary>
    /// Draws a <see cref="SandPour"/> at a moment in time: one quad per grain still in the cell, at its exact (sub-pixel)
    /// position, in its own colour. Resting grains are squares that tile into a solid surface; falling grains become
    /// round and stretch along their fall. A maskable uGUI graphic, so the cell's mask clips it.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SandGrainGraphic : MaskableGraphic
    {
        private static readonly Vector4 Tangent = new Vector4(1f, 0f, 0f, -1f);

        [Tooltip("How far a grain falls, in grains, while it turns from a square piece of the surface into a round grain.")]
        [Min(0.01f)]
        [SerializeField] private float roundingDistance = 2f;

        [Tooltip("How much bigger than its grid square a falling grain is drawn, so a loose stream still looks full.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float roundGrowth = 0.1f;

        private SandPour pour;
        private Color32[] colours;
        private float time;

        /// <param name="grainColours">Colour per grid cell, indexed by <see cref="SandPour.RestCell"/>.</param>
        public void Show(SandPour sandPour, Color32[] grainColours)
        {
            pour = sandPour;
            colours = grainColours;
            ShowTime(0f);
        }

        public void ShowTime(float seconds)
        {
            time = seconds;
            SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // The roundness of each grain travels in the second UV channel.
            if (canvas != null)
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        }

        protected override void OnPopulateMesh(VertexHelper vertices)
        {
            vertices.Clear();
            if (pour == null)
                return;

            var rect = rectTransform.rect;
            var grainWidth = rect.width / pour.Columns;
            var grainHeight = rect.height / pour.Rows;
            for (var grain = 0; grain < pour.Count; grain++)
            {
                pour.Locate(grain, time, out var x, out var y, out _, out var speed);
                if (y + 0.5f < 0f)
                    continue;

                var fallen = pour.FallenDistance(grain, y);
                var roundness = Mathf.Clamp01(fallen / roundingDistance);
                // Grows within its first quarter grain of fall, so neighbours that just let go overlap instead of
                // opening hairline gaps; a resting grain keeps its exact square.
                var half = 0.5f + roundGrowth * Mathf.Clamp01(fallen * 4f);
                var streak = pour.StreakLength(speed);
                var min = new Vector2(rect.xMin + (x - half) * grainWidth, rect.yMin + (y - half) * grainHeight);
                var max = new Vector2(rect.xMin + (x + half) * grainWidth, rect.yMin + (y + half + streak) * grainHeight);
                AddQuad(vertices, min, max, colours[pour.RestCell(grain)], roundness);
            }
        }

        /// <summary>A grain's quad: uv0 runs from -1 to 1 across it, uv1.x carries its roundness to the shader.</summary>
        private static void AddQuad(VertexHelper vertices, Vector2 min, Vector2 max, Color32 colour, float roundness)
        {
            var start = vertices.currentVertCount;
            var round = new Vector4(roundness, 0f, 0f, 0f);
            vertices.AddVert(new Vector3(min.x, min.y), colour, new Vector4(-1f, -1f), round, Vector3.back, Tangent);
            vertices.AddVert(new Vector3(min.x, max.y), colour, new Vector4(-1f, 1f), round, Vector3.back, Tangent);
            vertices.AddVert(new Vector3(max.x, max.y), colour, new Vector4(1f, 1f), round, Vector3.back, Tangent);
            vertices.AddVert(new Vector3(max.x, min.y), colour, new Vector4(1f, -1f), round, Vector3.back, Tangent);
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start + 2, start + 3, start);
        }
    }
}
