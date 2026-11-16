using System;
using System.Collections.Generic;
using System.Linq;
using MergeGame.Data.Authoring;
using MergeGame.Editor.Building;
using MergeGame.View.Audio;
using MergeGame.View.Notices;
using MergeGame.View.Theme;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MergeGame.Editor.ConfigHub
{
    /// <summary>One entry of the hub's side bar.</summary>
    internal sealed class HubPage
    {
        public HubPage(string id, string group, string title, Type iconType, Func<ConfigHubWindow, VisualElement> build,
            Func<ConfigHubWindow, string> badge = null)
        {
            Id = id;
            Group = group;
            Title = title;
            IconType = iconType;
            Build = build;
            Badge = badge;
        }

        public string Id { get; }
        public string Group { get; }
        public string Title { get; }
        public Type IconType { get; }
        public Func<ConfigHubWindow, VisualElement> Build { get; }
        public Func<ConfigHubWindow, string> Badge { get; }
    }

    /// <summary>The hub's pages and the parts they share: the asset header, the references card and the inspector card.</summary>
    internal static class HubPages
    {
        public const string Overview = "overview";
        public const string GameConfig = "config";
        public const string Chains = "chains";
        public const string ScoreTable = "score";
        public const string Orders = "orders";
        public const string BoardLayout = "layout";
        public const string Theme = "theme";
        public const string Sounds = "sounds";
        public const string Notices = "notices";
        public const string ProjectContext = "context";
        public const string SceneTuning = "scene";

        private const string GameGroup = "Game";
        private const string PresentationGroup = "Presentation";
        private const string ProjectGroup = "Project";

        public static readonly IReadOnlyList<HubPage> All = new[]
        {
            new HubPage(Overview, GameGroup, "Overview", typeof(SceneAsset), OverviewPage.Build,
                hub => hub.Problems.Count > 0 ? hub.Problems.Count.ToString() : null),
            new HubPage(GameConfig, GameGroup, "Game Config", typeof(ScriptableObject), ConfigPages.GameConfig),
            new HubPage(Chains, GameGroup, "Item Chains", typeof(Sprite), ChainsPage.Build,
                hub => hub.Config != null ? hub.Config.Chains.Count.ToString() : null),
            new HubPage(ScoreTable, GameGroup, "Score Table", typeof(TextAsset), ConfigPages.ScoreTable),
            new HubPage(Orders, GameGroup, "Orders & Customers", typeof(Animator), ConfigPages.Orders),
            new HubPage(BoardLayout, GameGroup, "Board Layout", typeof(Grid), BoardLayoutPage.Build),
            new HubPage(Theme, PresentationGroup, "UI Theme", typeof(Material), PresentationPages.Theme),
            new HubPage(Sounds, PresentationGroup, "Sounds", typeof(AudioClip), PresentationPages.Sounds),
            new HubPage(Notices, PresentationGroup, "Notice Texts", typeof(Font), PresentationPages.Notices),
            new HubPage(ProjectContext, ProjectGroup, "Project Context", typeof(GameObject), ProjectPages.ProjectContext),
            new HubPage(SceneTuning, ProjectGroup, "Game Scene Tuning", typeof(Camera), ProjectPages.SceneTuning)
        };

        /// <summary>The page that edits <paramref name="asset"/>, or null when the hub has none for it.</summary>
        public static string PageFor(Object asset)
        {
            switch (asset)
            {
                case GameConfigAsset _: return GameConfig;
                case ItemChainAsset _: return Chains;
                case ScoreTableAsset _: return ScoreTable;
                case OrderSettingsAsset _: return Orders;
                case BoardLayoutAsset _: return BoardLayout;
                case UiTheme _: return Theme;
                case SoundLibrary _: return Sounds;
                case NoticeTexts _: return Notices;
                case GameObject gameObject when AssetDatabase.GetAssetPath(gameObject) == BuildPaths.ProjectContextPrefab:
                    return ProjectContext;
                default: return null;
            }
        }

        // ---- Shared parts -----------------------------------------------------------------------------------------

        /// <summary>
        /// The asset a page edits, with a field to switch to another asset of the same type and the usual actions.
        /// </summary>
        public static VisualElement AssetHeader<T>(ConfigHubWindow hub, T asset, Action<T> onPick, Func<T> create = null)
            where T : Object
        {
            var card = HubUI.Card();
            card.AddToClassList("hub-asset-header");

            card.Add(HubUI.Icon(HubUI.Thumbnail(asset), "hub-asset-header__thumb"));

            var middle = HubUI.Box("hub-asset-header__middle");
            var field = new ObjectField { objectType = typeof(T), allowSceneObjects = false, value = asset };
            field.AddToClassList("hub-asset-header__field");
            field.RegisterValueChangedCallback(change => onPick(change.newValue as T));
            middle.Add(field);
            var count = HubAssets.FindAll<T>().Count;
            var path = asset != null ? AssetDatabase.GetAssetPath(asset) : "No asset selected";
            middle.Add(HubUI.Text($"{path}   ·   {count} {ObjectNames.NicifyVariableName(typeof(T).Name)}{(count == 1 ? "" : "s")} in the project",
                "hub-asset-header__path"));
            card.Add(middle);

            var actions = HubUI.Box("hub-asset-header__actions");
            if (asset != null)
            {
                actions.Add(HubUI.Button("Ping", () => EditorGUIUtility.PingObject(asset)));
                actions.Add(HubUI.Button("Inspector", () => EditorUtility.OpenPropertyEditor(asset)));
            }

            if (create != null)
                actions.Add(HubUI.Button("New", () =>
                {
                    var created = create();
                    if (created != null)
                        onPick(created);
                }));
            card.Add(actions);
            return card;
        }

        /// <summary>What <paramref name="asset"/> references and what references it; clicking a chip opens its page.</summary>
        public static VisualElement References(ConfigHubWindow hub, Object asset)
        {
            var card = HubUI.Card("References", "Direct links between assets, prefabs and scenes");
            var columns = HubUI.Box("hub-columns");
            columns.Add(ReferenceColumn(hub, "Uses", HubAssets.Uses(asset), "References no other asset."));
            columns.Add(ReferenceColumn(hub, "Used by", HubAssets.UsedBy(asset),
                "Nothing references this asset, so the game does not use it."));
            card.Add(columns);
            return card;
        }

        private static VisualElement ReferenceColumn(ConfigHubWindow hub, string title, List<Object> assets, string emptyText)
        {
            var column = HubUI.Box("hub-column");
            column.Add(HubUI.Text($"{title} ({assets.Count})", "hub-column__title"));
            if (assets.Count == 0)
            {
                column.Add(HubUI.Text(emptyText, "hub-muted"));
                return column;
            }

            var chips = HubUI.Box("hub-chips");
            foreach (var reference in assets)
                chips.Add(HubUI.Chip(reference, () => hub.Reveal(reference)));
            column.Add(chips);
            return column;
        }

        /// <summary>The asset's own inspector; <paramref name="onChanged"/> runs after every edit.</summary>
        public static VisualElement Inspector(ConfigHubWindow hub, Object asset, Action onChanged = null, string title = "Settings")
        {
            var card = HubUI.Card(title, "Hover a field for its tooltip");
            var serialized = new SerializedObject(asset);
            card.Add(new InspectorElement(serialized));
            card.TrackSerializedObjectValue(serialized, _ =>
            {
                HubAssets.Changed();
                onChanged?.Invoke();
                hub.RequestValidation();
            });
            return card;
        }

        /// <summary>Whether the edited game config uses <paramref name="asset"/> for <paramref name="field"/>, with a fix.</summary>
        public static VisualElement Assignment(ConfigHubWindow hub, Object asset, Object assigned, string field, string what)
        {
            var row = HubUI.Box("hub-assign");
            var config = hub.Config;
            if (config == null)
            {
                row.Add(HubUI.Pill("No game config in the project", "warn"));
                return row;
            }

            if (asset == assigned)
            {
                row.Add(HubUI.Pill($"Used by {config.name}", "ok"));
                return row;
            }

            row.Add(HubUI.Pill(assigned == null ? $"{config.name} has no {what}" : $"{config.name} uses {assigned.name} instead", "warn"));
            if (asset != null)
                row.Add(HubUI.Button($"Use in {config.name}", () =>
                {
                    HubAssets.SetReference(config, field, asset);
                    hub.RequestValidation();
                    hub.Rebuild();
                }, "hub-button--primary"));
            return row;
        }

        /// <summary>A whole page for one asset type: header, optional preview, references and inspector.</summary>
        public static VisualElement AssetPage<T>(ConfigHubWindow hub, string title, string description, T asset,
            Func<T> create, Func<T, VisualElement> assignment = null, Func<T, (VisualElement view, Action refresh)> preview = null)
            where T : Object
        {
            var page = HubUI.Page(title, description);
            page.Add(AssetHeader(hub, asset, hub.SetPick, create));
            if (asset == null)
            {
                page.Add(HubUI.EmptyState($"There is no {ObjectNames.NicifyVariableName(typeof(T).Name)} yet.",
                    create != null ? "Create one" : null, create != null ? () => hub.SetPick(create()) : (Action)null));
                return page;
            }

            if (assignment != null)
                page.Add(assignment(asset));

            Action refresh = null;
            if (preview != null)
            {
                var (view, refreshPreview) = preview(asset);
                page.Add(view);
                refresh = refreshPreview;
            }

            page.Add(Inspector(hub, asset, refresh));
            page.Add(References(hub, asset));
            return page;
        }
    }
}
