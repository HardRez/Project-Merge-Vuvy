using System;
using System.IO;
using MergeGame.View.Board;
using MergeGame.View.Board.Sand;
using MergeGame.View.Orders;
using Spine.Unity;
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
        private const string SandGrainShader = "MergeGame/UI/Sand Grains";
        private const string SandMaskShader = "MergeGame/UI/Sand Mask";
        private const string PortraitShader = "Spine/SkeletonGraphic";

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
            BuildCellCover(ui, WriteMaterial(SandGrainShader, BuildPaths.SandGrainMaterial), WriteMaterial(SandMaskShader, BuildPaths.SandMaskMaterial));
            BuildItemView(ui);
            BuildPointsPopup(ui);
            BuildOrderCard(ui, BuildRequirementSlot(ui), WritePortraitMaterial());
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
        /// Sand over a whole closed cell; while it pours, its grains are masked to the sand tile's own shape so they end
        /// exactly where the tile did and never leave the cell. The sand sprites are drawn at twice the cell sprite's
        /// resolution, so half the cell's corner scale gives the same corner radius.
        /// </summary>
        private static CellCover BuildCellCover(UiKit ui, Material grainMaterial, Material maskMaterial)
        {
            var root = ui.Image("CellCover", null, ui.Sprites.Sand[0], Color.white, cornerScale: CellCornerScale / 2f);
            root.material = maskMaterial;
            root.enabled = false;
            var mask = root.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            mask.enabled = false;

            var sand = ui.Image("Sand", root.rectTransform, ui.Sprites.Sand[0], Color.white, cornerScale: CellCornerScale / 2f);
            sand.rectTransform.Stretch();

            var grains = UiKit.Rect("Grains", root.rectTransform).Stretch().gameObject.AddComponent<SandGrainGraphic>();
            grains.material = grainMaterial;
            grains.raycastTarget = false;
            grains.gameObject.SetActive(false);

            root.gameObject.AddComponent<CellCover>()
                .Wire("sand", sand)
                .Wire("shape", root)
                .Wire("grains", grains)
                .Wire("variants", ui.Sprites.Sand);
            return Save<CellCover>(root.rectTransform, BuildPaths.CellCoverPrefab);
        }

        /// <summary>A material for one of the sand's own shaders; an asset, so the shader ships in builds.</summary>
        private static Material WriteMaterial(string shaderName, string path)
        {
            var shader = Shader.Find(shaderName) ?? throw new InvalidOperationException($"Shader '{shaderName}' not found.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                AssetFolders.Ensure(Path.GetDirectoryName(path));
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static ItemView BuildItemView(UiKit ui)
        {
            var root = UiKit.Rect("ItemView", null);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

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
            // Overlaps the icon's bottom-right corner so it reads as part of the item; the crown keeps the top-right.
            var orderBadge = ui.Icon("OrderMatchBadge", body, "check");
            orderBadge.rectTransform.Span(new Vector2(0.58f, 0.04f), new Vector2(0.96f, 0.42f));
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

        private static OrderCardView BuildOrderCard(UiKit ui, RequirementSlotView requirementSlot, Material portraitMaterial)
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

            var customer = BuildCustomer(ui, content, portraitMaterial);
            var requirements = BuildSpeechBubble(ui, content);
            var reward = BuildRewardPill(ui, content);
            var deliver = BuildDeliverButton(ui, content, out var deliverPulse);

            var celebration = ui.Text("CelebrationLabel", body, "+0", 96f, TextTone.Title);
            celebration.rectTransform.Pin(RectLayout.Center, new Vector2(400f, 140f), new Vector2(0f, -40f));
            celebration.color = ui.Theme.Gold;

            root.gameObject.AddComponent<OrderCardView>()
                .Wire("body", body)
                .Wire("content", contentGroup)
                .Wire("customer", customer)
                .Wire("requirementContainer", requirements)
                .Wire("requirementPrefab", requirementSlot)
                .Wire("rewardLabel", reward)
                .Wire("deliverButton", deliver.Button)
                .Wire("deliverPulse", deliverPulse)
                .Wire("celebrationGlow", glow)
                .Wire("celebrationLabel", celebration);
            return Save<OrderCardView>(root, BuildPaths.OrderCardPrefab);
        }

        /// <summary>
        /// The customer on its disc: a Spine portrait whose shoulders are masked to the disc and whose head may rise above
        /// it, with a still avatar for customers without one and a glow behind the disc while the order is ready.
        /// The portrait's texture is straight alpha and the card fades through a CanvasGroup, so the material reads straight
        /// alpha and is CanvasGroup compatible, and the mesh keeps its vertex colours unpremultiplied (Spine's guidance for both).
        /// </summary>
        private static CustomerPortraitView BuildCustomer(UiKit ui, RectTransform card, Material portraitMaterial)
        {
            const float discOffsetX = 20f;
            const float discOffsetY = -22f;
            const float frameInset = 8f;
            var size = LayoutMetrics.AvatarSize;

            const float glowSpread = 46f;
            var glow = ui.Image("ReadyGlow", card, ui.Sprites.Glow, WithAlpha(ui.Theme.Gold, 0f));
            glow.rectTransform.Pin(RectLayout.TopLeft, new Vector2(size + 2f * glowSpread, size + 2f * glowSpread),
                new Vector2(discOffsetX - glowSpread, discOffsetY + glowSpread));
            glow.preserveAspect = false;
            glow.gameObject.SetActive(false);

            var disc = ui.Image("AvatarDisc", card, ui.Sprites.Circle, ui.Theme.CellDark);
            disc.rectTransform.Pin(RectLayout.TopLeft, new Vector2(size, size), new Vector2(discOffsetX, discOffsetY));

            var frame = UiKit.Rect("Frame", disc.rectTransform);
            frame.Inset(frameInset);

            // The frame's circle with room above it for the head; the actor's origin is the circle's centre.
            var frameSize = size - 2f * frameInset;
            var windowHeight = frameSize * (1f + LayoutMetrics.PortraitHeadroomAboveFrame);
            var window = UiKit.Rect("Window", disc.rectTransform)
                .Pin(RectLayout.Bottom, new Vector2(frameSize, windowHeight), new Vector2(0f, frameInset));
            window.gameObject.AddComponent<PortraitWindow>().raycastTarget = true;
            window.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var actor = UiKit.Rect("Actor", window).Pin(new Vector2(0.5f, frameSize * 0.5f / windowHeight), Vector2.zero, pivot: RectLayout.Center);

            var avatar = ui.Image("Avatar", actor, null, Color.white);
            avatar.rectTransform.Pin(RectLayout.Center, new Vector2(frameSize, frameSize));
            avatar.raycastTarget = false;

            var portrait = UiKit.Rect("Portrait", actor)
                .Pin(RectLayout.Center, Vector2.zero)
                .gameObject.AddComponent<SkeletonGraphic>();
            portrait.material = portraitMaterial;
            portrait.raycastTarget = false;
            portrait.MeshGenerator.settings.pmaVertexColors = false;
            portrait.gameObject.SetActive(false);

            return disc.gameObject.AddComponent<CustomerPortraitView>()
                .Wire("frame", frame)
                .Wire("actor", actor)
                .Wire("portrait", portrait)
                .Wire("avatar", avatar)
                .Wire("readyGlow", glow);
        }

        private static Material WritePortraitMaterial()
        {
            var material = WriteMaterial(PortraitShader, BuildPaths.CustomerPortraitMaterial);
            material.SetInt("_StraightAlphaInput", 1);
            material.EnableKeyword("_STRAIGHT_ALPHA_INPUT");
            material.SetInt("_CanvasGroupCompatible", 1);
            material.EnableKeyword("_CANVAS_GROUP_COMPATIBLE");
            EditorUtility.SetDirty(material);
            return material;
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
            // Pivoted on its centre so the pulse grows evenly in every direction; the offset places that centre.
            pulse = UiKit.Rect("DeliverPulse", card)
                .Pin(RectLayout.BottomRight, new Vector2(220f, 92f), new Vector2(-128f, 62f), pivot: RectLayout.Center);
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
