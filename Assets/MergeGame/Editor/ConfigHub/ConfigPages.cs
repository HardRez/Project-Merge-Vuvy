using System;
using System.Linq;
using MergeGame.Data.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MergeGame.Editor.ConfigHub
{
    /// <summary>The game config itself and the rule assets it points at: score table and orders.</summary>
    internal static class ConfigPages
    {
        public static VisualElement GameConfig(ConfigHubWindow hub)
        {
            var config = hub.Config;
            var page = HubUI.Page("Game Config",
                "One playable configuration: board size, the chains in play, scoring, orders and the opening layout. " +
                "The rest of the hub edits the assets this config points at.");

            page.Add(HubPages.AssetHeader(hub, config, picked =>
            {
                hub.SetConfig(picked);
                hub.Rebuild();
            }, () => HubAssets.Create<GameConfigAsset>(ContentSeeder.ConfigFolder, "GameConfig")));

            if (config == null)
            {
                page.Add(HubUI.EmptyState("The project has no game config."));
                return page;
            }

            var installed = HubAssets.InstalledConfig;
            var row = HubUI.Box("hub-assign");
            if (installed == config)
            {
                row.Add(HubUI.Pill("The game plays this config (installed by ProjectContext)", "ok"));
            }
            else
            {
                row.Add(HubUI.Pill(installed == null ? "ProjectContext installs no config" : $"The game plays {installed.name}, not this one", "warn"));
                row.Add(HubUI.Button("Make the game play it", () =>
                {
                    HubAssets.Install(config);
                    hub.Rebuild();
                }, "hub-button--primary"));
            }

            page.Add(row);

            var parts = HubUI.Card("Parts", "Open a part to edit it");
            var chips = HubUI.Box("hub-chips");
            foreach (var chain in config.Chains)
                chips.Add(PartChip(hub, chain, "Chain"));
            chips.Add(PartChip(hub, config.ScoreTable, "Score table"));
            chips.Add(PartChip(hub, config.Orders, "Orders"));
            chips.Add(PartChip(hub, config.InitialLayout, "Opening layout"));
            parts.Add(chips);
            page.Add(parts);

            page.Add(HubPages.Inspector(hub, config));
            page.Add(HubPages.References(hub, config));
            return page;
        }

        public static VisualElement ScoreTable(ConfigHubWindow hub)
        {
            var config = hub.Config;
            var table = hub.Pick(config != null ? config.ScoreTable : null);
            return HubPages.AssetPage(hub, "Score Table",
                "Points for creating an item by merging, by the level created; each chain's score multiplier is applied on top. " +
                "Must not decrease and must reach the longest chain's max level.",
                table,
                () => HubAssets.Create<ScoreTableAsset>(ContentSeeder.ConfigFolder, "ScoreTable"),
                asset => HubPages.Assignment(hub, asset, config != null ? config.ScoreTable : null, "scoreTable", "score table"),
                asset =>
                {
                    var longest = config != null && config.Chains.Any(chain => chain != null)
                        ? config.Chains.Where(chain => chain != null).Max(chain => chain.MaxLevel)
                        : 0;
                    return Chart("Points by created level",
                        longest > 0 ? $"The longest chain has {longest} levels" : null,
                        () => asset.PointsByLevel,
                        () => longest > asset.PointsByLevel.Count
                            ? $"The table stops at level {asset.PointsByLevel.Count} but a chain reaches level {longest}."
                            : null);
                });
        }

        public static VisualElement Orders(ConfigHubWindow hub)
        {
            var config = hub.Config;
            var orders = hub.Pick(config != null ? config.Orders : null);
            return HubPages.AssetPage(hub, "Orders & Customers",
                "How many customers wait at once, what they ask for, what they pay, and who they are. " +
                "Leave the config's Orders empty for a game without orders.",
                orders,
                () => HubAssets.Create<OrderSettingsAsset>(ContentSeeder.ConfigFolder, "OrderSettings"),
                asset => HubPages.Assignment(hub, asset, config != null ? config.Orders : null, "orders", "order settings"),
                asset =>
                {
                    var holder = HubUI.Box();
                    void Fill()
                    {
                        holder.Clear();
                        holder.Add(OrderSummary(asset));
                        holder.Add(Chart("Reward per delivered item", "By item level, before the chain multiplier",
                            () => asset.RewardPointsByLevel,
                            () => asset.MaxRequestedLevel > asset.RewardPointsByLevel.Count
                                ? $"Rewards stop at level {asset.RewardPointsByLevel.Count} but customers may ask for level {asset.MaxRequestedLevel}."
                                : null).view);
                        holder.Add(Customers(asset));
                    }

                    Fill();
                    return (holder, (Action)Fill);
                });
        }

        private static VisualElement PartChip(ConfigHubWindow hub, UnityEngine.Object asset, string role)
        {
            if (asset == null)
            {
                var missing = HubUI.Chip(null, null, $"{role}: not assigned");
                missing.AddToClassList("hub-chip--missing");
                return missing;
            }

            return HubUI.Chip(asset, () => hub.Reveal(asset), $"{role}: {asset.name}");
        }

        private static (VisualElement view, Action refresh) Chart(string title, string subtitle,
            Func<System.Collections.Generic.IReadOnlyList<int>> values, Func<string> warning)
        {
            var card = HubUI.Card(title, subtitle);
            var body = HubUI.Box();
            card.Add(body);
            void Fill()
            {
                body.Clear();
                body.Add(HubUI.Bars(values(), index => $"LV {index + 1}"));
                var problem = warning();
                if (problem != null)
                    body.Add(HubUI.Banner(problem, HubUI.WarningIcon));
            }

            Fill();
            return (card, (Action)Fill);
        }

        private static VisualElement OrderSummary(OrderSettingsAsset orders)
        {
            var card = HubUI.Card("At a glance");
            var stats = HubUI.Box("hub-stats");
            stats.Add(HubUI.Stat(orders.MaxActiveOrders.ToString(), "Orders at once"));
            stats.Add(HubUI.Stat(Range(orders.MinItemsPerOrder, orders.MaxItemsPerOrder), "Items per order"));
            stats.Add(HubUI.Stat(Range(orders.MinRequestedLevel, orders.MaxRequestedLevel), "Requested levels"));
            stats.Add(HubUI.Stat(orders.CustomerCount.ToString(), "Customers"));
            card.Add(stats);
            return card;
        }

        private static string Range(int min, int max) => min == max ? min.ToString() : $"{min}–{max}";

        private static VisualElement Customers(OrderSettingsAsset orders)
        {
            var card = HubUI.Card("Customers", "Spine portrait, or the still avatar when there is none");
            var strip = HubUI.Box("hub-customers");
            for (var i = 0; i < orders.Customers.Count; i++)
            {
                var customer = orders.Customers[i];
                var tile = HubUI.Box("hub-customer");
                if (customer?.Avatar != null)
                    tile.Add(HubUI.SpriteIcon(customer.Avatar, "hub-customer__image"));
                else if (customer?.Portrait != null)
                    // A mini thumbnail only: a full preview would make Spine build an inspector on an import worker.
                    tile.Add(HubUI.Icon(HubUI.Thumbnail(customer.Portrait), "hub-customer__image", "hub-customer__image--icon"));
                else
                    tile.Add(HubUI.Text("?", "hub-level__placeholder"));

                var name = customer?.Portrait != null ? customer.Portrait.name.Replace("_SkeletonData", "")
                    : customer?.Avatar != null ? customer.Avatar.name : "(empty)";
                tile.Add(HubUI.Text(name, "hub-customer__name"));
                tile.Add(HubUI.Text(customer?.Portrait != null ? "Spine" : customer?.Avatar != null ? "Sprite" : "Missing", "hub-customer__kind"));
                if (customer?.Portrait != null)
                {
                    var portrait = customer.Portrait;
                    tile.tooltip = AssetDatabase.GetAssetPath(portrait);
                    tile.RegisterCallback<ClickEvent>(_ => EditorGUIUtility.PingObject(portrait));
                }

                strip.Add(tile);
            }

            if (orders.Customers.Count == 0)
                strip.Add(HubUI.Text("No customers; orders need at least one.", "hub-muted"));
            card.Add(strip);
            return card;
        }
    }
}
