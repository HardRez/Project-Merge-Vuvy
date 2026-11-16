using System.Collections.Generic;
using System.Linq;
using MergeGame.Data.Authoring;
using MergeGame.Editor.Building;
using MergeGame.View.Theme;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MergeGame.Editor.ConfigHub
{
    /// <summary>The opening board drawn as the player first sees it, next to the placement list that defines it.</summary>
    internal static class BoardLayoutPage
    {
        private const float BoardPixels = 380f;

        public static VisualElement Build(ConfigHubWindow hub)
        {
            var config = hub.Config;
            var layout = hub.Pick(config != null ? config.InitialLayout : null);
            return HubPages.AssetPage(hub, "Board Layout",
                "The items on the board when a new game starts. Webbed items are locked until a matching item is merged onto them; " +
                "sand-covered cells open when a webbed neighbour is freed.",
                layout,
                () => HubAssets.Create<BoardLayoutAsset>(ContentSeeder.ConfigFolder, "BoardLayout"),
                asset => HubPages.Assignment(hub, asset, config != null ? config.InitialLayout : null, "initialLayout", "opening layout"),
                asset =>
                {
                    var card = HubUI.Card("Preview", config != null ? $"{config.BoardWidth} × {config.BoardHeight} board of {config.name}; hover a cell for details" : null);
                    var body = HubUI.Box();
                    card.Add(body);
                    void Fill()
                    {
                        body.Clear();
                        body.Add(Preview(config, asset));
                    }

                    Fill();
                    return (card, (System.Action)Fill);
                });
        }

        private static VisualElement Preview(GameConfigAsset config, BoardLayoutAsset layout)
        {
            var root = HubUI.Box("hub-board-preview");
            var width = config != null ? Mathf.Max(1, config.BoardWidth) : 5;
            var height = config != null ? Mathf.Max(1, config.BoardHeight) : 5;
            var cellSize = Mathf.Floor(Mathf.Min(64f, BoardPixels / Mathf.Max(width, height)));
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(BuildPaths.Theme);
            var light = theme != null ? theme.BoardTileLight : new Color(0.56f, 0.72f, 0.95f);
            var dark = theme != null ? theme.BoardTileDark : new Color(0.53f, 0.70f, 0.93f);

            var byCell = new Dictionary<Vector2Int, InitialPlacementEntry>();
            var outside = new List<InitialPlacementEntry>();
            foreach (var entry in layout.Placements.Where(entry => entry != null))
            {
                var cell = new Vector2Int(entry.X, entry.Y);
                if (entry.X < 0 || entry.Y < 0 || entry.X >= width || entry.Y >= height)
                    outside.Add(entry);
                else if (!byCell.ContainsKey(cell))
                    byCell.Add(cell, entry);
            }

            var board = HubUI.Box("hub-board");
            for (var y = 0; y < height; y++)
            {
                var row = HubUI.Box("hub-board__row");
                for (var x = 0; x < width; x++)
                {
                    byCell.TryGetValue(new Vector2Int(x, y), out var entry);
                    row.Add(Cell(x, y, cellSize, (x + y) % 2 == 0 ? light : dark, entry));
                }

                board.Add(row);
            }

            root.Add(board);

            var side = HubUI.Box("hub-board-side");
            var stats = HubUI.Box("hub-stats");
            stats.Add(HubUI.Stat(byCell.Values.Count(entry => !entry.Locked).ToString(), "Free"));
            stats.Add(HubUI.Stat(byCell.Values.Count(entry => entry.Locked && !entry.Hidden).ToString(), "Webbed"));
            stats.Add(HubUI.Stat(byCell.Values.Count(entry => entry.Hidden).ToString(), "Under sand"));
            stats.Add(HubUI.Stat((width * height - byCell.Count).ToString(), "Empty cells"));
            side.Add(stats);

            var legend = HubUI.Box("hub-legend");
            legend.Add(LegendItem("hub-legend__swatch--webbed", "Webbed (locked)"));
            legend.Add(LegendItem("hub-legend__swatch--sand", "Under sand (hidden)"));
            side.Add(legend);
            side.Add(HubUI.Text("Row 0 is the top row; x counts columns from the left.", "hub-muted"));

            foreach (var entry in outside)
                side.Add(HubUI.Banner($"({entry.X}, {entry.Y}) {Describe(entry)} lies outside the board and is not drawn.", HubUI.WarningIcon));
            root.Add(side);
            return root;
        }

        private static VisualElement Cell(int x, int y, float size, Color tile, InitialPlacementEntry entry)
        {
            var cell = HubUI.Box("hub-cell");
            cell.style.width = size;
            cell.style.height = size;
            cell.style.backgroundColor = tile;
            cell.tooltip = entry == null ? $"({x}, {y}) empty" : $"({x}, {y}) {Describe(entry)}";

            var sprite = entry?.Chain != null && entry.Level >= 1 && entry.Level <= entry.Chain.MaxLevel
                ? entry.Chain.Levels[entry.Level - 1]?.Icon
                : null;
            if (entry != null && sprite != null)
                cell.Add(HubUI.SpriteIcon(sprite, "hub-cell__item"));
            else if (entry != null)
                cell.Add(HubUI.Text("!", "hub-cell__broken"));

            if (entry != null && entry.Locked && !entry.Hidden)
            {
                var bubble = HubUI.Box("hub-cell__bubble");
                var radius = size * 0.45f;
                bubble.style.borderTopLeftRadius = radius;
                bubble.style.borderTopRightRadius = radius;
                bubble.style.borderBottomLeftRadius = radius;
                bubble.style.borderBottomRightRadius = radius;
                cell.Add(bubble);
            }

            if (entry != null && entry.Hidden)
            {
                var sand = HubUI.Text("?", "hub-cell__sand");
                sand.style.fontSize = Mathf.Max(10f, size * 0.35f);
                cell.Add(sand);
            }

            if (entry != null && entry.Level > 0 && !entry.Hidden)
                cell.Add(HubUI.Text(entry.Level.ToString(), "hub-cell__level"));
            cell.Add(HubUI.Text($"{x},{y}", "hub-cell__coord"));
            return cell;
        }

        private static VisualElement LegendItem(string swatchClass, string text)
        {
            var item = HubUI.Box("hub-legend__item");
            item.Add(HubUI.Box("hub-legend__swatch", swatchClass));
            item.Add(HubUI.Text(text, "hub-legend__text"));
            return item;
        }

        private static string Describe(InitialPlacementEntry entry)
        {
            var chain = entry.Chain == null ? "no chain"
                : string.IsNullOrWhiteSpace(entry.Chain.DisplayName) ? entry.Chain.name : entry.Chain.DisplayName;
            var levelName = entry.Chain != null && entry.Level >= 1 && entry.Level <= entry.Chain.MaxLevel
                ? entry.Chain.Levels[entry.Level - 1]?.DisplayName
                : null;
            var state = entry.Hidden ? "under sand" : entry.Locked ? "webbed" : "free";
            return $"{chain} level {entry.Level}{(string.IsNullOrEmpty(levelName) ? "" : $" ({levelName})")}, {state}";
        }
    }
}
