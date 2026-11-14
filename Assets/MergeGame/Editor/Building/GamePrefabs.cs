using System;
using System.IO;
using MergeGame.View.Board;
using MergeGame.View.Orders;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MergeGame.Editor.Building
{
    /// <summary>The prefabs the game scene instantiates at runtime: board cells, pooled items and popups, order cards.</summary>
    internal sealed class GamePrefabs
    {
        private const float CellCornerScale = 0.7f;

        // Where the item icon sits inside its cell (normalised).
        private static readonly Vector2 IconMin = new Vector2(0.08f, 0.08f);
        private static readonly Vector2 IconMax = new Vector2(0.92f, 0.92f);

        private GamePrefabs()
        {
        }

        public Image BoardCell { get; private set; }
        public CellCover CellCover { get; private set; }
        public ItemView ItemView { get; private set; }
        public PointsPopup PointsPopup { get; private set; }
        public OrderCardView OrderCard { get; private set; }

        /// <summary>Writes every prefab; builds them in the open scene, which should be a scratch scene.</summary>
        public static void Generate(UiKit ui)
        {
            BuildBoardCell(ui);
            BuildCellCover(ui);
            BuildItemView(ui);
            BuildPointsPopup(ui);
            BuildOrderCard(ui, BuildRequirementSlot(ui));
        }

        public static GamePrefabs Load() => new GamePrefabs
        {
            BoardCell = LoadPrefab<Image>(BuildPaths.BoardCellPrefab),
            CellCover = LoadPrefab<CellCover>(BuildPaths.CellCoverPrefab),
            ItemView = LoadPrefab<ItemView>(BuildPaths.ItemViewPrefab),
            PointsPopup = LoadPrefab<PointsPopup>(BuildPaths.PointsPopupPrefab),
            OrderCard = LoadPrefab<OrderCardView>(BuildPaths.OrderCardPrefab)
        };

        private static T LoadPrefab<T>(string path) where T : Component =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException($"{path} is missing; generate the prefabs first.");

        private static Image BuildBoardCell(UiKit ui)
        {
            var cell = ui.Image("BoardCell", null, ui.Sprites.Cell, Color.white, cornerScale: CellCornerScale);
            return Save<Image>(cell.rectTransform, BuildPaths.BoardCellPrefab);
        }

        /// <summary>
        /// Sand over a whole closed cell. The sand sprites are drawn at twice the cell sprite's resolution, so half the
        /// cell's corner scale gives the same corner radius.
        /// </summary>
        private static CellCover BuildCellCover(UiKit ui)
        {
            var root = UiKit.Rect("CellCover", null);
            var sand = ui.Image("Sand", root, ui.Sprites.Sand[0], Color.white, cornerScale: CellCornerScale / 2f);
            sand.rectTransform.Stretch();

            var puffs = new Image[6];
            for (var i = 0; i < puffs.Length; i++)
            {
                puffs[i] = ui.Image($"Puff {i + 1}", root, ui.Sprites.Glow, ui.Theme.Sand);
                puffs[i].rectTransform.Span(new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f));
                puffs[i].gameObject.SetActive(false);
            }

            root.gameObject.AddComponent<CellCover>()
                .Wire("sand", sand)
                .Wire("variants", ui.Sprites.Sand)
                .Wire("puffs", puffs);
            return Save<CellCover>(root, BuildPaths.CellCoverPrefab);
        }

        private static ItemView BuildItemView(UiKit ui)
        {
            var root = UiKit.Rect("ItemView", null);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            var shadow = ui.Image("LiftShadow", root, ui.Sprites.Shadow, Color.clear, cornerScale: 0.6f);
            shadow.rectTransform.Stretch(4f, -30f, 4f, 18f);

            var accentLayer = UiKit.Rect("Accent", root).Stretch();
            var accentGlow = ui.Image("AccentGlow", accentLayer, ui.Sprites.Glow, Color.white);
            accentGlow.rectTransform.Inset(-84f);

            var body = UiKit.Rect("Body", accentLayer).Stretch();
            var glow = ui.Image("BurstGlow", body, ui.Sprites.Glow, WithAlpha(ui.Theme.Gold, 0f));
            glow.rectTransform.Inset(-46f);
            var orderHalo = ui.Image("OrderMatchHalo", body, ui.Sprites.Glow, ui.Theme.OrderMatch);
            orderHalo.rectTransform.Inset(-18f);
            orderHalo.gameObject.SetActive(false);
            var icon = ui.Image("Icon", body, null, Color.white);
            icon.rectTransform.Span(IconMin, IconMax);
            var lockOverlay = ui.Icon("LockOverlay", body, "cobweb");
            lockOverlay.rectTransform.Span(new Vector2(0.01f, 0.01f), new Vector2(0.99f, 0.99f));
            var crown = ui.Icon("MaxLevelBadge", body, "crown");
            crown.rectTransform.Span(new Vector2(0.6f, 0.62f), new Vector2(1.04f, 1.06f));
            crown.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            // Bottom-left, so it never collides with the crown in the top-right corner.
            var orderBadge = ui.Icon("OrderMatchBadge", body, "check");
            orderBadge.rectTransform.Span(new Vector2(-0.04f, -0.04f), new Vector2(0.36f, 0.36f));
            orderBadge.gameObject.SetActive(false);
            var sparkles = ui.Icon("BurstSparkles", body, "sparkles");
            sparkles.rectTransform.Inset(-30f);
            sparkles.gameObject.SetActive(false);

            var accent = BuildAccent(ui, accentLayer, accentGlow);
            var orderMark = root.gameObject.AddComponent<OrderMatchMark>()
                .Wire("halo", orderHalo)
                .Wire("badge", orderBadge);
            root.gameObject.AddComponent<ItemView>()
                .Wire("orderMark", orderMark)
                .Wire("body", body)
                .Wire("accent", accent)
                .Wire("icon", icon)
                .Wire("lockOverlay", lockOverlay)
                .Wire("maxLevelBadge", crown.gameObject)
                .Wire("liftShadow", shadow)
                .Wire("burstGlow", glow)
                .Wire("burstSparkles", sparkles);
            return Save<ItemView>(root, BuildPaths.ItemViewPrefab);
        }

        /// <summary>The shine sweeps inside a copy of the icon's shape, so it lights up the item and not its tile.</summary>
        private static ItemAccent BuildAccent(UiKit ui, RectTransform layer, Image glow)
        {
            var shineMask = ui.Image("ShineMask", layer, null, Color.white);
            shineMask.rectTransform.Span(IconMin, IconMax);
            shineMask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var shine = ui.Image("Shine", shineMask.rectTransform, ui.Sprites.Glow, Color.white);
            shine.preserveAspect = false;
            shine.rectTransform.Span(new Vector2(0.3f, -0.3f), new Vector2(0.7f, 1.3f));
            shine.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            var hintRing = ui.Image("HintRing", layer, ui.Sprites.Ring, ui.Theme.Gold, cornerScale: 0.6f);
            hintRing.rectTransform.Inset(2f);
            var sparkles = ui.Icon("AccentSparkles", layer, "sparkles");
            sparkles.rectTransform.Span(new Vector2(-0.16f, 0.56f), new Vector2(0.44f, 1.16f));

            return layer.gameObject.AddComponent<ItemAccent>()
                .Wire("glow", glow)
                .Wire("hintRing", hintRing)
                .Wire("shineMask", shineMask)
                .Wire("shine", shine.rectTransform)
                .Wire("sparkles", sparkles)
                .Wire("targetGlowColor", ui.Theme.Gold);
        }

        private static PointsPopup BuildPointsPopup(UiKit ui)
        {
            var root = UiKit.Rect("PointsPopup", null).Pin(RectLayout.Center, new Vector2(320f, 130f));
            root.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            var label = ui.Text("Label", root, "+10", 76f, TextTone.Light);
            label.rectTransform.Stretch();
            label.color = ui.Theme.Gold;

            root.gameObject.AddComponent<PointsPopup>().Wire("label", label);
            return Save<PointsPopup>(root, BuildPaths.PointsPopupPrefab);
        }

        private static RequirementSlotView BuildRequirementSlot(UiKit ui)
        {
            var size = LayoutMetrics.RequirementSize;
            var root = UiKit.Rect("RequirementSlot", null).Pin(RectLayout.Center, new Vector2(size, size));
            root.Prefer(size, size);
            var icon = ui.Image("Icon", root, null, Color.white);
            icon.rectTransform.Inset(2f);
            var check = ui.Icon("Check", root, "check");
            check.rectTransform.Pin(RectLayout.BottomRight, new Vector2(48f, 48f), new Vector2(8f, -6f));
            check.gameObject.SetActive(false);

            root.gameObject.AddComponent<RequirementSlotView>()
                .Wire("icon", icon)
                .Wire("checkMark", check.rectTransform);
            return Save<RequirementSlotView>(root, BuildPaths.RequirementSlotPrefab);
        }

        private static OrderCardView BuildOrderCard(UiKit ui, RequirementSlotView requirementSlot)
        {
            var root = UiKit.Rect("OrderCard", null).Pin(RectLayout.Center, new Vector2(500f, LayoutMetrics.OrdersHeight));
            root.Prefer(flexibleWidth: 1f);

            var body = UiKit.Rect("Body", root).Stretch();
            ui.DropShadow(body);
            ui.Image("Panel", body, ui.Sprites.Panel, ui.Theme.Panel).rectTransform.Stretch();

            // A golden flash across the card, under its content.
            var glow = ui.Image("CelebrationGlow", body, ui.Sprites.Glow, WithAlpha(ui.Theme.Gold, 0f));
            glow.rectTransform.Inset(-20f);
            glow.preserveAspect = false;

            var content = UiKit.Rect("Content", body).Stretch();
            var contentGroup = content.gameObject.AddComponent<CanvasGroup>();

            var avatar = BuildAvatar(ui, content);
            var requirements = BuildSpeechBubble(ui, content);
            var reward = BuildRewardPill(ui, content);
            var deliver = BuildDeliverButton(ui, content, out var deliverPulse);

            var celebration = ui.Text("CelebrationLabel", body, "+0", 96f, TextTone.Title);
            celebration.rectTransform.Pin(RectLayout.Center, new Vector2(400f, 140f), new Vector2(0f, -40f));
            celebration.color = ui.Theme.Gold;

            root.gameObject.AddComponent<OrderCardView>()
                .Wire("body", body)
                .Wire("content", contentGroup)
                .Wire("avatar", avatar)
                .Wire("requirementContainer", requirements)
                .Wire("requirementPrefab", requirementSlot)
                .Wire("rewardLabel", reward)
                .Wire("deliverButton", deliver.Button)
                .Wire("deliverPulse", deliverPulse)
                .Wire("celebrationGlow", glow)
                .Wire("celebrationLabel", celebration);
            return Save<OrderCardView>(root, BuildPaths.OrderCardPrefab);
        }

        private static Image BuildAvatar(UiKit ui, RectTransform card)
        {
            var size = LayoutMetrics.AvatarSize;
            var disc = ui.Image("AvatarDisc", card, ui.Sprites.Circle, ui.Theme.CellDark);
            disc.rectTransform.Pin(RectLayout.TopLeft, new Vector2(size, size), new Vector2(20f, -22f));
            var avatar = ui.Image("Avatar", disc.rectTransform, null, Color.white);
            avatar.rectTransform.Inset(8f);
            return avatar;
        }

        /// <returns>The row the requirement slots are laid out in.</returns>
        private static RectTransform BuildSpeechBubble(UiKit ui, RectTransform card)
        {
            var left = 20f + LayoutMetrics.AvatarSize + 8f;
            var bubble = ui.Image("SpeechBubble", card, ui.Sprites.SpeechBubble, ui.Theme.CellLight, cornerScale: 0.8f);
            bubble.rectTransform.Band(fromTop: true, height: 156f, offset: 14f);
            bubble.rectTransform.offsetMin = new Vector2(left, bubble.rectTransform.offsetMin.y);
            bubble.rectTransform.offsetMax = new Vector2(-16f, bubble.rectTransform.offsetMax.y);

            var row = UiKit.Rect("Requirements", bubble.rectTransform).Stretch(40f, 38f, 18f, 12f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        private static TextMeshProUGUI BuildRewardPill(UiKit ui, RectTransform card)
        {
            const float height = 76f;
            var pill = ui.Image("RewardPill", card, ui.Sprites.Pill, ui.Theme.CellLight, cornerScale: height / 128f);
            pill.rectTransform.Pin(RectLayout.BottomLeft, new Vector2(176f, height), new Vector2(20f, 20f));
            ui.Icon("Star", pill.rectTransform, "star").rectTransform.Pin(RectLayout.Left, new Vector2(70f, 70f), new Vector2(-4f, 2f));
            var label = ui.Text("Reward", pill.rectTransform, "+0", 42f, TextTone.Dark, TextAlignmentOptions.Center);
            label.rectTransform.Stretch(58f, 4f, 10f, 0f);
            return UiKit.ShrinkToFit(label, 28f);
        }

        private static ButtonParts BuildDeliverButton(UiKit ui, RectTransform card, out RectTransform pulse)
        {
            pulse = UiKit.Rect("DeliverPulse", card).Pin(RectLayout.BottomRight, new Vector2(220f, 92f), new Vector2(-18f, 16f));
            var button = ui.Button("DeliverButton", pulse, ui.Theme.Primary, cornerScale: 0.7f);
            button.Rect.Stretch();
            var label = ui.Text("Label", button.Content, "Teslim Et", 40f, TextTone.Light);
            label.rectTransform.Stretch(8f, 0f, 8f, 0f);
            UiKit.ShrinkToFit(label, 26f);
            return button;
        }

        private static T Save<T>(RectTransform root, string path) where T : Component
        {
            AssetFolders.Ensure(Path.GetDirectoryName(path));
            var asset = PrefabUtility.SaveAsPrefabAsset(root.gameObject, path, out var saved);
            Object.DestroyImmediate(root.gameObject);
            if (!saved)
                throw new InvalidOperationException($"Could not save the prefab {path}.");
            return asset.GetComponent<T>();
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
