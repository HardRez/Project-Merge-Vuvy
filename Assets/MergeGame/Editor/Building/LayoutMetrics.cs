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
        public const float OrdersHeight = 300f;
        public const float InfoBarHeight = 176f;
        public const float SpawnBarHeight = 168f;

        // Weights of the extra height on screens taller than 9:16 (only their ratios matter).
        public const float OrdersGrowth = 0.15f;
        public const float InfoBarGrowth = 0.1f;
        public const float SpaceAboveGroupShare = 0.4f;
        public const float SpaceAboveSpawnShare = 0.35f;

        /// <summary>The board shrinks down to this height before anything else gives way.</summary>
        public const float BoardMinHeight = 420f;

        public const float BoardFramePadding = 26f;
        public const float ToastHeight = 104f;

        public const float CardSpacing = 20f;
        public const float AvatarSize = 132f;

        /// <summary>How far above the customer's round frame its head may rise, in frame heights.</summary>
        public const float PortraitHeadroomAboveFrame = 0.32f;
        public const float RequirementSize = 100f;
    }
}
