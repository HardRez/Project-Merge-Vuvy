using System.Collections;
using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// The bubble around a locked item bursting: its skin tears apart (a flipbook from a sprite sheet) while a few small
    /// bubbles spray outwards and fade. Starts hidden (the prefab saves it inactive), plays once and hides itself;
    /// <see cref="Stop"/> hides it at once.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BubblePop : MonoBehaviour
    {
        [Tooltip("The bubble itself, blown outwards and faded in the first moment of the burst.")]
        [SerializeField] private Image shell;

        [Tooltip("A short white flash at the instant of the burst.")]
        [SerializeField] private Image flash;

        [Tooltip("Shows one cell of the sheet at a time.")]
        [SerializeField] private RawImage skin;

        [Tooltip("Columns and rows of the sheet; frames run left to right, top to bottom.")]
        [SerializeField] private Vector2Int sheetGrid = new Vector2Int(4, 4);

        [Tooltip("The small bubbles thrown outwards, spread evenly around the centre.")]
        [SerializeField] private Image[] droplets;

        [SerializeField] private float seconds = 0.5f;

        [Tooltip("How far the droplets travel, as a share of this rect's width.")]
        [SerializeField] private float sprayReach = 0.5f;

        [Tooltip("How far the shell blows out before it is gone, as a multiple of its size.")]
        [SerializeField] private float shellBlowScale = 1.75f;

        [Tooltip("Share of the burst in which the shell blows out and fades.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float shellShare = 0.32f;

        [SerializeField] private Color skinColor = new Color(0.9f, 0.97f, 1f, 1f);

        private TweenChannel motion;

        public bool IsPlaying { get; private set; }

        private TweenChannel Motion => motion ??= new TweenChannel(this);

        public void Play()
        {
            gameObject.SetActive(true);
            IsPlaying = true;
            if (!Motion.Play(Burst()))
                Hide();
        }

        public void Stop()
        {
            Motion.Stop();
            Hide();
        }

        private void Hide()
        {
            IsPlaying = false;
            gameObject.SetActive(false);
        }

        private IEnumerator Burst()
        {
            var frames = sheetGrid.x * sheetGrid.y;
            var width = ((RectTransform)transform).rect.width;
            yield return Tween.Run(seconds, Ease.Linear, t =>
            {
                var blow = Mathf.Clamp01(t / shellShare);
                shell.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.1f, shellBlowScale, Ease.OutCubic(blow));
                var shellColor = shell.color;
                shellColor.a = 1f - Ease.OutQuad(blow);
                shell.color = shellColor;
                skin.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.6f, Ease.OutCubic(t));

                // A bright snap in the first instants, gone before the spray is.
                var snap = Mathf.Clamp01(t / 0.35f);
                flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.5f, Ease.OutCubic(snap));
                var flashColor = flash.color;
                flashColor.a = 0.9f * Ease.Bump(Mathf.Min(1f, snap * 1.4f));
                flash.color = flashColor;

                // The skin tears through its frames in the first two thirds and fades as it goes.
                var frame = Mathf.Min(frames - 1, Mathf.FloorToInt(Mathf.Clamp01(t * 1.5f) * frames));
                skin.uvRect = new Rect(
                    (frame % sheetGrid.x) / (float)sheetGrid.x,
                    1f - (frame / sheetGrid.x + 1) / (float)sheetGrid.y,
                    1f / sheetGrid.x,
                    1f / sheetGrid.y);
                var skinColorNow = skinColor;
                skinColorNow.a = 1f - Ease.InQuad(Mathf.Clamp01(t * 1.5f));
                skin.color = skinColorNow;

                for (var i = 0; i < droplets.Length; i++)
                {
                    // An uneven fan: each droplet has its own angle offset, distance and start, so the spray never
                    // reads as a ring.
                    var angle = (i + 0.35f + 0.3f * Mathf.Sin(i * 2.3f)) / droplets.Length * Mathf.PI * 2f;
                    var reach = width * sprayReach * (0.75f + 0.25f * ((i * 7) % 4) / 3f);
                    var start = 0.06f * (i % 3);
                    var travelled = Ease.OutCubic(Mathf.Clamp01((t - start) / (1f - start)));
                    var droplet = droplets[i];
                    droplet.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (reach * travelled);
                    droplet.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, t);
                    var color = droplet.color;
                    color.a = 1f - Ease.InQuad(t);
                    droplet.color = color;
                }
            });

            Hide();
        }
    }
}
