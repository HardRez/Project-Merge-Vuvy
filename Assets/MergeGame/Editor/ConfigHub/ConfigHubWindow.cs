using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MergeGame.Data.Authoring;
using MergeGame.Data.Validation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MergeGame.Editor.ConfigHub
{
    /// <summary>
    /// One window for every ScriptableObject the game is configured with: edit them in place, see what references
    /// what, and check that the config is playable. Edits go straight to the assets, with undo, as in the Inspector.
    /// </summary>
    public sealed class ConfigHubWindow : EditorWindow
    {
        private const string StyleSheetName = "ConfigHub.uss";

        [SerializeField] private string currentPage = HubPages.Overview;
        [SerializeField] private GameConfigAsset editedConfig;

        // The asset each page shows when there are several of its type (e.g. a chain, a second score table).
        private readonly Dictionary<Type, Object> picks = new Dictionary<Type, Object>();

        private VisualElement nav;
        private ScrollView content;
        private VisualElement statusHost;
        private IVisualElementScheduledItem pendingValidation;
        private Action validated;

        [MenuItem("Merge Game/Config Hub", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<ConfigHubWindow>();
            window.titleContent = new GUIContent("Config Hub", HubUI.TypeIcon(typeof(ScriptableObject)));
            window.minSize = new Vector2(780, 480);
            window.Show();
        }

        /// <summary>The config being edited: the installed one unless another was picked.</summary>
        internal GameConfigAsset Config
        {
            get
            {
                if (editedConfig == null)
                    editedConfig = HubAssets.InstalledConfig ?? HubAssets.FindAll<GameConfigAsset>().FirstOrDefault();
                return editedConfig;
            }
        }

        internal IReadOnlyList<string> Problems { get; private set; } = Array.Empty<string>();

        private void OnEnable()
        {
            EditorApplication.projectChanged += OnProjectChanged;
            Undo.undoRedoPerformed += RequestValidation;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            Undo.undoRedoPerformed -= RequestValidation;
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            var styleSheet = LoadStyleSheet();
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            var shell = HubUI.Box("hub-root", EditorGUIUtility.isProSkin ? "dark" : "light");
            shell.Add(BuildHeader());

            var body = HubUI.Box("hub-body");
            nav = HubUI.Box("hub-nav");
            body.Add(nav);
            content = new ScrollView(ScrollViewMode.Vertical);
            content.AddToClassList("hub-content");
            body.Add(content);
            shell.Add(body);

            shell.Add(BuildFooter());
            root.Add(shell);

            Validate();
            ShowPage(currentPage);
        }

        internal void ShowPage(string pageId)
        {
            var page = HubPages.All.FirstOrDefault(candidate => candidate.Id == pageId) ?? HubPages.All[0];
            currentPage = page.Id;
            validated = null;
            BuildNav();

            content.Clear();
            try
            {
                content.Add(page.Build(this));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                var error = HubUI.Page(page.Title, null);
                error.Add(HubUI.Banner($"This page failed to draw: {exception.Message}", HubUI.WarningIcon));
                content.Add(error);
            }

            content.scrollOffset = Vector2.zero;
        }

        /// <summary>Redraws the current page, e.g. after a list of assets changed.</summary>
        internal void Rebuild() => ShowPage(currentPage);

        /// <summary>Goes to the page that edits <paramref name="asset"/>; assets without a page are pinged instead.</summary>
        internal void Reveal(Object asset)
        {
            if (asset == null)
                return;

            var page = HubPages.PageFor(asset);
            if (page == null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
                return;
            }

            if (asset is GameConfigAsset config)
                SetConfig(config);
            else if (asset is ScriptableObject)
                picks[asset.GetType()] = asset;
            ShowPage(page);
        }

        internal T Pick<T>(T fallback) where T : Object
        {
            if (picks.TryGetValue(typeof(T), out var picked) && picked != null)
                return (T)picked;
            return fallback != null ? fallback : HubAssets.FindAll<T>().FirstOrDefault();
        }

        internal void SetPick<T>(T asset) where T : Object
        {
            picks[typeof(T)] = asset;
            Rebuild();
        }

        internal void SetConfig(GameConfigAsset config)
        {
            if (config == editedConfig)
                return;
            editedConfig = config;
            picks.Clear();
            Validate();
        }

        private void BuildNav()
        {
            nav.Clear();
            string group = null;
            foreach (var page in HubPages.All)
            {
                if (page.Group != group)
                {
                    group = page.Group;
                    nav.Add(HubUI.Text(group.ToUpperInvariant(), "hub-nav__group"));
                }

                var item = HubUI.Box("hub-nav__item");
                if (page.Id == currentPage)
                    item.AddToClassList("hub-nav__item--active");
                item.Add(HubUI.Icon(HubUI.TypeIcon(page.IconType), "hub-nav__icon"));
                item.Add(HubUI.Text(page.Title, "hub-nav__label"));

                var badge = page.Badge?.Invoke(this);
                if (!string.IsNullOrEmpty(badge))
                {
                    var badgeLabel = HubUI.Text(badge, "hub-nav__badge");
                    if (page.Id == HubPages.Overview && Problems.Count > 0)
                        badgeLabel.AddToClassList("hub-nav__badge--warn");
                    item.Add(badgeLabel);
                }

                var id = page.Id;
                item.RegisterCallback<ClickEvent>(_ => ShowPage(id));
                nav.Add(item);
            }
        }

        private VisualElement BuildHeader()
        {
            var header = HubUI.Box("hub-header");
            header.Add(HubUI.Text("M", "hub-logo"));

            var titles = HubUI.Box("hub-header__titles");
            titles.Add(HubUI.Text("Merge Game Config Hub", "hub-title"));
            titles.Add(HubUI.Text("Every scriptable asset of the game, its references and its health in one place", "hub-subtitle"));
            header.Add(titles);
            header.Add(HubUI.Box("hub-spacer"));

            statusHost = HubUI.Box("hub-status");
            statusHost.RegisterCallback<ClickEvent>(_ => ShowPage(HubPages.Overview));
            statusHost.tooltip = "Runs the same checks as the config's Validate menu; click to see the details.";
            header.Add(statusHost);

            header.Add(HubUI.Button("Refresh", () =>
            {
                HubAssets.Changed();
                Validate();
                Rebuild();
            }));
            header.Add(HubUI.Button("Save", AssetDatabase.SaveAssets, "hub-button--primary"));
            return header;
        }

        private VisualElement BuildFooter()
        {
            var footer = HubUI.Box("hub-footer");
            footer.Add(HubUI.Text("Edits are written to the assets as you make them (Ctrl+Z undoes them); Save writes them to disk.", "hub-footer__text"));
            var toggle = new Toggle("Show on startup") { value = ConfigHubStartup.ShowOnStartup };
            toggle.AddToClassList("hub-footer__toggle");
            toggle.RegisterValueChangedCallback(change => ConfigHubStartup.ShowOnStartup = change.newValue);
            footer.Add(toggle);
            return footer;
        }

        /// <summary>Re-checks the config shortly after the last of a burst of edits.</summary>
        internal void RequestValidation()
        {
            if (rootVisualElement == null)
                return;
            pendingValidation ??= rootVisualElement.schedule.Execute(Validate);
            pendingValidation.ExecuteLater(300);
        }

        /// <summary>Runs <paramref name="callback"/> after every validation while the current page is shown.</summary>
        internal void OnValidated(Action callback) => validated += callback;

        private void Validate()
        {
            try
            {
                Problems = GameConfigValidator.Validate(Config);
            }
            catch (Exception exception)
            {
                Problems = new[] { $"Validation failed unexpectedly: {exception.Message}" };
            }

            UpdateStatus();
            if (nav != null)
                BuildNav();
            validated?.Invoke();
        }

        private void UpdateStatus()
        {
            if (statusHost == null)
                return;

            statusHost.Clear();
            var ok = Problems.Count == 0;
            var pill = HubUI.Box("hub-pill", ok ? "hub-pill--ok" : "hub-pill--warn", "hub-pill--status");
            pill.Add(HubUI.Icon(ok ? HubUI.OkIcon : HubUI.WarningIcon, "hub-pill__icon"));
            pill.Add(HubUI.Text(ok ? "Config is valid" : $"{Problems.Count} problem{(Problems.Count == 1 ? "" : "s")}", "hub-pill__text"));
            statusHost.Add(pill);
        }

        private void OnProjectChanged()
        {
            HubAssets.Changed();
            RequestValidation();
        }

        private StyleSheet LoadStyleSheet()
        {
            var script = MonoScript.FromScriptableObject(this);
            var folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(script));
            return folder == null ? null : AssetDatabase.LoadAssetAtPath<StyleSheet>(Path.Combine(folder, StyleSheetName).Replace('\\', '/'));
        }
    }
}
