using System.Collections.Generic;
using System.Linq;
using MergeGame.Data.Authoring;
using MergeGame.Editor.Building;
using MergeGame.View.Audio;
using MergeGame.View.Notices;
using MergeGame.View.Theme;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MergeGame.Editor.ConfigHub
{
    /// <summary>The landing page: the config in a few numbers, its problems, how the assets link up, and every asset.</summary>
    internal static class OverviewPage
    {
        public static VisualElement Build(ConfigHubWindow hub)
        {
            var page = HubUI.Page("Overview",
                "Everything the game reads from ScriptableObjects. Pick a section on the left to edit it, or click any asset below.");
            page.Add(HubUI.Banner("Merge Game > Seed Content rewrites the Content/Config assets from seed_content.json and deletes chains " +
                                  "the seed does not list. Edits made here stay unless you run it again."));

            var config = hub.Config;
            page.Add(ActiveConfig(hub, config));

            var validation = HubUI.Card("Validation", "Same checks as the game config's Validate menu");
            var problems = HubUI.Box();
            validation.Add(problems);
            void FillProblems()
            {
                problems.Clear();
                problems.Add(HubUI.Problems(hub.Problems));
            }

            FillProblems();
            hub.OnValidated(FillProblems);
            page.Add(validation);

            page.Add(ReferenceMap(hub, config));
            page.Add(AllAssets(hub));
            return page;
        }

        private static VisualElement ActiveConfig(ConfigHubWindow hub, GameConfigAsset config)
        {
            var installed = HubAssets.InstalledConfig;
            var card = HubUI.Card("Game config",
                config == null ? null : config == installed ? "Installed by the ProjectContext prefab: this is what the game plays"
                    : "Not the config the ProjectContext installs");
            if (config == null)
            {
                card.Add(HubUI.EmptyState("The project has no game config.", "Create one", () =>
                {
                    hub.SetConfig(HubAssets.Create<GameConfigAsset>(ContentSeeder.ConfigFolder, "GameConfig"));
                    hub.Rebuild();
                }));
                return card;
            }

            var row = HubUI.Box("hub-assign");
            row.Add(HubUI.Chip(config, () => hub.ShowPage(HubPages.GameConfig)));
            if (config != installed)
                row.Add(HubUI.Button("Make the game play it", () =>
                {
                    HubAssets.Install(config);
                    hub.Rebuild();
                }, "hub-button--primary"));
            card.Add(row);

            var chains = config.Chains.Where(chain => chain != null).ToList();
            var stats = HubUI.Box("hub-stats");
            stats.Add(HubUI.Stat($"{config.BoardWidth} × {config.BoardHeight}", "Board"));
            stats.Add(HubUI.Stat(chains.Count.ToString(), "Item chains"));
            stats.Add(HubUI.Stat(chains.Sum(chain => chain.MaxLevel).ToString(), "Item levels"));
            stats.Add(HubUI.Stat(config.Orders != null ? config.Orders.CustomerCount.ToString() : "Off", "Customers"));
            stats.Add(HubUI.Stat(config.InitialLayout != null ? config.InitialLayout.Placements.Count.ToString() : "0", "Opening items"));
            card.Add(stats);
            return card;
        }

        private static VisualElement ReferenceMap(ConfigHubWindow hub, GameConfigAsset config)
        {
            var card = HubUI.Card("Reference map", "Which asset feeds which; click a chip to open it");
            var tree = HubUI.Box("hub-tree");

            var context = HubAssets.ProjectContextPrefab;
            tree.Add(TreeRow(hub, 0, "Project context", context != null ? new Object[] { context } : new Object[0]));
            if (config != null)
            {
                tree.Add(TreeRow(hub, 1, "Game config", new Object[] { config }));
                tree.Add(TreeRow(hub, 2, "Chains", config.Chains.Cast<Object>().ToArray()));
                tree.Add(TreeRow(hub, 2, "Score table", new Object[] { config.ScoreTable }));
                tree.Add(TreeRow(hub, 2, "Orders", new Object[] { config.Orders }));
                tree.Add(TreeRow(hub, 2, "Opening layout", new Object[] { config.InitialLayout }));
            }

            foreach (var scenePath in new[] { BuildPaths.MainMenuScene, BuildPaths.GameScene })
            {
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (scene == null)
                    continue;
                tree.Add(TreeRow(hub, 0, "Scene", new Object[] { scene }));
                var dataAssets = HubAssets.Uses(scene).Where(asset => asset is ScriptableObject && HubPages.PageFor(asset) != null).ToArray();
                tree.Add(TreeRow(hub, 1, "Presentation data", dataAssets));
            }

            card.Add(tree);
            return card;
        }

        private static VisualElement TreeRow(ConfigHubWindow hub, int depth, string label, IReadOnlyList<Object> assets)
        {
            var row = HubUI.Box("hub-tree__row");
            row.style.marginLeft = depth * 22;
            row.Add(HubUI.Text(depth > 0 ? "└" : "●", "hub-tree__glyph"));
            row.Add(HubUI.Text(label, "hub-tree__label"));
            var chips = HubUI.Box("hub-chips");
            if (assets.Count == 0)
                chips.Add(HubUI.Text("none", "hub-muted"));
            foreach (var asset in assets)
            {
                if (asset == null)
                {
                    var missing = HubUI.Chip(null, null, "Not assigned");
                    missing.AddToClassList("hub-chip--missing");
                    chips.Add(missing);
                    continue;
                }

                chips.Add(HubUI.Chip(asset, () => hub.Reveal(asset)));
            }

            row.Add(chips);
            return row;
        }

        private static VisualElement AllAssets(ConfigHubWindow hub)
        {
            var assets = new List<Object>();
            assets.AddRange(HubAssets.FindAll<GameConfigAsset>());
            assets.AddRange(HubAssets.FindAll<ItemChainAsset>());
            assets.AddRange(HubAssets.FindAll<ScoreTableAsset>());
            assets.AddRange(HubAssets.FindAll<OrderSettingsAsset>());
            assets.AddRange(HubAssets.FindAll<BoardLayoutAsset>());
            assets.AddRange(HubAssets.FindAll<UiTheme>());
            assets.AddRange(HubAssets.FindAll<SoundLibrary>());
            assets.AddRange(HubAssets.FindAll<NoticeTexts>());

            var card = HubUI.Card("Scriptable assets", $"{assets.Count} in the project");
            var tiles = HubUI.Box("hub-tiles");
            foreach (var asset in assets)
            {
                var tile = HubUI.Box("hub-tile");
                tile.tooltip = AssetDatabase.GetAssetPath(asset);
                tile.Add(HubUI.Icon(HubUI.Thumbnail(asset), "hub-tile__icon"));
                var text = HubUI.Box("hub-tile__text");
                text.Add(HubUI.Text(ObjectNames.NicifyVariableName(asset.GetType().Name).Replace(" Asset", ""), "hub-tile__type"));
                text.Add(HubUI.Text(asset.name, "hub-tile__name"));
                var users = HubAssets.UsedBy(asset).Count;
                var usage = HubUI.Text(users == 0 ? "Unused" : $"Used by {users}", "hub-tile__usage");
                if (users == 0)
                    usage.AddToClassList("hub-tile__usage--unused");
                text.Add(usage);
                tile.Add(text);
                tile.RegisterCallback<ClickEvent>(_ => hub.Reveal(asset));
                tiles.Add(tile);
            }

            card.Add(tiles);
            return card;
        }
    }
}
