using UnityEngine;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// Sizes and spacing of the generated screens, in canvas units of the 1080×1920 reference resolution. Taller
    /// screens share their extra height by the growth weights below; wider ones show more background beside a capped
    /// game column.
    /// </summary>
    internal static class LayoutMetrics
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        /// <summary>The widest the game column grows on tablets; beyond it the background shows on both sides.</summary>
        public const float MaxColumnWidth = 1180f;

        public const float ScreenMargin = 28f;
        public const float TopMargin = 18f;
        public const float BottomMargin = 30f;
        public const float SectionSpacing = 16f;

        public const float TopBarHeight = 116f;
        /// <summary>The shop counter with the customers standing behind it.</summary>
        public const float OrdersHeight = 500f;
        public const float InfoBarHeight = 176f;
        /// <summary>The round spawn button with its name tag below it.</summary>
        public const float SpawnBarHeight = 200f;

        // Weights of the extra height on screens taller than 9:16 (only their ratios matter).
        public const float OrdersGrowth = 0.15f;
        public const float InfoBarGrowth = 0.1f;
        public const float SpaceAboveGroupShare = 0.4f;
        public const float SpaceAboveSpawnShare = 0.35f;

        /// <summary>The board shrinks down to this height before anything else gives way.</summary>
        public const float BoardMinHeight = 420f;

        public const float BoardFramePadding = 26f;
        public const float ToastHeight = 104f;

        /// <summary>
        /// The game's name on the main menu's ribbon, in rows of the ribbon art from its top. The ribbon's red band arches:
        /// in the 230 px art its middle runs from row 20 to row 104, and it drops towards the ends like a circle of about
        /// 2250 px radius. The band is too narrow for the whole name, so the art's rows 40 to 100 (inside the band all
        /// along the ribbon) are stretched by <see cref="TitleRibbonExtraRows"/>: the band then runs from row 20 to row
        /// 138 in the middle, while its arch, gold edges and folded ends keep their shape. The name (capitals 0.61 of the
        /// font size above the baseline, the tails of g and y 0.2 below it) stands on row 101 in the middle, so the body
        /// of the letters is centred on the band and the tails of g and y stay on it, and bends along the circle.
        /// </summary>
        public const float TitleNameSize = 90f;
        public const float TitleBaselineRow = 101f;
        public const float TitleArcRadiusRows = 2250f;
        public const float TitleRibbonExtraRows = 34f;

        /// <summary>
        /// The chains popup, from its window's top edge down: the tabs, the chosen chain's name, and its levels in two
        /// rows (a level's cell holds its tile, with the number badge over its corner, and its name under it on up to two
        /// lines; the arrow on to the next level sits in the gap between cells).
        /// </summary>
        public const float ChainsWindowWidth = 980f;
        public const float ChainsWindowHeight = 990f;
        public const float ChainTabsTop = 120f;
        public const float ChainTabSize = 170f;
        public const float ChainTabSpacing = 26f;
        public const float ChainHeadingTop = 324f;
        public const float ChainRowsTop = 384f;
        public static readonly Vector2 ChainTileSize = new Vector2(186f, 183f);
        public const float ChainCellHeight = 270f;
        public const float ChainCellSpacing = 26f;
        public const float ChainRowSpacing = 6f;
        public const float ChainNameHeight = 66f;

        /// <summary>The width one repeat of the shop's wallpaper and of its parquet are drawn at.</summary>
        public const float ShopWallTileWidth = 203f;
        public const float ShopFloorTileWidth = 300f;

        public const float CardSpacing = 20f;
        /// <summary>A requested item on its tray; the item art leaves a margin round the item, so this is drawn generously.</summary>
        public const float RequirementSize = 140f;

        // The shop counter across the bottom of the orders strip, measured up from the strip's bottom edge. The customers
        // stand in the space above the counter top; their trays stand on it and their deliver buttons hang on its front.

        /// <summary>The counter's top surface; customers are framed above it.</summary>
        public const float CounterTop = 196f;

        /// <summary>The wooden plank drawn down from the counter top (its art includes a soft shadow under it).</summary>
        public const float CounterPlankHeight = 100f;

        /// <summary>The cloth on the counter's front hangs from here, behind the plank's front edge.</summary>
        public const float CounterClothTop = 132f;

        /// <summary>The cloth's drawn height, its wavy hem and shadow included; it hangs a little past the strip.</summary>
        public const float CounterClothHeight = 163f;

        /// <summary>
        /// Customers are clipped here, behind the plank's top face (which spans about 126–196), so nothing of them can
        /// show below the counter.
        /// </summary>
        public const float CustomerClipBottom = 150f;

        /// <summary>The tray's bottom edge, on the plank's top surface, and the tray's height.</summary>
        public const float TrayBottom = 150f;
        public const float TrayHeight = 84f;

        /// <summary>The centre of the deliver button, on the counter's cloth.</summary>
        public const float DeliverButtonCentre = 62f;
    }
}
