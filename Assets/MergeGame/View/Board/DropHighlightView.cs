using MergeGame.Presentation.Board;
using MergeGame.View.Common;
using MergeGame.View.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// Marks the hovered cell during a drag with what a drop would do. A move, a refusal or a cell beyond the last level
    /// is a tinted square in its colour; a possible merge is no square at all but the pack's round merge glow
    /// (<see cref="MergeTargetGlow"/>), so the reward to come is what the player sees.
    /// </summary>
    public sealed class DropHighlightView : MonoBehaviour
    {
        [SerializeField] private UiTheme theme;
        [SerializeField] private RectTransform square;
        [SerializeField] private Image fill;
        [SerializeField] private Image outline;
        [SerializeField] private MergeTargetGlow mergeGlow;

        [Range(0f, 1f)]
        [SerializeField] private float fillAlpha = 0.5f;

        private CellRect shownCell;

        public bool IsShown => gameObject.activeSelf;
        public bool ShowsSquare => IsShown && square.gameObject.activeSelf;
        public MergeTargetGlow MergeGlow => mergeGlow;

        public void Show(CellRect cell, DropHighlight highlight)
        {
            gameObject.SetActive(true);
            ((RectTransform)transform).StretchOver(cell.Min, cell.Max);

            var invitesMerge = highlight == DropHighlight.Merge;
            square.gameObject.SetActive(!invitesMerge);
            var color = theme.GetDropHighlightColor(highlight);
            fill.color = new Color(color.r, color.g, color.b, fillAlpha * color.a);
            outline.color = color;

            // A glow moved to another cell starts over, fading in where it is now.
            if (!invitesMerge || !SameCell(cell, shownCell))
                mergeGlow.Hide();
            if (invitesMerge)
                mergeGlow.Show();
            shownCell = cell;
        }

        public void Hide()
        {
            mergeGlow.Hide();
            gameObject.SetActive(false);
        }

        private static bool SameCell(CellRect a, CellRect b) => a.Min == b.Min && a.Max == b.Max;
    }
}
