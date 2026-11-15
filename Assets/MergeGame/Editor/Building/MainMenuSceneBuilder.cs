using System;
using MergeGame.App.Installers;
using MergeGame.Data.Authoring;
using MergeGame.View.Common;
using MergeGame.View.MainMenu;
using MergeGame.View.Orders;
using Spine.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// Generates <c>MainMenu.unity</c>: the shop's front. Bunting across the top, the game's name on a ribbon, three
    /// customers waiting behind the counter with the best burger on a tray, and the Play and Quit buttons on the floor.
    /// </summary>
    internal sealed class MainMenuSceneBuilder
    {
        /// <summary>The customers behind the counter, left to right (folders under Content/Characters).</summary>
        private static readonly string[] Customers = { "Faye", "Theron", "Elowen" };

        /// <summary>Where the counter's top surface lies, as a share of the screen's height from its bottom.</summary>
        private const float CounterHeightShare = 0.385f;

        private const float CustomerSpacing = 320f;
        private const float CustomerStageWidth = 360f;

        /// <summary>The space above the counter the customers stand in; they are drawn 1.25 times as tall.</summary>
        private const float CustomerFrameHeight = 590f;

        private readonly UiKit ui;
        private readonly GameConfigAsset config;
        private readonly GamePrefabs prefabs;

        private MainMenuSceneBuilder(UiKit ui, GameConfigAsset config, GamePrefabs prefabs)
        {
            this.ui = ui;
            this.config = config;
            this.prefabs = prefabs;
        }

        public static void Build(string configPath)
        {
            var scene = SceneScaffold.NewScene();
            new MainMenuSceneBuilder(UiKit.Load(), AssetDatabase.LoadAssetAtPath<GameConfigAsset>(configPath), GamePrefabs.Load()).Populate();
            SceneScaffold.Save(scene, BuildPaths.MainMenuScene);
        }

        private void Populate()
        {
            var camera = SceneScaffold.Camera(ui.Theme.CellLight);
            SceneScaffold.EventSystem();
            var canvas = SceneScaffold.Canvas(camera);
            // The shop's parquet floor; the wallpaper comes down behind the counter.
            var content = SceneScaffold.SafeContent(ui, canvas, ui.Sprites.ShopFloor, LayoutMetrics.ShopFloorTileWidth);

            var plank = Shop(content);
            ShopScenery.Wall(ui, canvas, plank);
            Bunting(content);
            var title = Title(content);
            var play = GameButton(content, "PlayButton", "Play", ui.Sprites.GreenButton, new Vector2(600f, 180f), 0.18f, 104f);
            var quit = GameButton(content, "QuitButton", "Quit", ui.Sprites.QuitButton, new Vector2(340f, 110f), 0.075f, 56f);

            var view = content.gameObject.AddComponent<MainMenuView>()
                .Wire("playButton", play.Button)
                .Wire("quitButton", quit.Button)
                .Wire("title", title)
                .Wire("entranceItems", play.Rect.parent, quit.Rect.parent);

            var sounds = SceneScaffold.Sounds(UiDataAssets.LoadOrCreateSoundLibrary());
            SceneScaffold.ClickSound(play.Button, sounds);
            SceneScaffold.ClickSound(quit.Button, sounds);

            SceneScaffold.SceneContext<MainMenuInstaller>().Wire("mainMenuView", view);
        }

        /// <summary>A string of coloured flags across the top of the screen.</summary>
        private void Bunting(RectTransform content)
        {
            var art = ui.Sprites.Bunting;
            var bunting = ui.Image("Bunting", content, art, Color.white);
            bunting.preserveAspect = false;
            bunting.rectTransform.Band(fromTop: true, LayoutMetrics.ReferenceResolution.x * art.rect.height / art.rect.width, offset: -40f);
        }

        /// <summary>The game's name on the pack's red ribbon, and what the game is about on a cream tag under it.</summary>
        private RectTransform Title(RectTransform content)
        {
            var art = ui.Sprites.TitleRibbon;
            const float ribbonWidth = 1000f;
            var ribbonScale = ribbonWidth / art.rect.width;
            // Taller than the art by the stretched band rows, at the same scale, so the band holds the whole name.
            var ribbonHeight = (art.rect.height + LayoutMetrics.TitleRibbonExtraRows) * ribbonScale;
            const float tagHeight = 76f;

            // Pivoted on its centre, so it drops in evenly.
            var title = UiKit.Rect("Title", content)
                .Pin(RectLayout.Top, new Vector2(ribbonWidth, ribbonHeight + tagHeight), new Vector2(0f, -170f - (ribbonHeight + tagHeight) / 2f), pivot: RectLayout.Center);

            var ribbon = ui.Image("Ribbon", title, art, Color.white, cornerScale: ribbonScale);
            ribbon.rectTransform.Pin(RectLayout.Top, new Vector2(ribbonWidth, ribbonHeight));
            // The name is written along the ribbon's arched red band: centred on it in the middle and bent with it towards
            // the ends (see LayoutMetrics.TitleArcRadiusRows); the descenders of g and y dip into the gold stripe under it.
            var name = ui.Text("Name", ribbon.rectTransform, "Merge Vuvy", LayoutMetrics.TitleNameSize, TextTone.Title, TextAlignmentOptions.Baseline);
            name.rectTransform.Pin(RectLayout.Top, new Vector2(ribbonWidth - 240f, 0f), new Vector2(0f, -LayoutMetrics.TitleBaselineRow * ribbonScale), pivot: RectLayout.Center);
            name.color = Color.white;
            name.gameObject.AddComponent<ArcText>().Wire("radius", LayoutMetrics.TitleArcRadiusRows * ribbonScale);

            var tag = ui.Image("Tagline", title, ui.Sprites.Pill, ui.Theme.CellLight, cornerScale: tagHeight / 128f);
            tag.rectTransform.Pin(RectLayout.Bottom, new Vector2(600f, tagHeight), new Vector2(0f, 6f));
            ui.Image("Rim", tag.rectTransform, ui.Sprites.Ring, ui.Theme.WoodLight, cornerScale: tagHeight / 128f).rectTransform.Stretch();
            var tagline = ui.Text("Label", tag.rectTransform, "Merge • Spawn • Deliver", 40f, TextTone.Dark);
            tagline.rectTransform.Stretch(24f, 4f, 24f, 0f);
            UiKit.ShrinkToFit(tagline, 28f);
            return title;
        }

        /// <summary>
        /// The customers waiting behind the counter (the game's own customer prefab), the counter in front of them, and
        /// the best burger on a tray on it.
        /// </summary>
        /// <returns>The counter's plank, which the wall reaches down behind.</returns>
        private RectTransform Shop(RectTransform content)
        {
            var shop = UiKit.Rect("Shop", content);
            shop.anchorMin = new Vector2(0f, CounterHeightShare);
            shop.anchorMax = new Vector2(1f, CounterHeightShare);
            shop.pivot = new Vector2(0.5f, 0f);
            shop.sizeDelta = new Vector2(0f, LayoutMetrics.CounterTop + CustomerFrameHeight);
            shop.anchoredPosition = new Vector2(0f, -LayoutMetrics.CounterTop);

            var customers = new CustomerPortraitView[Customers.Length];
            var portraits = new SkeletonDataAsset[Customers.Length];
            for (var i = 0; i < Customers.Length; i++)
            {
                var customer = (CustomerPortraitView)PrefabUtility.InstantiatePrefab(prefabs.CustomerStage, shop);
                customer.name = Customers[i];
                ((RectTransform)customer.transform).Pin(RectLayout.Bottom,
                    new Vector2(CustomerStageWidth, LayoutMetrics.CounterTop + CustomerFrameHeight),
                    new Vector2((i - (Customers.Length - 1) / 2f) * CustomerSpacing, 0f));
                customers[i] = customer;
                portraits[i] = Portrait(Customers[i]);
            }

            var plank = ShopScenery.Counter(ui, shop);

            var tray = ui.Image("Tray", shop, ui.Sprites.OrderTray, Color.white, cornerScale: LayoutMetrics.TrayHeight / ui.Sprites.OrderTray.rect.height);
            tray.rectTransform.Pin(RectLayout.Bottom, new Vector2(300f, LayoutMetrics.TrayHeight), new Vector2(0f, LayoutMetrics.TrayBottom));
            // The last chain's best item: the burger the menu has always shown.
            var chain = config.Chains[config.Chains.Count - 1];
            ui.Image("Best", shop, chain.Levels[chain.Levels.Count - 1].Icon, Color.white).rectTransform
                .Pin(RectLayout.Bottom, new Vector2(220f, 220f), new Vector2(0f, LayoutMetrics.TrayBottom - 8f));

            shop.gameObject.AddComponent<MenuCustomers>()
                .Wire("customers", customers)
                .Wire("portraits", portraits);
            return plank;
        }

        private static SkeletonDataAsset Portrait(string character)
        {
            var path = $"{ContentSeeder.CharactersFolder}/{character}/{character}_SkeletonData.asset";
            return AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path) ?? throw new InvalidOperationException($"{path} is missing.");
        }

        /// <summary>One of the pack's glossy buttons, sliced across only and drawn at its art's proportions, with its label.</summary>
        private ButtonParts GameButton(RectTransform content, string name, string label, Sprite face, Vector2 size, float heightShare, float fontSize)
        {
            // The holder pops in; the button presses inside it.
            var holder = UiKit.Rect(name + "Holder", content).Span(new Vector2(0.5f, heightShare), new Vector2(0.5f, heightShare));
            holder.sizeDelta = size;

            var button = ui.ArtButton(name, holder, face, contentLift: size.y * 0.1f, height: size.y);
            button.Rect.Stretch();
            var text = ui.Text("Label", button.Content, label, fontSize, TextTone.Light);
            text.rectTransform.Stretch(24f, 0f, 24f, 0f);
            UiKit.ShrinkToFit(text, fontSize * 0.6f);
            return button;
        }
    }
}
