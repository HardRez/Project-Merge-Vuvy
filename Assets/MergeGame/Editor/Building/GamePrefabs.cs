using System;
using System.Linq;
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
    /// <summary>The prefabs the game scene instantiates at runtime: board cells, pooled items and popups, order cards and customers.</summary>
    internal sealed class GamePrefabs
    {
        private const float CellCornerScale = 0.7f;
        private const string SandGrainShader = "MergeGame/UI/Sand Grains";
        private const string SandMaskShader = "MergeGame/UI/Sand Mask";
        private const string PortraitShader = "Spine/SkeletonGraphic";
        private const string AdditiveShader = "MergeGame/UI/Additive";

        /// <summary>The colours of the pack's merge glow, added as light: a deep gold and a warm white.</summary>
        public static readonly Color GlowGold = new Color(1f, 0.74f, 0.22f, 1f);
        public static readonly Color GlowWarm = new Color(1f, 0.93f, 0.7f, 1f);

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
        public CustomerPortraitView CustomerStage { get; private set; }

        /// <summary>Writes every prefab; builds them in the open scene, which should be a scratch scene.</summary>
        public static void Generate(UiKit ui)
        {
            BuildBoardCell(ui);
            BuildCellCover(ui, WriteMaterial(SandGrainShader, BuildPaths.SandGrainMaterial), WriteMaterial(SandMaskShader, BuildPaths.SandMaskMaterial));
            BuildItemView(ui);
            BuildPointsPopup(ui);
            BuildOrderCard(ui, BuildRequirementSlot(ui));
            BuildCustomerStage(ui, WritePortraitMaterial());
        }

        public static GamePrefabs Load() => new GamePrefabs
        {
            BoardCell = LoadPrefab<Image>(BuildPaths.BoardCellPrefab),
            CellCover = LoadPrefab<CellCover>(BuildPaths.CellCoverPrefab),
            ItemView = LoadPrefab<ItemView>(BuildPaths.ItemViewPrefab),
            PointsPopup = LoadPrefab<PointsPopup>(BuildPaths.PointsPopupPrefab),
            OrderCard = LoadPrefab<OrderCardView>(BuildPaths.OrderCardPrefab),
            CustomerStage = LoadPrefab<CustomerPortraitView>(BuildPaths.CustomerStagePrefab)
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
        /// <summary>Adds an image to what is under it, as light (the merge glow and burst); written once, shared.</summary>
        public static Material AdditiveMaterial() => WriteMaterial(AdditiveShader, BuildPaths.UiAdditiveMaterial);

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

            // Behind everything else of the item, so the ring spreads out from under the new item.
            var mergeRing = ui.Image("MergeRing", root, ui.Sprites.MergeRing, Color.white);
            mergeRing.rectTransform.Inset(-6f);
            mergeRing.gameObject.SetActive(false);
            var burst = UiKit.Rect("MergeBurst", root).Stretch();

            var bubblePop = BuildBubblePop(ui, root);

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
            // A clear bubble around the item, so a locked item stays fully readable. The bubble fills about 72% of its
            // image, so the rect reaches past the cell for the bubble to wrap the whole item. It breathes on a layer of
            // its own, so its swell and burst on a merge compose with the breathing.
            var breathing = UiKit.Rect("LockOverlay", body).Span(new Vector2(-0.15f, -0.15f), new Vector2(1.15f, 1.15f));
            var lockOverlay = ui.Image("Bubble", breathing, ui.Sprites.LockBubble, Color.white);
            lockOverlay.rectTransform.Stretch();
            breathing.gameObject.AddComponent<BubbleBreath>().Wire("bubble", lockOverlay);
            var crown = ui.Icon("MaxLevelBadge", body, "crown");
            crown.rectTransform.Span(new Vector2(0.6f, 0.62f), new Vector2(1.04f, 1.06f));
            crown.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -18f);
            // Overlaps the icon's bottom-right corner so it reads as part of the item; the crown keeps the top-right.
            var orderBadge = ui.Icon("OrderMatchBadge", body, "check");
            orderBadge.rectTransform.Span(new Vector2(0.58f, 0.04f), new Vector2(0.96f, 0.42f));
            orderBadge.gameObject.SetActive(false);
            // A glint at the top-right corner: the star's bright core would hide the new item if it sat over it.
            var sparkles = ui.Image("BurstSparkles", body, ui.Sprites.SparkleStar, Color.white);
            sparkles.rectTransform.Span(new Vector2(0.5f, 0.5f), new Vector2(1.12f, 1.12f));
            sparkles.gameObject.SetActive(false);

            var accent = BuildAccent(ui, accentLayer, accentGlow);
            var mergeBurst = BuildMergeBurst(ui, burst, UiKit.Rect("MergeBurstStars", root).Stretch());
            var orderMark = root.gameObject.AddComponent<OrderMatchMark>()
                .Wire("halo", orderHalo)
                .Wire("badge", orderBadge);
            root.gameObject.AddComponent<ItemView>()
                .Wire("orderMark", orderMark)
                .Wire("body", body)
                .Wire("accent", accent)
                .Wire("icon", icon)
                .Wire("lockOverlay", lockOverlay)
                .Wire("bubblePop", bubblePop)
                .Wire("maxLevelBadge", crown.gameObject)
                .Wire("burstGlow", glow)
                .Wire("burstSparkles", sparkles)
                .Wire("mergeRing", mergeRing)
                .Wire("mergeBurst", mergeBurst);
            return Save<ItemView>(root, BuildPaths.ItemViewPrefab);
        }

        /// <summary>
        /// The burst of a merge, from the pack's own merge glow: behind the item a warm flash, a fan of rays and a shock
        /// wave ring; over it (in <paramref name="front"/>, the item's last layer) stars that fly out of it.
        /// </summary>
        private static MergeBurst BuildMergeBurst(UiKit ui, RectTransform behind, RectTransform front)
        {
            var light = AdditiveMaterial();
            var flash = ui.Image("Flash", behind, ui.Sprites.MergeHalo, GlowWarm);
            flash.rectTransform.Span(new Vector2(-0.4f, -0.4f), new Vector2(1.4f, 1.4f));
            var rays = ui.Image("Rays", behind, ui.Sprites.MergeRays, GlowGold);
            rays.rectTransform.Span(new Vector2(-0.6f, -0.6f), new Vector2(1.6f, 1.6f));
            var ring = ui.Image("ShockRing", behind, ui.Sprites.MergeHaloRing, GlowWarm);
            ring.rectTransform.Stretch();

            var stars = new Image[MergeBurstStars];
            for (var i = 0; i < stars.Length; i++)
            {
                stars[i] = ui.Image($"Star{i + 1}", front, ui.Sprites.GlintStar, GlowWarm);
                stars[i].rectTransform.Span(new Vector2(0.4f, 0.3f), new Vector2(0.6f, 0.7f));
            }

            foreach (var part in stars.Append(flash).Append(rays).Append(ring))
                part.material = light;

            var component = behind.gameObject.AddComponent<MergeBurst>()
                .Wire("flash", flash)
                .Wire("rays", rays)
                .Wire("shockRing", ring)
                .Wire("stars", stars);
            component.Rest();
            return component;
        }

        private const int MergeBurstStars = 6;

        /// <summary>
        /// The burst of a locked item's bubble, over the bubble's own rect: the pack's popping-bubble sheet as a flipbook,
        /// and small bubbles that spray outwards.
        /// </summary>
        private static BubblePop BuildBubblePop(UiKit ui, RectTransform root)
        {
            var pop = UiKit.Rect("BubblePop", root).Span(new Vector2(-0.15f, -0.15f), new Vector2(1.15f, 1.15f));
            var shell = ui.Image("Shell", pop, ui.Sprites.LockBubble, Color.white);
            shell.rectTransform.Stretch();
            var flash = ui.Image("Flash", pop, ui.Sprites.Glow, Color.white);
            flash.preserveAspect = false;
            flash.rectTransform.Inset(10f);
            var skin = UiKit.Rect("Skin", pop).Stretch().gameObject.AddComponent<RawImage>();
            skin.texture = ui.Sprites.BubblePopSheet.texture;
            skin.raycastTarget = false;

            // Many small droplets in three sizes, so the spray is dense and has depth without reaching further.
            float[] sizes = { 56f, 42f, 30f };
            const int dropletCount = 16;
            var droplets = new Image[dropletCount];
            for (var i = 0; i < dropletCount; i++)
            {
                var size = sizes[i % sizes.Length];
                var droplet = ui.Image($"Droplet{i + 1}", pop, ui.Sprites.BubbleDroplet, Color.white);
                droplet.rectTransform.Pin(RectLayout.Center, new Vector2(size, size), pivot: RectLayout.Center);
                droplets[i] = droplet;
            }

            var component = pop.gameObject.AddComponent<BubblePop>()
                .Wire("shell", shell)
                .Wire("flash", flash)
                .Wire("skin", skin)
                .Wire("droplets", droplets);
            pop.gameObject.SetActive(false);
            return component;
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
            var sparkles = BuildSparkleCluster(ui, layer, out var twinkles);

            return layer.gameObject.AddComponent<ItemAccent>()
                .Wire("glow", glow)
                .Wire("hintRing", hintRing)
                .Wire("shineMask", shineMask)
                .Wire("shine", shine.rectTransform)
                .Wire("sparkles", sparkles)
                .Wire("twinkles", twinkles)
                .Wire("targetGlowColor", ui.Theme.Gold);
        }

        // (centre, size) of each star in the cluster, normalised to the cluster: one large star and two small ones.
        private static readonly (Vector2 centre, float size)[] SparkleStars =
        {
            (new Vector2(0.46f, 0.48f), 0.78f),
            (new Vector2(0.86f, 0.86f), 0.4f),
            (new Vector2(0.12f, 0.14f), 0.34f)
        };

        /// <summary>A few twinkling stars over the item's top-left corner, faded as one through a canvas group.</summary>
        private static CanvasGroup BuildSparkleCluster(UiKit ui, RectTransform layer, out Image[] twinkles)
        {
            var cluster = UiKit.Rect("AccentSparkles", layer).Span(new Vector2(-0.16f, 0.56f), new Vector2(0.44f, 1.16f));
            var group = cluster.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            twinkles = new Image[SparkleStars.Length];
            for (var i = 0; i < SparkleStars.Length; i++)
            {
                var (centre, size) = SparkleStars[i];
                var star = ui.Image($"Star{i + 1}", cluster, ui.Sprites.SparkleStar, Color.white);
                var half = new Vector2(size, size) * 0.5f;
                star.rectTransform.Span(centre - half, centre + half);
                twinkles[i] = star;
            }

            return group;
        }

        /// <summary>
        /// "+25" with the score's bills before it, so a merge shows what it earned; the two are centred together over the
        /// merged cell, however wide the number.
        /// </summary>
        private static PointsPopup BuildPointsPopup(UiKit ui)
        {
            var root = UiKit.Rect("PointsPopup", null).Pin(RectLayout.Center, new Vector2(360f, 130f));
            root.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // The bills are wider than tall (422 x 308); drawn at the label's cap height.
            ui.Image("Cash", root, ui.Sprites.ScoreCash, Color.white).Prefer(width: 96f, height: 70f);
            var label = ui.Text("Label", root, "+10", 76f, TextTone.Light);
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
            icon.rectTransform.Stretch();
            // On the item's lower right corner; the item art's own margin keeps it clear of the slot's edge.
            var check = ui.Icon("Check", root, "check");
            check.rectTransform.Pin(RectLayout.BottomRight, new Vector2(54f, 54f), new Vector2(-6f, 8f));
            check.gameObject.SetActive(false);

            root.gameObject.AddComponent<RequirementSlotView>()
                .Wire("icon", icon)
                .Wire("checkMark", check.rectTransform);
            return Save<RequirementSlotView>(root, BuildPaths.RequirementSlotPrefab);
        }

        /// <summary>
        /// The front of an order, standing on the counter: the requested items on a tray, and on the counter's front the
        /// reward tag with the deliver button right under it. Its customer is a separate prefab drawn behind the
        /// counter (<see cref="BuildCustomerStage"/>).
        /// </summary>
        private static OrderCardView BuildOrderCard(UiKit ui, RequirementSlotView requirementSlot)
        {
            var root = UiKit.Rect("OrderCard", null).Pin(RectLayout.Center, new Vector2(500f, LayoutMetrics.OrdersHeight));

            var body = UiKit.Rect("Body", root).Stretch();

            // A golden flash over the slot, under its content.
            var glow = ui.Image("CelebrationGlow", body, ui.Sprites.Glow, WithAlpha(ui.Theme.Gold, 0f));
            glow.rectTransform.Inset(-20f);
            glow.preserveAspect = false;

            var content = UiKit.Rect("Content", body).Stretch();
            var contentGroup = content.gameObject.AddComponent<CanvasGroup>();

            var tray = BuildTray(ui, content);
            var deliver = BuildDeliverButton(ui, content, out var deliverPulse);
            var reward = BuildRewardTag(ui, content);

            var celebration = ui.Text("CelebrationLabel", body, "+0", 96f, TextTone.Title);
            celebration.rectTransform.Pin(RectLayout.Center, new Vector2(400f, 140f), new Vector2(0f, 40f));
            celebration.color = ui.Theme.Gold;

            root.gameObject.AddComponent<OrderCardView>()
                .Wire("body", body)
                .Wire("content", contentGroup)
                .Wire("requirementContainer", tray)
                .Wire("requirementPrefab", requirementSlot)
                .Wire("rewardLabel", reward)
                .Wire("payoutOrigin", reward.rectTransform.parent)
                .Wire("deliverButton", deliver.Button)
                .Wire("deliverPulse", deliverPulse)
                .Wire("celebrationGlow", glow)
                .Wire("celebrationLabel", celebration);
            return Save<OrderCardView>(root, BuildPaths.OrderCardPrefab);
        }

        /// <summary>
        /// The pack's white tray on the counter top, as wide as the items on it; the items stand on it and rise above its
        /// rim. Sliced across only, so it is drawn at its art's height.
        /// </summary>
        /// <returns>The tray, which lays the requirement slots out.</returns>
        private static RectTransform BuildTray(UiKit ui, RectTransform card)
        {
            var art = ui.Sprites.OrderTray;
            var tray = ui.Image("Tray", card, art, Color.white, cornerScale: LayoutMetrics.TrayHeight / art.rect.height);
            tray.rectTransform.Pin(RectLayout.Bottom, new Vector2(0f, LayoutMetrics.TrayHeight), new Vector2(0f, LayoutMetrics.TrayBottom));

            var layout = tray.gameObject.AddComponent<HorizontalLayoutGroup>();
            // The items stand in the tray's dish, just above its front rim.
            layout.padding = new RectOffset(28, 28, 0, Mathf.RoundToInt(LayoutMetrics.TrayHeight * 0.08f));
            layout.childAlignment = TextAnchor.LowerCenter;
            // The items' own margins already space them apart.
            layout.spacing = -4f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            tray.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return tray.rectTransform;
        }

        /// <summary>
        /// A customer behind the counter: a Spine portrait standing in the space above the counter (its frame) and
        /// reaching down behind it, with a still avatar for customers without one and a glow behind it while the order is
        /// ready. The portrait's texture is straight alpha and the slot fades through a CanvasGroup, so the material reads
        /// straight alpha and is CanvasGroup compatible, and the mesh keeps its vertex colours unpremultiplied (Spine's
        /// guidance for both).
        /// <para>
        /// The customer is clipped just under the plank's top face: whatever its animations do (leaving sinks it), it never
        /// shows below the counter. The clip reaches far past the slot's sides and top, so hops and heads stay whole.
        /// </para>
        /// </summary>
        private static CustomerPortraitView BuildCustomerStage(UiKit ui, Material portraitMaterial)
        {
            var root = UiKit.Rect("CustomerStage", null).Pin(RectLayout.Center, new Vector2(500f, LayoutMetrics.OrdersHeight));
            var group = root.gameObject.AddComponent<CanvasGroup>();

            const float clipReach = 400f;
            var clip = UiKit.Rect("CounterClip", root).Stretch(-clipReach, LayoutMetrics.CustomerClipBottom, -clipReach, -clipReach);
            clip.gameObject.AddComponent<RectMask2D>();

            var frame = UiKit.Rect("Frame", clip)
                .Stretch(clipReach, LayoutMetrics.CounterTop - LayoutMetrics.CustomerClipBottom, clipReach, clipReach);

            const float glowSize = 480f;
            var glow = ui.Image("ReadyGlow", frame, ui.Sprites.Glow, WithAlpha(ui.Theme.Gold, 0f));
            glow.rectTransform.Pin(RectLayout.Center, new Vector2(glowSize, glowSize), new Vector2(0f, 20f));
            glow.preserveAspect = false;
            glow.gameObject.SetActive(false);

            // Catches taps on the customer, which make it hop.
            var hitArea = ui.Image("HitArea", frame, null, Color.clear);
            hitArea.rectTransform.Pin(RectLayout.Center, new Vector2(260f, 0f));
            hitArea.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            hitArea.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            hitArea.raycastTarget = true;

            // The actor's origin is the frame's centre; it hops.
            var actor = UiKit.Rect("Actor", frame).Pin(RectLayout.Center, Vector2.zero);

            var avatar = ui.Image("Avatar", actor, null, Color.white);
            avatar.rectTransform.Pin(RectLayout.Center, new Vector2(260f, 260f));

            var portrait = UiKit.Rect("Portrait", actor)
                .Pin(RectLayout.Center, Vector2.zero)
                .gameObject.AddComponent<SkeletonGraphic>();
            portrait.material = portraitMaterial;
            portrait.raycastTarget = false;
            portrait.MeshGenerator.settings.pmaVertexColors = false;
            portrait.gameObject.SetActive(false);

            root.gameObject.AddComponent<CustomerPortraitView>()
                .Wire("group", group)
                .Wire("frame", frame)
                .Wire("actor", actor)
                .Wire("portrait", portrait)
                .Wire("avatar", avatar)
                .Wire("readyGlow", glow);
            return Save<CustomerPortraitView>(root, BuildPaths.CustomerStagePrefab);
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

        /// <summary>The reward as a tag centred right above the deliver button, a small gap apart.</summary>
        private static TextMeshProUGUI BuildRewardTag(UiKit ui, RectTransform card)
        {
            const float height = LayoutMetrics.RewardTagHeight;
            var pill = ui.Image("RewardPill", card, ui.Sprites.Pill, ui.Theme.CellLight, cornerScale: height / 128f);
            pill.rectTransform.Pin(RectLayout.Bottom, new Vector2(146f, height), new Vector2(0f, LayoutMetrics.RewardTagCentre), pivot: RectLayout.Center);
            ui.Image("Cash", pill.rectTransform, ui.Sprites.ScoreCash, Color.white).rectTransform.Pin(RectLayout.Left, new Vector2(64f, 48f), new Vector2(-10f, 2f));
            var label = ui.Text("Reward", pill.rectTransform, "+0", 32f, TextTone.Dark, TextAlignmentOptions.Center);
            label.rectTransform.Stretch(52f, 2f, 10f, 0f);
            return UiKit.ShrinkToFit(label, 22f);
        }

        private static ButtonParts BuildDeliverButton(UiKit ui, RectTransform card, out RectTransform pulse)
        {
            // Pivoted on its centre so the button pops in and pulses evenly in every direction; the offset places that centre.
            const float height = LayoutMetrics.DeliverButtonHeight;
            pulse = UiKit.Rect("DeliverPulse", card)
                .Pin(RectLayout.Bottom, new Vector2(224f, height), new Vector2(0f, LayoutMetrics.DeliverButtonCentre), pivot: RectLayout.Center);
            // The pack's glossy green button, sliced across only and drawn at its art's proportions; its darker bottom rim
            // is about a tenth of its height, so the label sits above it.
            var button = ui.ArtButton("DeliverButton", pulse, ui.Sprites.GreenButton, contentLift: height * 0.1f, height: height);
            button.Rect.Stretch();
            var label = ui.Text("Label", button.Content, "Teslim Et", 36f, TextTone.Light);
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
