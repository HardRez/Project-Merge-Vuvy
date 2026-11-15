using MergeGame.View.Common;
using UnityEngine;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// The little shop both screens are set in: a papered wall, and a wooden counter the customers stand behind. The
    /// floor is the screens' tiled background (<see cref="SceneScaffold.SafeContent"/>).
    /// </summary>
    internal static class ShopScenery
    {
        /// <summary>
        /// The shop counter across the bottom of <paramref name="strip"/>, from one edge of the screen to the other: a
        /// teal cloth hanging down its front under a wooden plank whose top surface is <see cref="LayoutMetrics.CounterTop"/>
        /// above the strip's bottom. The cloth's hem hangs a little past the strip. Whatever is added to the strip after
        /// the counter is drawn in front of it.
        /// </summary>
        /// <returns>The plank.</returns>
        public static RectTransform Counter(UiKit ui, RectTransform strip)
        {
            const float hemOverhang = LayoutMetrics.CounterClothHeight - LayoutMetrics.CounterClothTop;
            var counter = UiKit.Rect("Counter", strip).Band(fromTop: false, LayoutMetrics.CounterTop + hemOverhang, offset: -hemOverhang);
            counter.gameObject.AddComponent<CanvasWidthFitter>();

            var cloth = ui.Image("Cloth", counter, ui.Sprites.CounterCloth, Color.white);
            cloth.preserveAspect = false;
            cloth.rectTransform.Band(fromTop: false, LayoutMetrics.CounterClothHeight);

            var plank = ui.Image("Plank", counter, ui.Sprites.CounterTop, Color.white);
            plank.preserveAspect = false;
            plank.rectTransform.Band(fromTop: true, LayoutMetrics.CounterPlankHeight);
            return plank.rectTransform;
        }

        /// <summary>
        /// The shop's wallpaper behind the customers: across the whole screen, from its top down to the middle of the
        /// counter's plank, wherever the layout puts the counter. It lies over the floor and under all content.
        /// </summary>
        public static void Wall(UiKit ui, RectTransform canvas, RectTransform counterPlank)
        {
            var wall = ui.Tiled("Wall", canvas, ui.Sprites.ShopWall, LayoutMetrics.ShopWallTileWidth);
            wall.rectTransform.Stretch();
            wall.rectTransform.SetSiblingIndex(1);
            wall.gameObject.AddComponent<BottomEdgeFollower>()
                .Wire("target", counterPlank)
                .Wire("heightInTarget", 0.5f);
        }
    }
}
