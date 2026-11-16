using System;
using System.Linq;
using MergeGame.Data.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MergeGame.Editor.ConfigHub
{
    /// <summary>Every merge chain: the config's list (order, spawn share), each chain's levels as the player sees them, and its settings.</summary>
    internal static class ChainsPage
    {
        public static VisualElement Build(ConfigHubWindow hub)
        {
            var page = HubUI.Page("Item Chains",
                "A chain is one ItemChainAsset listed in the game config; adding a chain or a level needs no code. " +
                "The list order is the spawn roll order and the order of the chains window in the game.");

            var config = hub.Config;
            var listed = config != null ? config.Chains.Where(chain => chain != null).ToList() : new System.Collections.Generic.List<ItemChainAsset>();
            var all = HubAssets.FindAll<ItemChainAsset>();
            var chain = hub.Pick(listed.FirstOrDefault());

            var split = HubUI.Box("hub-split");
            var list = HubUI.Box("hub-split__list");
            var detail = HubUI.Box("hub-split__detail");
            split.Add(list);
            split.Add(detail);
            page.Add(split);

            void FillList()
            {
                list.Clear();
                var listCard = HubUI.Card($"In {(config != null ? config.name : "the config")}", $"{listed.Count} chains");
                var totalWeight = listed.Sum(item => Math.Max(0, item.SpawnWeight));
                foreach (var item in listed)
                    listCard.Add(ListItem(hub, item, item == chain, totalWeight));
                if (listed.Count == 0)
                    listCard.Add(HubUI.Text("No chains listed yet.", "hub-muted"));
                list.Add(listCard);

                var orphans = all.Where(item => !listed.Contains(item)).ToList();
                if (orphans.Count > 0)
                {
                    var orphanCard = HubUI.Card("Not in the config", "The game never uses these");
                    foreach (var item in orphans)
                        orphanCard.Add(ListItem(hub, item, item == chain, 0));
                    list.Add(orphanCard);
                }

                list.Add(HubUI.Button("+ New chain", () =>
                {
                    var created = HubAssets.CreateChain();
                    if (config != null)
                        HubAssets.AddChain(config, created);
                    hub.SetPick(created);
                }, "hub-button--primary", "hub-button--wide"));
            }

            FillList();

            if (chain == null)
            {
                detail.Add(HubUI.EmptyState("There are no item chains yet."));
                return page;
            }

            detail.Add(HubPages.AssetHeader(hub, chain, hub.SetPick, HubAssets.CreateChain));
            detail.Add(Membership(hub, config, chain));

            var levelsCard = HubUI.Card("Levels", "Merging two of a level makes the next; the last is the max level");
            var levels = HubUI.Box();
            levelsCard.Add(levels);
            void FillLevels()
            {
                levels.Clear();
                levels.Add(LevelStrip(chain, config != null ? config.ScoreTable : null));
            }

            FillLevels();
            detail.Add(levelsCard);
            detail.Add(HubPages.Inspector(hub, chain, () =>
            {
                FillLevels();
                FillList();
            }));
            detail.Add(HubPages.References(hub, chain));
            return page;
        }

        private static VisualElement ListItem(ConfigHubWindow hub, ItemChainAsset chain, bool active, int totalWeight)
        {
            var item = HubUI.Box("hub-list-item");
            if (active)
                item.AddToClassList("hub-list-item--active");

            var firstIcon = chain.Levels.FirstOrDefault(level => level != null && level.Icon != null)?.Icon;
            item.Add(firstIcon != null ? (VisualElement)HubUI.SpriteIcon(firstIcon, "hub-list-item__icon") : HubUI.Icon(HubUI.Thumbnail(chain), "hub-list-item__icon"));

            var text = HubUI.Box("hub-list-item__text");
            text.Add(HubUI.Text(string.IsNullOrWhiteSpace(chain.DisplayName) ? chain.name : chain.DisplayName, "hub-list-item__title"));
            var share = totalWeight > 0 ? (float)Math.Max(0, chain.SpawnWeight) / totalWeight : 0f;
            var sub = $"{chain.Id} · {chain.MaxLevel} levels";
            if (totalWeight > 0)
                sub += $" · {share:P0} spawns";
            text.Add(HubUI.Text(sub, "hub-list-item__sub"));
            if (totalWeight > 0)
            {
                var meter = HubUI.Box("hub-meter");
                var fill = HubUI.Box("hub-meter__fill");
                fill.style.width = Length.Percent(share * 100f);
                meter.Add(fill);
                text.Add(meter);
            }

            item.Add(text);
            item.RegisterCallback<ClickEvent>(_ => hub.SetPick(chain));
            return item;
        }

        private static VisualElement Membership(ConfigHubWindow hub, GameConfigAsset config, ItemChainAsset chain)
        {
            var row = HubUI.Box("hub-assign");
            if (config == null)
            {
                row.Add(HubUI.Pill("No game config in the project", "warn"));
                return row;
            }

            var index = config.Chains.ToList().IndexOf(chain);
            if (index < 0)
            {
                row.Add(HubUI.Pill($"Not listed in {config.name}", "warn"));
                row.Add(HubUI.Button($"Add to {config.name}", () =>
                {
                    HubAssets.AddChain(config, chain);
                    hub.RequestValidation();
                    hub.Rebuild();
                }, "hub-button--primary"));
                return row;
            }

            row.Add(HubUI.Pill($"Chain {index + 1} of {config.Chains.Count} in {config.name}", "ok"));
            var up = HubUI.Button("Move up", () => Move(hub, config, index, index - 1));
            up.SetEnabled(index > 0);
            row.Add(up);
            var down = HubUI.Button("Move down", () => Move(hub, config, index, index + 1));
            down.SetEnabled(index < config.Chains.Count - 1);
            row.Add(down);
            row.Add(HubUI.Button("Remove from config", () =>
            {
                if (!EditorUtility.DisplayDialog("Remove chain",
                        $"Remove '{chain.name}' from {config.name}? The asset stays in the project; opening items that use it will be reported.",
                        "Remove", "Cancel"))
                    return;
                HubAssets.RemoveChainAt(config, index);
                hub.RequestValidation();
                hub.Rebuild();
            }, "hub-button--danger"));
            return row;
        }

        private static void Move(ConfigHubWindow hub, GameConfigAsset config, int from, int to)
        {
            HubAssets.MoveChain(config, from, to);
            hub.Rebuild();
        }

        private static VisualElement LevelStrip(ItemChainAsset chain, ScoreTableAsset scoreTable)
        {
            var strip = HubUI.Box("hub-levels");
            if (chain.Levels.Count == 0)
            {
                strip.Add(HubUI.Text("This chain has no levels; add some under Settings.", "hub-muted"));
                return strip;
            }

            for (var i = 0; i < chain.Levels.Count; i++)
            {
                var level = chain.Levels[i];
                var tile = HubUI.Box("hub-level");
                if (level == null || level.Icon == null)
                {
                    tile.AddToClassList("hub-level--missing");
                    tile.Add(HubUI.Text("?", "hub-level__placeholder"));
                }
                else
                {
                    tile.Add(HubUI.SpriteIcon(level.Icon, "hub-level__icon"));
                }

                tile.Add(HubUI.Text($"LV {i + 1}{(i == chain.Levels.Count - 1 ? " · MAX" : "")}", "hub-level__number"));
                tile.Add(HubUI.Text(level != null && !string.IsNullOrWhiteSpace(level.DisplayName) ? level.DisplayName : "(no name)", "hub-level__name"));

                // Merges create levels 2 and up; same rounding as Core's ChainPoints.
                if (i > 0 && scoreTable != null && i < scoreTable.PointsByLevel.Count)
                {
                    var points = (int)Math.Round(scoreTable.PointsByLevel[i] * (double)chain.ScoreMultiplier, MidpointRounding.AwayFromZero);
                    tile.Add(HubUI.Text($"+{points} pts", "hub-level__points"));
                }

                strip.Add(tile);
            }

            return strip;
        }
    }
}
