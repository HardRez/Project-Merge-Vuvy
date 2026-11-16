using MergeGame.App.Installers;
using MergeGame.Editor.Building;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace MergeGame.Editor.ConfigHub
{
    /// <summary>Settings that live on installers rather than in data assets: the project context and the Game scene.</summary>
    internal static class ProjectPages
    {
        public static VisualElement ProjectContext(ConfigHubWindow hub)
        {
            var page = HubUI.Page("Project Context",
                "The Zenject ProjectContext prefab, loaded from Resources before the first scene: which game config is played, " +
                "the random seed and the scene names.");

            var prefab = HubAssets.ProjectContextPrefab;
            var installer = HubAssets.ProjectInstaller;
            if (prefab == null || installer == null)
            {
                page.Add(HubUI.EmptyState($"No ProjectContext prefab with a ProjectInstaller at {BuildPaths.ProjectContextPrefab}. " +
                                          "Merge Game > Build Scenes creates it."));
                return page;
            }

            var header = HubUI.Card();
            header.AddToClassList("hub-asset-header");
            header.Add(HubUI.Icon(HubUI.Thumbnail(prefab), "hub-asset-header__thumb"));
            var middle = HubUI.Box("hub-asset-header__middle");
            middle.Add(HubUI.Text(prefab.name, "hub-card__title"));
            middle.Add(HubUI.Text(BuildPaths.ProjectContextPrefab, "hub-asset-header__path"));
            header.Add(middle);
            var actions = HubUI.Box("hub-asset-header__actions");
            actions.Add(HubUI.Button("Ping", () => EditorGUIUtility.PingObject(prefab)));
            actions.Add(HubUI.Button("Open prefab", () => AssetDatabase.OpenAsset(prefab)));
            header.Add(actions);
            page.Add(header);

            page.Add(HubUI.Banner("Rebuilding with Merge Game > Build Scenes recreates this prefab and resets the random seed and scene names; " +
                                  "the game config stays the shipped one."));
            page.Add(HubPages.Inspector(hub, installer, null, "Project Installer"));
            page.Add(HubPages.References(hub, prefab));
            return page;
        }

        public static VisualElement SceneTuning(ConfigHubWindow hub)
        {
            var page = HubUI.Page("Game Scene Tuning",
                "Values set on the Game scene's installer: merge hint timing and the item pool. They are saved with the scene.");

            var installer = Object.FindAnyObjectByType<GameInstaller>(FindObjectsInactive.Include);
            if (installer == null)
            {
                page.Add(HubUI.EmptyState("The Game scene is not open, so its installer cannot be edited.", "Open the Game scene", () =>
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        return;
                    EditorSceneManager.OpenScene(BuildPaths.GameScene);
                    hub.Rebuild();
                }));
                return page;
            }

            var scene = installer.gameObject.scene;
            var row = HubUI.Box("hub-assign");
            row.Add(HubUI.Pill(scene.isDirty ? $"{scene.name} has unsaved changes" : $"Editing {scene.name}", scene.isDirty ? "warn" : "ok"));
            row.Add(HubUI.Button("Select installer", () =>
            {
                Selection.activeObject = installer;
                EditorGUIUtility.PingObject(installer);
            }));
            row.Add(HubUI.Button("Save scene", () =>
            {
                EditorSceneManager.SaveScene(scene);
                hub.Rebuild();
            }, "hub-button--primary"));
            page.Add(row);

            page.Add(HubUI.Banner("Merge Game > Build Scenes regenerates the scene, so tuning set here goes back to the installer's defaults on a rebuild."));
            page.Add(HubPages.Inspector(hub, installer, null, "Game Installer"));
            return page;
        }
    }
}
