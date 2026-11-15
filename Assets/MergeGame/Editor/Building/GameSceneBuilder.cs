using MergeGame.App.Installers;
using MergeGame.View.Board;
using MergeGame.View.Common;
using MergeGame.View.Feedback;
using MergeGame.View.Hints;
using MergeGame.View.Navigation;
using MergeGame.View.Notices;
using MergeGame.View.Orders;
using MergeGame.View.Score;
using MergeGame.View.Selection;
using MergeGame.View.Settings;
using MergeGame.View.Spawning;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// Generates <c>Game.unity</c>: one portrait column of top bar, orders strip, board, item info bar and spawn button.
    /// The board takes whatever height the other sections leave. The screen is a little shop: the customers stand behind
    /// a counter in front of a papered wall, and the board lies on the parquet floor.
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
            var column = Column(SceneScaffold.SafeContent(ui, canvas, ui.Sprites.ShopFloor, LayoutMetrics.ShopFloorTileWidth));

            // Orders, board and info bar form one group. Height beyond the 9:16 reference first lets the orders strip
            // and the info bar grow a little, the rest is shared between the space above the group and above the
            // spawn button; the board itself never stretches past its natural size.
            var menu = TopBar(column, out var score, out var home, out var settingsButton);
            Spacer("SpaceAboveGroup", column, LayoutMetrics.SpaceAboveGroupShare);
            var orders = OrdersStrip(column, out var counterPlank);
            var boardArea = UiKit.Rect("BoardArea", column);
            var itemInfo = InfoBar(column);
            Spacer("SpaceAboveSpawn", column, LayoutMetrics.SpaceAboveSpawnShare);
            var spawn = SpawnBar(column);

            ShopScenery.Wall(ui, canvas, counterPlank);

            var pool = UiKit.Rect("Pool", canvas).Stretch();
            var dragLayer = UiKit.Rect("DragLayer", canvas).Stretch();
            // Last on the canvas, so the popup covers everything, dragged items included.
            var settings = SettingsPopup(canvas, settingsButton);
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
            foreach (var button in new[] { settings.OpenButton, settings.CloseButton, settings.SoundButton, settings.VibrationButton })
                SceneScaffold.ClickSound(button, sounds);

            var haptics = new GameObject("Haptics").AddComponent<Haptics>();
            board.Wire("haptics", haptics);
            orders.Wire("haptics", haptics);

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
                .Wire("settingsView", settings)
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

        private GameMenuView TopBar(RectTransform column, out ScoreView score, out Button homeButton, out Button settingsButton)
        {
            var bar = Section("TopBar", column, LayoutMetrics.TopBarHeight);
            var size = LayoutMetrics.TopBarHeight;

            // The pack's glossy blue square; its bottom rim is about a tenth of its height.
            var home = ui.ArtButton("HomeButton", bar, ui.Sprites.HomeButton, contentLift: 8f);
            // Pivoted on its centre, as every tactile button is, so a press shrinks it towards its middle.
            home.Rect.Pin(RectLayout.Left, new Vector2(size, size), new Vector2(size / 2f, 0f), pivot: RectLayout.Center);
            ui.Icon("Icon", home.Content, "home").rectTransform.Inset(18f);

            // The same glossy blue square on the right, with a gear: the settings.
            var settings = ui.ArtButton("SettingsButton", bar, ui.Sprites.HomeButton, contentLift: 8f);
            settings.Rect.Pin(RectLayout.Right, new Vector2(size, size), new Vector2(-size / 2f, 0f), pivot: RectLayout.Center);
            ui.Image("Icon", settings.Content, ui.Sprites.SettingsIcon, Color.white).rectTransform.Inset(20f);

            score = ScorePill(bar);
            homeButton = home.Button;
            settingsButton = settings.Button;
            return bar.gameObject.AddComponent<GameMenuView>().Wire("backButton", home.Button);
        }

        /// <summary>
        /// The settings popup over the whole screen, laid out like the asset pack's own settings window: a dim that closes
        /// it when tapped, a small wooden window with a title tab across its top edge and a round close button in its top
        /// right corner, and a row of the pack's small green switch buttons, each with its name below.
        /// </summary>
        private SettingsView SettingsPopup(RectTransform canvas, Button openButton)
        {
            var root = UiKit.Rect("Settings", canvas).Stretch();
            var popup = UiKit.Rect("Popup", root).Stretch();
            var group = popup.gameObject.AddComponent<CanvasGroup>();

            var dim = ui.Image("Dim", popup, null, new Color(0.12f, 0.08f, 0.05f, 0.6f));
            dim.rectTransform.Stretch();
            dim.raycastTarget = true;
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;

            var window = UiKit.Rect("Window", popup).Pin(RectLayout.Center, new Vector2(640f, 390f), new Vector2(0f, 40f), pivot: RectLayout.Center);
            var frame = ui.Image("Frame", window, ui.Sprites.WindowFrame, Color.white);
            frame.rectTransform.Stretch();
            // Catches taps on the window, so only taps beside it close the popup.
            frame.raycastTarget = true;
            ui.Image("Panel", window, ui.Sprites.WindowPanel, Color.white, cornerScale: 0.8f).rectTransform.Stretch(25f, 34f, 25f, 24f);

            TitleTab(window, "Ayarlar");

            var close = ui.ArtButton("CloseButton", window, ui.Sprites.WindowClose, contentLift: 0f);
            close.Rect.Pin(RectLayout.TopRight, new Vector2(82f, 82f), new Vector2(-36f - 41f, -40f - 41f), pivot: RectLayout.Center);

            const float switchSpacing = 220f;
            var sound = Switch(window, "Sound", "Ses", ui.Sprites.SoundIcon, new Vector2(61f, 52f), -switchSpacing / 2f, out var soundOff);
            var vibration = Switch(window, "Vibration", "Titreşim", ui.Sprites.VibrationIcon, new Vector2(76f, 59f), switchSpacing / 2f, out var vibrationOff);

            return root.gameObject.AddComponent<SettingsView>()
                .Wire("openButton", openButton)
                .Wire("popup", group)
                .Wire("window", window)
                .Wire("closeButton", close.Button)
                .Wire("dimButton", dimButton)
                .Wire("soundButton", sound)
                .Wire("soundOffMark", soundOff)
                .Wire("vibrationButton", vibration)
                .Wire("vibrationOffMark", vibrationOff);
        }

        /// <summary>
        /// The window's title on its tab. The pack draws only the left half of the tab; the right half is the same art
        /// mirrored, as in the pack's own windows.
        /// </summary>
        private void TitleTab(RectTransform window, string text)
        {
            const float height = 80f;
            var art = ui.Sprites.WindowTitle;
            var halfWidth = art.rect.width * height / art.rect.height;
            var tab = UiKit.Rect("Title", window).Pin(RectLayout.Top, new Vector2(halfWidth * 2f, height), new Vector2(0f, height / 2f + 6f));
            ui.Image("Left", tab, art, Color.white).rectTransform
                .Pin(RectLayout.Left, new Vector2(halfWidth, height), new Vector2(halfWidth / 2f, 0f), pivot: RectLayout.Center);
            var right = ui.Image("Right", tab, art, Color.white).rectTransform;
            right.Pin(RectLayout.Right, new Vector2(halfWidth, height), new Vector2(-halfWidth / 2f, 0f), pivot: RectLayout.Center);
            right.localScale = new Vector3(-1f, 1f, 1f);
            var label = ui.Text("Label", tab, text, 46f, TextTone.Light);
            label.rectTransform.Stretch(24f, 8f, 24f, 4f);
        }

        /// <summary>
        /// One switch, as the pack draws them: its small green square button with a white icon (a red slash across it
        /// while off), and its name below.
        /// </summary>
        private Button Switch(RectTransform window, string name, string title, Sprite icon, Vector2 iconSize, float x, out GameObject offMark)
        {
            var size = new Vector2(130f, 127f);
            var button = ui.ArtButton(name + "Button", window, ui.Sprites.SwitchFace, contentLift: 0f);
            button.Rect.Pin(RectLayout.Center, size, new Vector2(x, 14f), pivot: RectLayout.Center);
            ui.Image("Icon", button.Content, icon, Color.white).rectTransform.Pin(RectLayout.Center, iconSize, new Vector2(0f, 2f));
            var off = ui.Image("Off", button.Content, ui.Sprites.SwitchOff, Color.white);
            off.rectTransform.Pin(RectLayout.Center, new Vector2(66f, 47f));
            offMark = off.gameObject;

            var label = ui.Text(name + "Name", window, title, 36f, TextTone.Dark);
            label.rectTransform.Pin(RectLayout.Center, new Vector2(240f, 50f), new Vector2(x, 14f - size.y / 2f - 34f));
            return button.Button;
        }

        private ScoreView ScorePill(RectTransform bar)
        {
            const float height = 100f;
            var pill = ui.Image("ScorePill", bar, ui.Sprites.Pill, ui.Theme.Panel, cornerScale: height / 128f);
            pill.rectTransform.Pin(RectLayout.Center, new Vector2(380f, height));
            // The bills are wider than tall; their rect keeps that shape so they reach over the pill's left end.
            var cash = ui.Image("Cash", pill.rectTransform, ui.Sprites.ScoreCash, Color.white);
            cash.rectTransform.Pin(RectLayout.Left, new Vector2(150f, 110f), new Vector2(-48f, 2f));
            var label = ui.Text("Score", pill.rectTransform, "0", 62f, TextTone.Dark);
            label.rectTransform.Stretch(80f, 8f, 28f, 0f);
            UiKit.ShrinkToFit(label, 36f);

            return pill.gameObject.AddComponent<ScoreView>()
                .Wire("label", label)
                .Wire("pill", pill.rectTransform)
                .Wire("icon", cash.rectTransform);
        }

        /// <summary>
        /// The customers behind the counter and their orders in front of it, in that drawing order, so the counter hides
        /// the customers' lower bodies while the trays stand on it.
        /// </summary>
        /// <param name="counterPlank">The counter's plank, which the wall reaches down behind.</param>
        private OrdersView OrdersStrip(RectTransform column, out RectTransform counterPlank)
        {
            var strip = Section("Orders", column, LayoutMetrics.OrdersHeight, LayoutMetrics.OrdersGrowth);
            var customers = SlotRow("Customers", strip);
            counterPlank = ShopScenery.Counter(ui, strip);
            var cards = SlotRow("Cards", strip);

            return strip.gameObject.AddComponent<OrdersView>()
                .Wire("customerContainer", customers)
                .Wire("customerPrefab", prefabs.CustomerStage)
                .Wire("cardContainer", cards)
                .Wire("cardPrefab", prefabs.OrderCard);
        }

        /// <summary>A row of order slots sharing the strip's width; the customers' row and the cards' row lay out alike.</summary>
        private static RectTransform SlotRow(string name, RectTransform strip)
        {
            var row = UiKit.Rect(name, strip).Stretch();
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = LayoutMetrics.CardSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
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

            var coverExplanation = ui.Text("CoverExplanation", details, "Yanındaki baloncuk patlayınca açılır.", 36f, TextTone.Muted, TextAlignmentOptions.TopLeft);
            coverExplanation.rectTransform.Span(Vector2.zero, new Vector2(1f, 0.5f));
            coverExplanation.rectTransform.offsetMin = new Vector2(170f, 18f);
            coverExplanation.rectTransform.offsetMax = new Vector2(-32f, -4f);
            UiKit.ShrinkToFit(coverExplanation, 26f);
            coverExplanation.gameObject.SetActive(false);

            var discard = ui.Button("DiscardButton", details, ui.Theme.Danger, cornerScale: 0.7f);
            discard.Rect.Pin(RectLayout.Right, new Vector2(116f, 116f), new Vector2(-22f - 58f, 0f), pivot: RectLayout.Center);
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
            // The game's own round "produce" button, grey while the board is full, with a whisk and spatula on it; its
            // name sits on a tag across the bottom of the button, so the button stays round and the word stays readable.
            const float size = 176f;
            const float tagHeight = 58f;
            var button = ui.ArtButton("SpawnButton", bar, ui.Sprites.SpawnButton, ui.Sprites.SpawnButtonDisabled, contentLift: 0f);
            // Pivoted on the circle's centre, so a press shrinks it towards its middle rather than its top edge.
            button.Rect.Pin(RectLayout.Top, new Vector2(size, size), new Vector2(0f, -size / 2f), pivot: RectLayout.Center);

            var icon = ui.Image("Icon", button.Content, ui.Sprites.SpawnIcon, Color.white);
            icon.rectTransform.Pin(RectLayout.Center, new Vector2(size * 0.6f, size * 0.6f), new Vector2(0f, size * 0.06f));

            // Hangs from the button down to the bottom of the bar, over the lower edge of the circle.
            var tag = ui.Image("Tag", button.Content, ui.Sprites.Pill, ui.Theme.CellLight, cornerScale: tagHeight / 128f);
            tag.rectTransform.Pin(RectLayout.Bottom, new Vector2(210f, tagHeight), new Vector2(0f, size - LayoutMetrics.SpawnBarHeight));
            var tagRim = ui.Image("Rim", tag.rectTransform, ui.Sprites.Ring, ui.Theme.WoodLight, cornerScale: tagHeight / 128f);
            tagRim.rectTransform.Stretch();
            var label = ui.Text("Label", tag.rectTransform, "Üret", 46f, TextTone.Dark);
            label.rectTransform.Stretch(12f, 4f, 12f, 0f);
            UiKit.ShrinkToFit(label, 30f);

            return bar.gameObject.AddComponent<SpawnView>()
                .Wire("button", button.Button)
                .Wire("label", label);
        }
    }
}
