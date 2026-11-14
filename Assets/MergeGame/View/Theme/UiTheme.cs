using MergeGame.Presentation.Board;
using UnityEngine;

namespace MergeGame.View.Theme
{
    /// <summary>
    /// The colour palette of the whole UI. The scene builder tints the generated screens with it and views read the
    /// colours they need at runtime, so a palette change is one asset edit plus a rebuild.
    /// </summary>
    [CreateAssetMenu(menuName = "Merge Game/UI Theme", fileName = "UiTheme")]
    public sealed class UiTheme : ScriptableObject
    {
        [Header("Surfaces")]
        [SerializeField] private Color panel = Hex(0xFFFFFF);
        [SerializeField] private Color woodLight = Hex(0xC98B52);
        [SerializeField] private Color woodDark = Hex(0x8B5530);
        [SerializeField] private Color cellLight = Hex(0xFFF4DA);
        [SerializeField] private Color cellDark = Hex(0xF7E4BD);
        [SerializeField] private Color shadow = new Color(0.16f, 0.09f, 0.03f, 0.28f);
        [Tooltip("Dust as the last sand leaves an opened cell.")]
        [SerializeField] private Color sand = Hex(0xD9AE66);

        [Tooltip("Loose grains trickling out of an opened cell.")]
        [SerializeField] private Color sandGrain = Hex(0xCFA25E);
        [SerializeField] private Color toastBackground = new Color(0.24f, 0.14f, 0.07f, 0.9f);

        [Header("Accents")]
        [SerializeField] private Color primary = Hex(0x5CC94B);
        [SerializeField] private Color secondary = Hex(0xFFA62B);
        [SerializeField] private Color danger = Hex(0xF0596D);
        [SerializeField] private Color gold = Hex(0xFFC83D);
        [SerializeField] private Color disabled = Hex(0xBDB5AA);

        [Header("Text")]
        [SerializeField] private Color textDark = Hex(0x5B3A1E);
        [SerializeField] private Color textMuted = Hex(0x9C7B5B);
        [SerializeField] private Color textLight = Hex(0xFFFFFF);
        [SerializeField] private Color textOutline = Hex(0x4A2A12);

        [Header("Drop highlights")]
        [SerializeField] private Color moveHighlight = new Color(0.55f, 0.83f, 1f, 0.75f);
        [SerializeField] private Color mergeHighlight = Hex(0x7BEF52);
        [SerializeField] private Color invalidHighlight = new Color(1f, 0.38f, 0.38f, 0.8f);
        [SerializeField] private Color maxLevelHighlight = Hex(0xFFC83D);
        [SerializeField] private Color selection = Hex(0xFFD23F);

        [Header("Order match")]
        [Tooltip("The halo behind an item a customer is waiting for; the check badge carries the main signal.")]
        [SerializeField] private Color orderMatch = Hex(0x34C759);

        public Color Panel => panel;
        public Color WoodLight => woodLight;
        public Color WoodDark => woodDark;
        public Color CellLight => cellLight;
        public Color CellDark => cellDark;
        public Color Shadow => shadow;
        public Color Sand => sand;
        public Color SandGrain => sandGrain;
        public Color ToastBackground => toastBackground;

        public Color Primary => primary;
        public Color Secondary => secondary;
        public Color Danger => danger;
        public Color Gold => gold;
        public Color Disabled => disabled;

        public Color TextDark => textDark;
        public Color TextMuted => textMuted;
        public Color TextLight => textLight;
        public Color TextOutline => textOutline;

        public Color Selection => selection;
        public Color OrderMatch => orderMatch;

        public Color GetDropHighlightColor(DropHighlight highlight)
        {
            switch (highlight)
            {
                case DropHighlight.MoveToEmptyCell:
                    return moveHighlight;
                case DropHighlight.Merge:
                    return mergeHighlight;
                case DropHighlight.MaxLevel:
                    return maxLevelHighlight;
                default:
                    return invalidHighlight;
            }
        }

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
    }
}
