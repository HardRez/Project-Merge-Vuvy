using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// The tintable UI shapes and icons the screens are made of. The 9-slice borders come from
    /// <c>Tools/PlaceholderArt/README.md</c> (left, bottom, right, top), in source pixels.
    /// </summary>
    internal sealed class UiSprites
    {
        private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>(StringComparer.Ordinal)
        {
            ["panel"] = new Vector4(40, 44, 40, 40),
            ["button"] = new Vector4(44, 54, 44, 40),
            ["pill"] = new Vector4(62, 68, 62, 60),
            ["cell"] = new Vector4(32, 32, 32, 32),
            ["shadow"] = new Vector4(50, 50, 50, 50),
            ["ring"] = new Vector4(34, 34, 34, 34),
            ["board_frame"] = new Vector4(40, 40, 40, 40),
            ["badge"] = new Vector4(24, 26, 24, 22),
            // The tray's rounded ends stay, its flat middle stretches; sliced across only, drawn at the art's height.
            ["order_tray"] = new Vector4(46, 0, 46, 0),
            // A glossy pillow-shaped button: likewise only its flat middle stretches across.
            ["green_button"] = new Vector4(50, 0, 50, 0),
            ["quit_button"] = new Vector4(60, 0, 60, 0),
            // Only the rows inside the ribbon's red band (40 to 100) stretch, to make the band taller for the name.
            ["title_ribbon"] = new Vector4(0, 130, 0, 40),
            // The settings popup's wooden frame and its cream panel.
            ["window_frame"] = new Vector4(70, 76, 70, 70),
            ["window_panel"] = new Vector4(60, 60, 60, 60),
            ["selection_brackets"] = new Vector4(60, 60, 60, 60),
            ["board_tray"] = new Vector4(30, 30, 30, 30),
            ["sand_01"] = new Vector4(64, 64, 64, 64),
            ["sand_02"] = new Vector4(64, 64, 64, 64),
            ["sand_03"] = new Vector4(64, 64, 64, 64)
        };

        private UiSprites()
        {
        }

        public Sprite Panel { get; private set; }
        public Sprite Button { get; private set; }
        public Sprite Pill { get; private set; }
        public Sprite Cell { get; private set; }
        public Sprite Circle { get; private set; }
        public Sprite Glow { get; private set; }
        public Sprite Shadow { get; private set; }
        public Sprite Ring { get; private set; }
        public Sprite BoardFrame { get; private set; }

        /// <summary>One repeat of a peach leaf wallpaper, drawn tiled behind the customers; from the asset pack.</summary>
        public Sprite ShopWall { get; private set; }

        /// <summary>A calm herringbone parquet, drawn tiled under the board; made from the asset pack's floor.</summary>
        public Sprite ShopFloor { get; private set; }

        /// <summary>The counter's wooden plank seen from the front, with its shadow; from the asset pack.</summary>
        public Sprite CounterTop { get; private set; }

        /// <summary>The teal cloth hanging down the counter's front, with a wavy hem; from the asset pack.</summary>
        public Sprite CounterCloth { get; private set; }

        /// <summary>The white tray a customer's requested items stand on; from the asset pack.</summary>
        public Sprite OrderTray { get; private set; }

        /// <summary>The glossy green button orders are delivered and the game is started with; from the asset pack.</summary>
        public Sprite GreenButton { get; private set; }

        /// <summary>The glossy orange pill the main menu's quit button is drawn with; from the asset pack.</summary>
        public Sprite QuitButton { get; private set; }

        /// <summary>A string of coloured flags hung across the top of the main menu; from the asset pack.</summary>
        public Sprite Bunting { get; private set; }

        /// <summary>The red ribbon with stars the game's name is written on; from the asset pack.</summary>
        public Sprite TitleRibbon { get; private set; }

        /// <summary>A white gear with a blue core, on the settings button; from the asset pack.</summary>
        public Sprite SettingsIcon { get; private set; }

        /// <summary>
        /// A popup window's wooden frame, its cream panel, the left half of its title tab (mirrored for the right half)
        /// and its round close button; from the asset pack.
        /// </summary>
        public Sprite WindowFrame { get; private set; }
        public Sprite WindowPanel { get; private set; }
        public Sprite WindowTitle { get; private set; }
        public Sprite WindowClose { get; private set; }

        /// <summary>
        /// A settings switch as the pack's settings window draws it: a small green square button, the white sound and
        /// vibration icons on it and the red slash laid across an icon while that setting is off; from the asset pack.
        /// </summary>
        public Sprite SwitchFace { get; private set; }
        public Sprite SoundIcon { get; private set; }
        public Sprite VibrationIcon { get; private set; }
        public Sprite SwitchOff { get; private set; }

        /// <summary>
        /// A fan of green bills with a V on the top one, beside the score and the orders' rewards; supplied with the
        /// project (not from the asset pack).
        /// </summary>
        public Sprite ScoreCash { get; private set; }

        /// <summary>Four corner brackets around the selected cell, from the asset pack; drawn in its own colours.</summary>
        public Sprite SelectionBrackets { get; private set; }

        /// <summary>The blue tray the board's tiles sit in, with its own rim and soft shadow; from the asset pack.</summary>
        public Sprite BoardTray { get; private set; }

        /// <summary>A gold-white four-point star with a soft glow, for sparkles; from the asset pack.</summary>
        public Sprite SparkleStar { get; private set; }

        /// <summary>A soft white ring that spreads out from a merge; from the asset pack.</summary>
        public Sprite MergeRing { get; private set; }

        /// <summary>A clear soap bubble around a locked item; from the asset pack.</summary>
        public Sprite LockBubble { get; private set; }

        /// <summary>A 4x4 sheet of a bubble's skin tearing apart, played as a flipbook when a bubble bursts; from the asset pack.</summary>
        public Sprite BubblePopSheet { get; private set; }

        /// <summary>A small glossy bubble, sprayed out when a bubble bursts; from the asset pack.</summary>
        public Sprite BubbleDroplet { get; private set; }

        /// <summary>The game's own round "produce" button, its tap hand removed; from the asset pack.</summary>
        public Sprite SpawnButton { get; private set; }

        /// <summary>The same round button in grey, shown while the board is full; made from the pack's button.</summary>
        public Sprite SpawnButtonDisabled { get; private set; }

        /// <summary>A whisk and a spatula, saying the spawn button makes something; from the asset pack.</summary>
        public Sprite SpawnIcon { get; private set; }

        /// <summary>The glossy blue square the home button is drawn with; from the asset pack.</summary>
        public Sprite HomeButton { get; private set; }

        /// <summary>Sand covering a closed cell, in several looks; drawn at twice the cell's resolution.</summary>
        public Sprite[] Sand { get; private set; }

        /// <summary>Unsliced shapes get no border; that includes the tiled shop wall and floor.</summary>
        public static Vector4 BorderOf(string shapeName) => Borders.TryGetValue(shapeName, out var border) ? border : Vector4.zero;

        /// <summary>The sand tiles are read at runtime: their pixels colour the grains of a pouring cover.</summary>
        public static bool IsReadAtRuntime(string shapeName) => shapeName.StartsWith("sand_", StringComparison.Ordinal);

        public static UiSprites Load() => new UiSprites
        {
            Panel = LoadShape("panel"),
            Button = LoadShape("button"),
            Pill = LoadShape("pill"),
            Cell = LoadShape("cell"),
            Circle = LoadShape("circle"),
            Glow = LoadShape("glow"),
            Shadow = LoadShape("shadow"),
            Ring = LoadShape("ring"),
            BoardFrame = LoadShape("board_frame"),
            ShopWall = LoadShape("shop_wall"),
            ShopFloor = LoadShape("shop_floor"),
            CounterTop = LoadShape("counter_top"),
            CounterCloth = LoadShape("counter_cloth"),
            OrderTray = LoadShape("order_tray"),
            GreenButton = LoadShape("green_button"),
            QuitButton = LoadShape("quit_button"),
            Bunting = LoadShape("bunting"),
            TitleRibbon = LoadShape("title_ribbon"),
            SettingsIcon = LoadShape("settings_icon"),
            WindowFrame = LoadShape("window_frame"),
            WindowPanel = LoadShape("window_panel"),
            WindowTitle = LoadShape("window_title"),
            WindowClose = LoadShape("window_close"),
            SwitchFace = LoadShape("switch_face"),
            SoundIcon = LoadShape("sound_icon"),
            VibrationIcon = LoadShape("vibration_icon"),
            SwitchOff = LoadShape("switch_off"),
            ScoreCash = LoadShape("score_cash"),
            SelectionBrackets = LoadShape("selection_brackets"),
            BoardTray = LoadShape("board_tray"),
            SparkleStar = LoadShape("sparkle_star"),
            MergeRing = LoadShape("merge_ring"),
            LockBubble = LoadShape("lock_bubble"),
            BubblePopSheet = LoadShape("bubble_pop_sheet"),
            BubbleDroplet = LoadShape("bubble_droplet"),
            SpawnButton = LoadShape("spawn_button"),
            SpawnButtonDisabled = LoadShape("spawn_button_disabled"),
            SpawnIcon = LoadShape("spawn_icon"),
            HomeButton = LoadShape("home_button"),
            Sand = new[] { LoadShape("sand_01"), LoadShape("sand_02"), LoadShape("sand_03") }
        };

        /// <param name="name">An icon file name without extension, e.g. <c>star</c> or <c>customer_01</c>.</param>
        public Sprite Icon(string name) => LoadSprite($"{BuildPaths.IconSpritesFolder}/{name}.png");

        private static Sprite LoadShape(string name) => LoadSprite($"{BuildPaths.UiSpritesFolder}/{name}.png");

        private static Sprite LoadSprite(string path) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new InvalidOperationException($"{path} is missing or does not import as a sprite.");
    }
}
