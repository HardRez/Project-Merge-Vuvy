using MergeGame.View.Chains;
using MergeGame.View.Common;
using MergeGame.View.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// The game screen's popups, laid out like the asset pack's own windows: the settings window (after the pack's
    /// SettingWindow) and the chains window (after its ItemDetailWindow). Both share the same shell: a dim over the
    /// screen and the wooden window with its cream panel and round close button (<see cref="PopupWindow"/>).
    /// </summary>
    internal sealed class GamePopups
    {
        private readonly UiKit ui;

        public GamePopups(UiKit ui) => this.ui = ui;

        /// <summary>A small window with a title tab and two switches, sound and vibration.</summary>
        public SettingsView Settings(RectTransform canvas, Button openButton)
        {
            var shell = Shell(canvas, "Settings", new Vector2(640f, 390f), new Vector2(0f, 40f));
            TitleTab(shell.Window, "Ayarlar");

            const float switchSpacing = 220f;
            var sound = Switch(shell.Window, "Sound", "Ses", ui.Sprites.SoundIcon, new Vector2(61f, 52f), -switchSpacing / 2f, out var soundOff);
            var vibration = Switch(shell.Window, "Vibration", "Titreşim", ui.Sprites.VibrationIcon, new Vector2(76f, 59f), switchSpacing / 2f, out var vibrationOff);

            return shell.Root.gameObject.AddComponent<SettingsView>()
                .Wire("openButton", openButton)
                .Wire("popup", shell.Popup)
                .Wire("soundButton", sound)
                .Wire("soundOffMark", soundOff)
                .Wire("vibrationButton", vibration)
                .Wire("vibrationOffMark", vibrationOff);
        }

        /// <summary>
        /// A big window with the title on the pack's blue wooden board and a corner flourish. Under the title a tab for
        /// every chain (its best item and name); under the tabs the chosen chain's name between lace flourishes and its
        /// levels, big, in two rows. Tabs and cells are copied at runtime from the templates built here.
        /// </summary>
        public ChainsView Chains(RectTransform canvas, Button openButton)
        {
            var shell = Shell(canvas, "Chains", new Vector2(LayoutMetrics.ChainsWindowWidth, LayoutMetrics.ChainsWindowHeight), new Vector2(0f, -20f));
            var window = shell.Window;

            // The corner flourish sits in the panel's top right corner, under the close button.
            var corner = ui.Image("Corner", window, ui.Sprites.WindowCorner, Color.white);
            corner.rectTransform.Pin(RectLayout.TopRight, new Vector2(150f, 151f), new Vector2(-25f, -24f));
            corner.rectTransform.SetSiblingIndex(2);

            var board = ui.Sprites.TitleBoard;
            const float boardWidth = 700f;
            var boardHeight = boardWidth * board.rect.height / board.rect.width;
            var title = ui.Image("Title", window, board, Color.white);
            title.rectTransform.Pin(RectLayout.Top, new Vector2(boardWidth, boardHeight), new Vector2(0f, -10f), pivot: RectLayout.Center);
            var titleText = ui.Text("Label", title.rectTransform, "Eşya Zincirleri", 56f, TextTone.Title);
            titleText.color = Color.white;
            // On the board's blue face, between its wooden ends.
            titleText.rectTransform.Stretch(boardWidth * 0.16f, boardHeight * 0.3f, boardWidth * 0.16f, boardHeight * 0.28f);
            UiKit.ShrinkToFit(titleText, 40f);

            var tabs = Row("Tabs", window, LayoutMetrics.ChainTabsTop, LayoutMetrics.ChainTabSize, LayoutMetrics.ChainTabSpacing);
            // Tabs give up width, down to a minimum, rather than run off the window when there are many chains.
            tabs.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;

            var heading = UiKit.Rect("Heading", window).Pin(RectLayout.Top, new Vector2(LayoutMetrics.ChainsWindowWidth - 120f, 52f), new Vector2(0f, -LayoutMetrics.ChainHeadingTop));
            var headingRow = heading.gameObject.AddComponent<HorizontalLayoutGroup>();
            headingRow.spacing = 18f;
            headingRow.childAlignment = TextAnchor.MiddleCenter;
            headingRow.childControlWidth = true;
            headingRow.childControlHeight = true;
            headingRow.childForceExpandWidth = false;
            headingRow.childForceExpandHeight = false;
            Flourish(heading, mirrored: false);
            var chainTitle = ui.Text("Name", heading, "Zincir", 46f, TextTone.Dark);
            Flourish(heading, mirrored: true);

            var firstRow = Row("FirstRow", window, LayoutMetrics.ChainRowsTop, LayoutMetrics.ChainCellHeight, LayoutMetrics.ChainCellSpacing);
            var secondRow = Row("SecondRow", window, LayoutMetrics.ChainRowsTop + LayoutMetrics.ChainCellHeight + LayoutMetrics.ChainRowSpacing,
                LayoutMetrics.ChainCellHeight, LayoutMetrics.ChainCellSpacing);

            var templates = UiKit.Rect("Templates", window).Stretch();
            var tab = TabTemplate(templates);
            var cell = CellTemplate(templates);

            return shell.Root.gameObject.AddComponent<ChainsView>()
                .Wire("openButton", openButton)
                .Wire("popup", shell.Popup)
                .Wire("tabContainer", tabs)
                .Wire("tabTemplate", tab)
                .Wire("chainTitle", chainTitle)
                .Wire("firstRow", firstRow)
                .Wire("secondRow", secondRow)
                .Wire("cellTemplate", cell);
        }

        /// <summary>A centred row across the window, <paramref name="top"/> below its top edge, laying its children out side by side.</summary>
        private static RectTransform Row(string name, RectTransform window, float top, float height, float spacing)
        {
            var row = UiKit.Rect(name, window).Pin(RectLayout.Top, new Vector2(LayoutMetrics.ChainsWindowWidth - 80f, height), new Vector2(0f, -top));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        private readonly struct PopupShell
        {
            public PopupShell(RectTransform root, PopupWindow popup, RectTransform window)
            {
                Root = root;
                Popup = popup;
                Window = window;
            }

            public RectTransform Root { get; }
            public PopupWindow Popup { get; }
            public RectTransform Window { get; }
        }

        /// <summary>
        /// The dim over the whole screen, which closes the popup when tapped, and the pack's wooden window with its cream
        /// panel and round close button in the top right corner.
        /// </summary>
        private PopupShell Shell(RectTransform canvas, string name, Vector2 size, Vector2 offset)
        {
            var root = UiKit.Rect(name, canvas).Stretch();
            var popupRoot = UiKit.Rect("Popup", root).Stretch();
            popupRoot.gameObject.AddComponent<CanvasGroup>();

            var dim = ui.Image("Dim", popupRoot, null, new Color(0.12f, 0.08f, 0.05f, 0.6f));
            dim.rectTransform.Stretch();
            dim.raycastTarget = true;
            var dimButton = dim.gameObject.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;

            var window = UiKit.Rect("Window", popupRoot).Pin(RectLayout.Center, size, offset, pivot: RectLayout.Center);
            var frame = ui.Image("Frame", window, ui.Sprites.WindowFrame, Color.white);
            frame.rectTransform.Stretch();
            // Catches taps on the window, so only taps beside it close the popup.
            frame.raycastTarget = true;
            ui.Image("Panel", window, ui.Sprites.WindowPanel, Color.white, cornerScale: 0.8f).rectTransform.Stretch(25f, 34f, 25f, 24f);

            var close = ui.ArtButton("CloseButton", window, ui.Sprites.WindowClose, contentLift: 0f);
            close.Rect.Pin(RectLayout.TopRight, new Vector2(82f, 82f), new Vector2(-36f - 41f, -40f - 41f), pivot: RectLayout.Center);

            var popup = popupRoot.gameObject.AddComponent<PopupWindow>()
                .Wire("window", window)
                .Wire("closeButton", close.Button)
                .Wire("dimButton", dimButton);
            return new PopupShell(root, popup, window);
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

        /// <summary>
        /// A chain's tab: the pack's white square with the chain's best item and its name, and the pack's glowing yellow
        /// square laid over it while it is chosen.
        /// </summary>
        private ChainTabView TabTemplate(RectTransform templates)
        {
            var size = LayoutMetrics.ChainTabSize;
            var tab = UiKit.Rect("Tab", templates).Pin(RectLayout.Center, new Vector2(size, size));
            var element = tab.Prefer(size, size);
            element.minWidth = size * 0.6f;

            var face = ui.Image("Face", tab, ui.Sprites.Tab, Color.white);
            face.preserveAspect = false;
            face.rectTransform.Stretch();
            face.raycastTarget = true;
            // The chosen square has a glow round it, so it is drawn larger than the tab.
            var chosen = ui.Image("Chosen", tab, ui.Sprites.TabSelected, Color.white);
            chosen.preserveAspect = false;
            chosen.rectTransform.Stretch(-12f, -12f, -12f, -12f);

            var icon = ui.Image("Icon", tab, null, Color.white);
            icon.rectTransform.Stretch(18f, size * 0.24f, 18f, 4f);
            var label = ui.Text("Name", tab, "Zincir", 30f, TextTone.Dark);
            label.rectTransform.Band(fromTop: false, 44f, offset: 8f);
            UiKit.ShrinkToFit(label, 20f);

            var button = tab.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.ColorTint;
            return tab.gameObject.AddComponent<ChainTabView>()
                .Wire("button", button)
                .Wire("icon", icon)
                .Wire("label", label)
                .Wire("chosenMark", chosen.gameObject);
        }

        /// <summary>The pack's lace beside a chain's name: a curl and a fading line, mirrored on the right.</summary>
        private void Flourish(RectTransform heading, bool mirrored)
        {
            var group = UiKit.Rect(mirrored ? "LaceRight" : "LaceLeft", heading);
            group.Prefer(width: 220f, height: 24f);
            var curl = ui.Image("Curl", group, ui.Sprites.LaceCurl, Color.white);
            curl.rectTransform.Pin(RectLayout.Left, new Vector2(44f, 22f));
            var line = ui.Image("Line", group, ui.Sprites.LaceLine, Color.white);
            line.preserveAspect = false;
            line.rectTransform.Pin(RectLayout.Right, new Vector2(170f, 5f));
            if (mirrored)
                group.localScale = new Vector3(-1f, 1f, 1f);
        }

        /// <summary>
        /// One level: the item on the pack's blue tile, its number on the green badge at the tile's top left, the small
        /// arrow on to the next level at the tile's right, and its name under the tile on up to two lines.
        /// </summary>
        private ChainLevelCellView CellTemplate(RectTransform templates)
        {
            var tileSize = LayoutMetrics.ChainTileSize;
            var cell = UiKit.Rect("Cell", templates).Pin(RectLayout.Center, new Vector2(tileSize.x, LayoutMetrics.ChainCellHeight));

            var tile = ui.Image("Tile", cell, ui.Sprites.ChainTile, Color.white);
            tile.rectTransform.Pin(RectLayout.Top, tileSize, new Vector2(0f, -16f));
            var arrow = ui.Image("Arrow", tile.rectTransform, ui.Sprites.ChainArrow, Color.white);
            arrow.rectTransform.Pin(RectLayout.Right, new Vector2(15f, 33f), new Vector2(LayoutMetrics.ChainCellSpacing / 2f, 0f), pivot: RectLayout.Center);
            var icon = ui.Image("Icon", tile.rectTransform, null, Color.white);
            icon.rectTransform.Pin(RectLayout.Center, new Vector2(tileSize.x + 4f, tileSize.x + 4f), new Vector2(0f, 4f));

            var badge = ui.Image("Badge", tile.rectTransform, ui.Sprites.LevelBadge, Color.white);
            // Pivoted on its centre, so the last level's larger badge grows evenly round the same spot.
            badge.rectTransform.Pin(RectLayout.TopLeft, new Vector2(56f, 66f), new Vector2(14f, -17f), pivot: RectLayout.Center);
            var level = ui.Text("Level", badge.rectTransform, "1", 36f, TextTone.Light);
            // Inside the badge's round head: a number keeps its full size, the last level's "MAX" shrinks to fit.
            level.rectTransform.Stretch(7f, 12f, 7f, 2f);
            UiKit.ShrinkToFit(level, 14f);

            // As wide as the tile, so neighbouring names never touch: a long name wraps onto a second line instead. Every
            // name is the same size (no shrinking to fit), so the rows read evenly.
            var name = ui.Text("Name", cell, "Eşya", 25f, TextTone.Dark, TextAlignmentOptions.Top);
            name.textWrappingMode = TextWrappingModes.Normal;
            name.lineSpacing = -12f;
            name.rectTransform.Pin(RectLayout.Bottom, new Vector2(tileSize.x, LayoutMetrics.ChainNameHeight));

            cell.Prefer(tileSize.x, LayoutMetrics.ChainCellHeight);
            return cell.gameObject.AddComponent<ChainLevelCellView>()
                .Wire("icon", icon)
                .Wire("levelLabel", level)
                .Wire("badge", badge.rectTransform)
                .Wire("nameLabel", name)
                .Wire("arrow", arrow.gameObject);
        }
    }
}
