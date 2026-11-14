using System.Collections.Generic;
using System.Linq;
using MergeGame.App.Installers;
using MergeGame.Data.Authoring;
using MergeGame.View.Common;
using MergeGame.View.MainMenu;
using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.Editor.Building
{
    /// <summary>Generates <c>MainMenu.unity</c>: the title with floating items around it, and the Play and Quit buttons.</summary>
    internal sealed class MainMenuSceneBuilder
    {
        // Normalised positions (in the safe area) and sizes of the floating items around the hero.
        private static readonly (Vector2 anchor, float size)[] DecorationSpots =
        {
            (new Vector2(0.14f, 0.93f), 150f),
            (new Vector2(0.86f, 0.94f), 140f),
            (new Vector2(0.11f, 0.61f), 150f),
            (new Vector2(0.89f, 0.63f), 160f),
            (new Vector2(0.84f, 0.41f), 120f),
            (new Vector2(0.16f, 0.43f), 120f)
        };

        private readonly UiKit ui;
        private readonly GameConfigAsset config;

        private MainMenuSceneBuilder(UiKit ui, GameConfigAsset config)
        {
            this.ui = ui;
            this.config = config;
        }

        public static void Build(string configPath)
        {
            var scene = SceneScaffold.NewScene();
            new MainMenuSceneBuilder(UiKit.Load(), AssetDatabase.LoadAssetAtPath<GameConfigAsset>(configPath)).Populate();
            SceneScaffold.Save(scene, BuildPaths.MainMenuScene);
        }

        private void Populate()
        {
            var camera = SceneScaffold.Camera(ui.Theme.CellLight);
            SceneScaffold.EventSystem();
            var canvas = SceneScaffold.Canvas(camera);
            var content = SceneScaffold.SafeContent(ui, canvas);

            Decorations(content);
            Hero(content);
            var title = Title(content);
            var play = BigButton(content, "PlayButton", "Oyna", "play", ui.Theme.Primary, new Vector2(640f, 196f), 0.27f);
            var quit = BigButton(content, "QuitButton", "Çıkış", null, ui.Theme.Danger, new Vector2(420f, 132f), 0.15f);

            var view = content.gameObject.AddComponent<MainMenuView>()
                .Wire("playButton", play.Button)
                .Wire("quitButton", quit.Button)
                .Wire("title", title)
                .Wire("entranceItems", play.Rect.parent, quit.Rect.parent);

            SceneScaffold.SceneContext<MainMenuInstaller>().Wire("mainMenuView", view);
        }

        private RectTransform Title(RectTransform content)
        {
            var title = UiKit.Rect("Title", content).Span(new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f));
            title.sizeDelta = new Vector2(1000f, 330f);
            var name = ui.Text("Name", title, "Merge Vuvy", 180f, TextTone.Title);
            name.rectTransform.Band(fromTop: true, height: 230f);
            name.enableVertexGradient = true;
            name.colorGradient = new VertexGradient(Color.white, Color.white, ui.Theme.Gold, ui.Theme.Gold);
            UiKit.ShrinkToFit(name, 110f);
            var tagline = ui.Text("Tagline", title, "Birleştir • Üret • Teslim Et", 50f, TextTone.Light);
            tagline.rectTransform.Band(fromTop: false, height: 80f);
            return title;
        }

        /// <summary>The chain's top item glowing in the middle of the screen: what the player works towards.</summary>
        private void Hero(RectTransform content)
        {
            var hero = UiKit.Rect("Hero", content).Span(new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f));
            hero.sizeDelta = new Vector2(420f, 420f);
            var glow = ui.Image("Glow", hero, ui.Sprites.Glow, ui.Theme.Gold);
            glow.rectTransform.Inset(-140f);
            var chain = config.Chains[config.Chains.Count - 1];
            var item = ui.Image("Item", hero, chain.Levels[chain.Levels.Count - 1].Icon, Color.white);
            item.rectTransform.Stretch();
            ui.Icon("Sparkles", hero, "sparkles").rectTransform.Pin(RectLayout.TopRight, new Vector2(150f, 150f), new Vector2(40f, 30f));
            hero.gameObject.AddComponent<IdleBob>().Wire("amplitude", 10f).Wire("swayDegrees", 2f);
        }

        private void Decorations(RectTransform content)
        {
            var icons = DecorationIcons().ToList();
            for (var i = 0; i < DecorationSpots.Length && i < icons.Count; i++)
                Decoration(content, icons[i], DecorationSpots[i].anchor, DecorationSpots[i].size, phase: i / (float)DecorationSpots.Length);
        }

        private void Decoration(RectTransform parent, Sprite icon, Vector2 anchor, float size, float phase)
        {
            var image = ui.Image(icon.name, parent, icon, Color.white);
            image.rectTransform.Pin(anchor, new Vector2(size, size), pivot: RectLayout.Center);
            image.gameObject.AddComponent<IdleBob>().Wire("phase", phase);
        }

        /// <summary>One high-level item of each chain, so the menu previews what the game is about.</summary>
        private IEnumerable<Sprite> DecorationIcons()
        {
            var levelPicks = new[] { 4, 5, 3, 6, 2 };
            for (var i = 0; i < config.Chains.Count * 2; i++)
            {
                var chain = config.Chains[i % config.Chains.Count];
                var level = Mathf.Min(levelPicks[i % levelPicks.Length], chain.Levels.Count);
                yield return chain.Levels[level - 1].Icon;
            }
        }

        private ButtonParts BigButton(RectTransform content, string name, string label, string iconName, Color color, Vector2 size, float heightFraction)
        {
            var holder = UiKit.Rect(name + "Holder", content).Span(new Vector2(0.5f, heightFraction), new Vector2(0.5f, heightFraction));
            holder.sizeDelta = size;

            var button = ui.Button(name, holder, color);
            button.Rect.Stretch();
            var layout = button.Content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.padding = new RectOffset(0, 0, 22, 22);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            if (iconName != null)
                ui.Icon("Icon", button.Content, iconName).Prefer(width: size.y * 0.55f);
            ui.Text("Label", button.Content, label, size.y * 0.45f, TextTone.Light);
            return button;
        }
    }
}
