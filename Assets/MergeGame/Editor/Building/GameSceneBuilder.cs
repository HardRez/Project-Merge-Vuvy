using MergeGame.App.Installers;
using MergeGame.View.Board;
using MergeGame.View.Common;
using MergeGame.View.Hints;
using MergeGame.View.Navigation;
using MergeGame.View.Notices;
using MergeGame.View.Orders;
using MergeGame.View.Score;
using MergeGame.View.Selection;
using MergeGame.View.Spawning;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// Generates <c>Game.unity</c>: one portrait column of top bar, orders strip, board, item info bar and spawn button.
    /// The board takes whatever height the other sections leave.
    /// </summary>
    internal sealed class GameSceneBuilder
    {
        private readonly UiKit ui;
        private readonly GamePrefabs prefabs;
        private readonly NoticeTexts noticeTexts;

        private GameSceneBuilder(UiKit ui, GamePrefabs prefabs, NoticeTexts noticeTexts)
        {
            this.ui = ui;
            this.prefabs = prefabs;
            this.noticeTexts = noticeTexts;
        }

        public static void Build()
        {
            var scene = SceneScaffold.NewScene();
            var ui = UiKit.Load();
            new GameSceneBuilder(ui, GamePrefabs.Load(), UiDataAssets.LoadOrCreateNoticeTexts(ui.Sprites)).Populate();
            SceneScaffold.Save(scene, BuildPaths.GameScene);
        }

        private void Populate()
        {
            var camera = SceneScaffold.Camera(ui.Theme.CellLight);
            SceneScaffold.EventSystem();
            var canvas = SceneScaffold.Canvas(camera);
            var column = Column(SceneScaffold.SafeContent(ui, canvas));

            // Orders, board and info bar form one group. Height beyond the 9:16 reference first lets the orders strip
            // and the info bar grow a little, the rest is shared between the space above the group and above the
            // spawn button; the board itself never stretches past its natural size.
            var menu = TopBar(column, out var score, out var home);
            Spacer("SpaceAboveGroup", column, LayoutMetrics.SpaceAboveGroupShare);
            var orders = OrdersStrip(column);
            var boardArea = UiKit.Rect("BoardArea", column);
            var itemInfo = InfoBar(column);
            Spacer("SpaceAboveSpawn", column, LayoutMetrics.SpaceAboveSpawnShare);
            var spawn = SpawnBar(column);

            var pool = UiKit.Rect("Pool", canvas).Stretch();
            var dragLayer = UiKit.Rect("DragLayer", canvas).Stretch();
            var board = Board(boardArea, dragLayer);
            var mergeHint = board.gameObject.AddComponent<MergeHintView>().Wire("board", board);
            var orderMatches = board.gameObject.AddComponent<OrderMatchView>().Wire("board", board);
            boardArea.gameObject.AddComponent<AspectHeightLayoutElement>()
                .Wire("content", board.GetComponent<AspectRatioFitter>())
                .Wire("minHeight", LayoutMetrics.BoardMinHeight);
            var notices = Toast(boardArea);

            var sounds = SceneScaffold.Sounds(UiDataAssets.LoadOrCreateSoundLibrary());
            board.Wire("sounds", sounds);
            orders.Wire("sounds", sounds);
            notices.Wire("sounds", sounds);
            SceneScaffold.ClickSound(home, sounds);

            SceneScaffold.SceneContext<GameInstaller>()
                .Wire("boardView", board)
                .Wire("itemInfoView", itemInfo)
                .Wire("ordersView", orders)
                .Wire("scoreView", score)
                .Wire("spawnView", spawn)
                .Wire("noticeView", notices)
                .Wire("gameMenuView", menu)
                .Wire("mergeHintView", mergeHint)
                .Wire("orderMatchView", orderMatches)
                .Wire("itemPrefab", prefabs.ItemView)
                .Wire("pointsPopupPrefab", prefabs.PointsPopup)
                .Wire("poolRoot", pool);
        }

        private static RectTransform Column(RectTransform safeArea)
        {
            var column = UiKit.Rect("Column", safeArea).Stretch();
            column.gameObject.AddComponent<MaxWidthLimiter>().Wire("maxWidth", LayoutMetrics.MaxColumnWidth);
            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            var margin = Mathf.RoundToInt(LayoutMetrics.ScreenMargin);
            layout.padding = new RectOffset(margin, margin, Mathf.RoundToInt(LayoutMetrics.TopMargin), Mathf.RoundToInt(LayoutMetrics.BottomMargin));
            layout.spacing = LayoutMetrics.SectionSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return column;
        }

        /// <param name="growth">Share of the leftover height on tall screens; 0 keeps the section at its height.</param>
        private static RectTransform Section(string name, RectTransform column, float height, float growth = 0f)
        {
            var section = UiKit.Rect(name, column);
            // Set explicitly: a layout group inside the section would otherwise report its children's flexibility.
            section.Prefer(height: height, flexibleHeight: growth);
            return section;
        }

        private static void Spacer(string name, RectTransform column, float share) =>
            UiKit.Rect(name, column).Prefer(height: 0f, flexibleHeight: share);

        private GameMenuView TopBar(RectTransform column, out ScoreView score, out Button homeButton)
        {
            var bar = Section("TopBar", column, LayoutMetrics.TopBarHeight);
            var size = LayoutMetrics.TopBarHeight;

            var home = ui.Button("HomeButton", bar, ui.Theme.Secondary, cornerScale: 0.75f);
            home.Rect.Pin(RectLayout.Left, new Vector2(size, size));
            ui.Icon("Icon", home.Content, "home").rectTransform.Inset(16f);

            score = ScorePill(bar);
            homeButton = home.Button;
            return bar.gameObject.AddComponent<GameMenuView>().Wire("backButton", home.Button);
        }

        private ScoreView ScorePill(RectTransform bar)
        {
            const float height = 100f;
            var pill = ui.Image("ScorePill", bar, ui.Sprites.Pill, ui.Theme.Panel, cornerScale: height / 128f);
            pill.rectTransform.Pin(RectLayout.Center, new Vector2(380f, height));
            var star = ui.Icon("Star", pill.rectTransform, "star");
            star.rectTransform.Pin(RectLayout.Left, new Vector2(128f, 128f), new Vector2(-40f, 4f));
            var label = ui.Text("Score", pill.rectTransform, "0", 62f, TextTone.Dark);
            label.rectTransform.Stretch(80f, 8f, 28f, 0f);
            UiKit.ShrinkToFit(label, 36f);

            return pill.gameObject.AddComponent<ScoreView>()
                .Wire("label", label)
                .Wire("pill", pill.rectTransform)
                .Wire("star", star.rectTransform);
        }

        private OrdersView OrdersStrip(RectTransform column)
        {
            var strip = Section("Orders", column, LayoutMetrics.OrdersHeight, LayoutMetrics.OrdersGrowth);
            var layout = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = LayoutMetrics.CardSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            return strip.gameObject.AddComponent<OrdersView>()
                .Wire("cardContainer", strip)
                .Wire("cardPrefab", prefabs.OrderCard);
        }

        private BoardView Board(RectTransform area, RectTransform dragLayer)
        {
            var board = UiKit.Rect("Board", area).Stretch();
            var fitter = board.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            // A blue tray in the wooden frame: the warm item art stands out on it, the frame ties it to the warm UI.
            ui.DropShadow(board, spread: 22f, drop: 18f);
            ui.Image("Tray", board, ui.Sprites.BoardTray, Color.white).rectTransform.Inset(4f);
            ui.Image("Frame", board, ui.Sprites.BoardFrame, ui.Theme.WoodDark).rectTransform.Stretch();

            var grid = ui.Image("Grid", board, null, Color.clear);
            grid.raycastTarget = true;
            grid.rectTransform.Inset(LayoutMetrics.BoardFramePadding);
            var input = grid.gameObject.AddComponent<BoardInputSurface>();

            var cells = UiKit.Rect("Cells", grid.rectTransform).Stretch();
            var covers = UiKit.Rect("Covers", grid.rectTransform).Stretch();
            var highlight = DropHighlight(grid.rectTransform);
            var items = UiKit.Rect("Items", grid.rectTransform).Stretch();
            var selection = SelectionMarker(grid.rectTransform);
            var effects = UiKit.Rect("Effects", grid.rectTransform).Stretch();

            return board.gameObject.AddComponent<BoardView>()
                .Wire("boardFitter", fitter)
                .Wire("grid", grid.rectTransform)
                .Wire("input", input)
                .Wire("cellLayer", cells)
                .Wire("coverLayer", covers)
                .Wire("itemLayer", items)
                .Wire("effectLayer", effects)
                .Wire("dragLayer", dragLayer)
                .Wire("cellPrefab", prefabs.BoardCell)
                .Wire("coverPrefab", prefabs.CellCover)
                .Wire("dropHighlight", highlight)
                .Wire("selectionMarker", selection)
                .Wire("theme", ui.Theme);
        }

        private DropHighlightView DropHighlight(RectTransform grid)
        {
            var root = UiKit.Rect("DropHighlight", grid).Stretch();
            var pulsing = UiKit.Rect("Pulsing", root).Stretch();
            var glow = ui.Image("Glow", pulsing, ui.Sprites.Glow, Color.white);
            glow.rectTransform.Inset(-44f);
            glow.preserveAspect = false;
            var fill = ui.Image("Fill", pulsing, ui.Sprites.Cell, Color.white, cornerScale: 0.7f);
            fill.rectTransform.Inset(3f);
            var outline = ui.Image("Outline", pulsing, ui.Sprites.Ring, Color.white, cornerScale: 0.6f);
            outline.rectTransform.Inset(1f);

            return root.gameObject.AddComponent<DropHighlightView>()
                .Wire("theme", ui.Theme)
                .Wire("pulsing", pulsing)
                .Wire("fill", fill)
                .Wire("outline", outline)
                .Wire("glow", glow);
        }

        private SelectionMarkerView SelectionMarker(RectTransform grid)
        {
            var root = UiKit.Rect("SelectionMarker", grid).Stretch();
            // The brackets reach a little past the tile, so they frame the item instead of covering its corners.
            var ring = ui.Image("Brackets", root, ui.Sprites.SelectionBrackets, Color.white, cornerScale: 0.7f);
            ring.rectTransform.Inset(-8f);
            return root.gameObject.AddComponent<SelectionMarkerView>().Wire("ring", ring.rectTransform);
        }

        private NoticeView Toast(RectTransform area)
        {
            var root = UiKit.Rect("Toast", area).Band(fromTop: true, LayoutMetrics.ToastHeight, offset: 46f);
            root.gameObject.AddComponent<CanvasGroup>();

            var bubble = ui.Image("Bubble", root, ui.Sprites.Pill, ui.Theme.ToastBackground, cornerScale: LayoutMetrics.ToastHeight / 128f);
            bubble.rectTransform.Pin(RectLayout.Top, new Vector2(600f, LayoutMetrics.ToastHeight));
            var layout = bubble.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 14, 18);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            bubble.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var icon = ui.Image("Icon", bubble.rectTransform, null, Color.white);
            icon.Prefer(width: 64f);
            var label = ui.Text("Label", bubble.rectTransform, string.Empty, 36f, TextTone.Light);

            return root.gameObject.AddComponent<NoticeView>()
                .Wire("texts", noticeTexts)
                .Wire("theme", ui.Theme)
                .Wire("toast", bubble.rectTransform)
                .Wire("background", bubble)
                .Wire("icon", icon)
                .Wire("label", label);
        }

        private ItemInfoView InfoBar(RectTransform column)
        {
            var bar = Section("ItemInfo", column, LayoutMetrics.InfoBarHeight, LayoutMetrics.InfoBarGrowth);
            ui.DropShadow(bar);
            ui.Image("Panel", bar, ui.Sprites.Panel, ui.Theme.Panel).rectTransform.Stretch();

            var hint = ui.Text("Hint", bar, "Detayları görmek için bir eşyaya dokun", 40f, TextTone.Muted);
            hint.rectTransform.Stretch(32f, 6f, 32f, 0f);
            UiKit.ShrinkToFit(hint, 28f);

            var details = UiKit.Rect("Details", bar).Stretch(0f, 6f, 0f, 0f);
            var disc = ui.Image("IconDisc", details, ui.Sprites.Circle, ui.Theme.CellLight);
            disc.rectTransform.Pin(RectLayout.Left, new Vector2(128f, 128f), new Vector2(22f, 0f));
            var icon = ui.Image("Icon", disc.rectTransform, null, Color.white);
            icon.rectTransform.Inset(12f);

            var name = ui.Text("Name", details, string.Empty, 50f, TextTone.Dark, TextAlignmentOptions.BottomLeft);
            name.rectTransform.Span(new Vector2(0f, 0.5f), Vector2.one);
            name.rectTransform.offsetMin = new Vector2(170f, 0f);
            name.rectTransform.offsetMax = new Vector2(-300f, -14f);
            UiKit.ShrinkToFit(name, 32f);

            var levelRow = LevelRow(details, out var level, out var maxTag, out var lockedTag);
            var next = NextItem(details, out var nextIcon);

            var coverExplanation = ui.Text("CoverExplanation", details, "Yanındaki ağ temizlenince açılır.", 36f, TextTone.Muted, TextAlignmentOptions.TopLeft);
            coverExplanation.rectTransform.Span(Vector2.zero, new Vector2(1f, 0.5f));
            coverExplanation.rectTransform.offsetMin = new Vector2(170f, 18f);
            coverExplanation.rectTransform.offsetMax = new Vector2(-32f, -4f);
            UiKit.ShrinkToFit(coverExplanation, 26f);
            coverExplanation.gameObject.SetActive(false);

            var discard = ui.Button("DiscardButton", details, ui.Theme.Danger, cornerScale: 0.7f);
            discard.Rect.Pin(RectLayout.Right, new Vector2(116f, 116f), new Vector2(-22f, 0f));
            ui.Icon("Icon", discard.Content, "trash").rectTransform.Inset(16f);

            details.gameObject.SetActive(false);
            return bar.gameObject.AddComponent<ItemInfoView>()
                .Wire("hint", hint.gameObject)
                .Wire("details", details.gameObject)
                .Wire("icon", icon)
                .Wire("nameLabel", name)
                .Wire("levelRow", levelRow.gameObject)
                .Wire("levelLabel", level)
                .Wire("maxLevelTag", maxTag)
                .Wire("lockedTag", lockedTag)
                .Wire("next", next)
                .Wire("nextIcon", nextIcon)
                .Wire("discardButton", discard.Button)
                .Wire("coverIcon", ui.Sprites.Sand[0])
                .Wire("coverExplanation", coverExplanation.gameObject);
        }

        private RectTransform LevelRow(RectTransform details, out TextMeshProUGUI level, out GameObject maxTag, out GameObject lockedTag)
        {
            var row = UiKit.Rect("LevelRow", details).Span(Vector2.zero, new Vector2(1f, 0.5f));
            row.offsetMin = new Vector2(170f, 18f);
            row.offsetMax = new Vector2(-300f, 0f);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            level = ui.Text("Level", row, string.Empty, 36f, TextTone.Muted, TextAlignmentOptions.Left);
            level.rectTransform.sizeDelta = new Vector2(0f, 52f);
            maxTag = Tag(row, "MaxLevelTag", "crown", "En Yüksek", ui.Theme.Gold);
            lockedTag = Tag(row, "LockedTag", "lock", "Kilitli", ui.Theme.TextMuted);
            return row;
        }

        private GameObject Tag(RectTransform row, string name, string iconName, string text, Color color)
        {
            const float height = 52f;
            var tag = ui.Image(name, row, ui.Sprites.Pill, color, cornerScale: height / 128f);
            tag.rectTransform.sizeDelta = new Vector2(0f, height);
            var layout = tag.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 18, 4, 8);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            ui.Icon("Icon", tag.rectTransform, iconName).Prefer(width: 38f);
            ui.Text("Label", tag.rectTransform, text, 30f, TextTone.Light);
            return tag.gameObject;
        }

        private GameObject NextItem(RectTransform details, out Image icon)
        {
            var next = UiKit.Rect("Next", details).Pin(RectLayout.Right, new Vector2(140f, 150f), new Vector2(-152f, 0f));
            var label = ui.Text("Label", next, "Sonraki:", 30f, TextTone.Muted, TextAlignmentOptions.Center);
            label.rectTransform.Band(fromTop: true, height: 40f);
            var disc = ui.Image("Disc", next, ui.Sprites.Circle, ui.Theme.CellLight);
            disc.rectTransform.Pin(RectLayout.Bottom, new Vector2(96f, 96f), new Vector2(0f, 6f));
            icon = ui.Image("Icon", disc.rectTransform, null, Color.white);
            icon.rectTransform.Inset(10f);
            return next.gameObject;
        }

        private SpawnView SpawnBar(RectTransform column)
        {
            var bar = Section("SpawnBar", column, LayoutMetrics.SpawnBarHeight);
            var button = ui.Button("SpawnButton", bar, ui.Theme.Primary);
            button.Rect.Pin(RectLayout.Center, new Vector2(600f, LayoutMetrics.SpawnBarHeight - 8f));

            var layout = button.Content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.padding = new RectOffset(0, 10, 18, 18);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            ui.Icon("Icon", button.Content, "sparkles").Prefer(width: 96f);
            var label = ui.Text("Label", button.Content, "Üret", 76f, TextTone.Light);

            return bar.gameObject.AddComponent<SpawnView>()
                .Wire("button", button.Button)
                .Wire("label", label);
        }
    }
}
