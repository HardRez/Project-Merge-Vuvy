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
    /// cell tile itself and can never be mistaken for an item. When the cell opens, the tile becomes a grid of grains in
    /// the tile's own colours that let go and pour out of the cell (<see cref="SandPour"/>), drawn by
    /// <see cref="SandGrainGraphic"/>. While the sand moves (a refusal shake, the pour) the mask on this object clips it
    /// to the tile's shape; at rest it is off, so the tile keeps its soft, antialiased edge.
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(Mask))]
    public sealed class CellCover : MonoBehaviour
    {
        [Tooltip("The resting sand tile; also the source of the grains' colours.")]
        [SerializeField] private Image sand;

        [Tooltip("The mask's graphic: the sand tile's shape, cut where the tile is at least half opaque.")]
        [SerializeField] private Image shape;

        [SerializeField] private SandGrainGraphic grains;

        [Tooltip("Looks picked per cell, so neighbouring covers do not look stamped.")]
        [SerializeField] private Sprite[] variants = Array.Empty<Sprite>();

        [Header("Pour")]
        [Tooltip("Grains across the cell; about 4–4.5 screen pixels per grain on a phone, so single grains read.")]
        [Min(8)]
        [SerializeField] private int columns = 42;

        [SerializeField] private SandPourSettings pour = new SandPourSettings();

        [Header("Refuse")]
        [SerializeField] private float refuseSeconds = 0.35f;

        private Mask mask;
        private TweenChannel motion;
        private Action poured;
        private int seed;
        private SandPour sandPour;
        private float pourTime;
        private Vector2 sampledSize;

        private TweenChannel Motion => motion ??= new TweenChannel(this);

        public bool IsPouring => poured != null && sandPour != null;

        /// <summary>Grains still in the cell; 0 before the pour has started.</summary>
        public int GrainCount => sandPour?.LiveCount(pourTime) ?? 0;

        /// <summary>Seconds the pour takes from letting go to an empty cell.</summary>
        public float PourSeconds => pour.TotalSeconds;

        /// <param name="variantSeed">Any stable number for the cell, e.g. derived from its coordinates; also seeds the pour.</param>
        public void Show(int variantSeed)
        {
            seed = variantSeed;
            if (variants.Length > 0)
                sand.sprite = shape.sprite = variants[Mathf.Abs(variantSeed) % variants.Length];
            Motion.Stop();
            sand.rectTransform.anchoredPosition = Vector2.zero;
            grains.gameObject.SetActive(false);
            SetClipped(false);
        }

        /// <summary>A short shake: the sand does not move.</summary>
        public void PlayRefuse()
        {
            var tile = sand.rectTransform;
            SetClipped(true);
            Motion.Play(Tween.Run(refuseSeconds, Ease.Linear, t =>
            {
                tile.anchoredPosition = new Vector2(Ease.Shake(t, 5) * 8f, 0f);
                if (t >= 1f)
                    SetClipped(false);
            }));
        }

        /// <summary>
        /// After <paramref name="delay"/> the sand pours out of the cell; once it is empty, or when <see cref="Finish"/>
        /// cuts it short, <paramref name="onPoured"/> is called once.
        /// </summary>
        public void Pour(float delay, Action onPoured)
        {
            poured = onPoured;
            // Until the grains let go, the cover looks exactly like the resting tile, even if it was mid-shake.
            Motion.Stop();
            sand.rectTransform.anchoredPosition = Vector2.zero;
            SetClipped(false);
            if (!Motion.Play(PlayPour(delay)))
                Finish();
        }

        /// <summary>Shows the pour as it is <paramref name="seconds"/> after the sand let go, and holds it there.</summary>
        public void ShowPourAt(float seconds)
        {
            Motion.Stop();
            BeginPour();
            ShowTime(seconds);
        }

        /// <summary>Ends the pour at once: the sand is gone.</summary>
        public void Finish()
        {
            Motion.Stop();
            var onPoured = poured;
            poured = null;
            onPoured?.Invoke();
        }

        private IEnumerator PlayPour(float delay)
        {
            yield return Tween.Delay(delay);

            BeginPour();
            for (var elapsed = 0f; elapsed < sandPour.Duration; elapsed += Time.unscaledDeltaTime)
            {
                ShowTime(elapsed);
                yield return null;
            }

            Finish();
        }

        /// <summary>Swaps the tile for its grains: same colours at the same spots, so the swap cannot be seen.</summary>
        private void BeginPour()
        {
            // The grains are sampled for the cell's drawn size: the tile's rounded corners keep their size when the cell
            // is resized, so a cell resized before any grain has moved (the screen changed) is sampled again.
            var size = sand.rectTransform.rect.size;
            if (sandPour != null && (size == sampledSize || pourTime > 0f))
                return;

            sampledSize = size;
            var rows = Mathf.Max(8, Mathf.RoundToInt(columns * size.y / Mathf.Max(1f, size.x)));
            var colours = new Color32[columns * rows];
            var occupied = new bool[columns * rows];
            SandGrainPalette.Sample(sand, columns, rows, colours, occupied);
            sandPour = new SandPour(columns, rows, occupied, seed, pour);

            grains.Show(sandPour, colours);
            grains.gameObject.SetActive(true);
            sand.enabled = false;
            SetClipped(true);
        }

        private void SetClipped(bool clipped)
        {
            if (mask == null)
                mask = GetComponent<Mask>();
            mask.enabled = clipped;
            shape.enabled = clipped;
        }

        private void ShowTime(float seconds)
        {
            pourTime = seconds;
            grains.ShowTime(seconds);
        }
    }
}
