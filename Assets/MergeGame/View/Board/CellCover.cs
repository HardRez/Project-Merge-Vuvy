using System;
using System.Collections;
using MergeGame.View.Animation;
using MergeGame.View.Board.Sand;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// Sand burying a closed cell. It belongs to the board rather than to an item, so it fills the whole cell like the
    /// cell tile itself and can never be mistaken for an item. When the cell opens, the tile turns into a
    /// <see cref="SandSimulation"/> of individual grains that pour out through the cell's floor, drawn by the sand grain
    /// shader and clipped to the cell's rounded shape by the mask on this object.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Mask))]
    public sealed class CellCover : MonoBehaviour
    {
        [Tooltip("The resting sand tile; also the source of the grains' colours.")]
        [SerializeField] private Image sand;

        [Tooltip("Shows the pouring grains through the sand grain material.")]
        [SerializeField] private RawImage grains;

        [Tooltip("Looks picked per cell, so neighbouring covers do not look stamped.")]
        [SerializeField] private Sprite[] variants = Array.Empty<Sprite>();

        [Header("Pour")]
        [Tooltip("Grains across the cell; about 3 screen pixels per grain on a phone.")]
        [Min(8)]
        [SerializeField] private int columns = 64;

        [Tooltip("Simulation steps per second; with the pour settings this sets how long the sand takes to run out.")]
        [Min(1f)]
        [SerializeField] private float stepsPerSecond = 75f;

        [Tooltip("Most simulation steps run in one frame, so a long frame slows the pour instead of jumping it.")]
        [Min(1)]
        [SerializeField] private int maxStepsPerFrame = 4;

        [SerializeField] private SandPourSettings pour = new SandPourSettings();

        [Header("Refuse")]
        [SerializeField] private float refuseSeconds = 0.35f;

        private TweenChannel motion;
        private Action poured;
        private int seed;
        private SandSimulation simulation;
        private Color32[] grainColours;
        private Color32[] statePixels;
        private Texture2D state;

        private TweenChannel Motion => motion ??= new TweenChannel(this);

        public bool IsPouring => poured != null && simulation != null;

        /// <summary>Grains still in the cell; 0 before the pour has started.</summary>
        public int GrainCount => simulation?.GrainCount ?? 0;

        /// <summary>Pour steps done so far.</summary>
        public int PourStep => simulation?.StepCount ?? 0;

        public float StepsPerSecond => stepsPerSecond;

        /// <param name="variantSeed">Any stable number for the cell, e.g. derived from its coordinates; also seeds the pour.</param>
        public void Show(int variantSeed)
        {
            seed = variantSeed;
            if (variants.Length > 0)
                sand.sprite = variants[Mathf.Abs(variantSeed) % variants.Length];
            Motion.Stop();
            sand.rectTransform.anchoredPosition = Vector2.zero;
            grains.gameObject.SetActive(false);
        }

        /// <summary>A short shake: the sand does not move.</summary>
        public void PlayRefuse()
        {
            var tile = sand.rectTransform;
            Motion.Play(Tween.Run(refuseSeconds, Ease.Linear, t => tile.anchoredPosition = new Vector2(Ease.Shake(t, 5) * 8f, 0f)));
        }

        /// <summary>
        /// After <paramref name="delay"/> the sand pours out of the cell; once it is empty, or when <see cref="Finish"/>
        /// cuts it short, <paramref name="onPoured"/> is called once.
        /// </summary>
        public void Pour(float delay, Action onPoured)
        {
            poured = onPoured;
            if (!Motion.Play(PlayPour(delay)))
                Finish();
        }

        /// <summary>Shows the pour exactly as it is after <paramref name="step"/> simulation steps and holds it there.</summary>
        public void ShowPourStep(int step)
        {
            Motion.Stop();
            if (simulation != null && simulation.StepCount > step)
                simulation = null;
            BeginPour();
            while (simulation.StepCount < step && !simulation.IsEmpty)
                simulation.Step();
            Upload();
        }

        /// <summary>Ends the pour at once: the sand is gone.</summary>
        public void Finish()
        {
            Motion.Stop();
            var onPoured = poured;
            poured = null;
            onPoured?.Invoke();
        }

        private void OnDestroy()
        {
            if (state != null)
                Destroy(state);
        }

        private IEnumerator PlayPour(float delay)
        {
            yield return Tween.Delay(delay);

            BeginPour();
            var pendingSteps = 0f;
            while (!simulation.IsEmpty)
            {
                yield return null;
                pendingSteps += Time.unscaledDeltaTime * stepsPerSecond;
                var steps = Mathf.Min((int)pendingSteps, maxStepsPerFrame);
                pendingSteps = Mathf.Min(pendingSteps - steps, 1f);
                for (var i = 0; i < steps && !simulation.IsEmpty; i++)
                    simulation.Step();
                if (steps > 0)
                    Upload();
            }

            Finish();
        }

        /// <summary>Swaps the tile for its grains: same colours at the same spots, so the swap cannot be seen.</summary>
        private void BeginPour()
        {
            if (simulation != null)
                return;

            var size = sand.rectTransform.rect.size;
            var rows = Mathf.Max(8, Mathf.RoundToInt(columns * size.y / Mathf.Max(1f, size.x)));
            var count = columns * rows;
            if (grainColours == null || grainColours.Length != count)
            {
                grainColours = new Color32[count];
                statePixels = new Color32[count];
                if (state != null)
                    Destroy(state);
                state = new Texture2D(columns, rows, TextureFormat.RGBA32, mipChain: false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            var occupied = new bool[count];
            SandGrainPalette.Sample(sand, columns, rows, grainColours, occupied);
            simulation = new SandSimulation(columns, rows, occupied, seed, pour);

            grains.texture = state;
            Upload();
            grains.gameObject.SetActive(true);
            sand.enabled = false;
        }

        /// <summary>One texel per grain: its colour, and in alpha whether it is there and how fast it falls.</summary>
        private void Upload()
        {
            for (var y = 0; y < simulation.Height; y++)
            {
                for (var x = 0; x < simulation.Width; x++)
                {
                    var index = y * simulation.Width + x;
                    var grain = simulation.GrainAt(x, y);
                    if (grain == SandSimulation.Empty)
                    {
                        statePixels[index] = default;
                        continue;
                    }

                    var colour = grainColours[grain - 1];
                    colour.a = (byte)(128 + 127 * Mathf.Clamp01(simulation.SpeedAt(x, y)));
                    statePixels[index] = colour;
                }
            }

            state.SetPixels32(statePixels);
            state.Apply(updateMipmaps: false);
        }
    }
}
