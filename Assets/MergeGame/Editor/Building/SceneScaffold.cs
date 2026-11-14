using System;
using System.IO;
using MergeGame.View.Audio;
using MergeGame.View.Common;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace MergeGame.Editor.Building
{
    /// <summary>What every screen scene has: camera, input, a portrait canvas over the background and a scene context.</summary>
    internal static class SceneScaffold
    {
        private const float CanvasDistance = 10f;

        /// <summary>Touch-friendly: a finger has to travel this far (in pixels) before a press becomes a drag.</summary>
        private const int DragThresholdPixels = 16;

        /// <summary>
        /// Replaces the open scene with an empty one. This unloads unused assets, and references to them loaded
        /// earlier can no longer be serialized, so load the assets a scene needs after creating it.
        /// </summary>
        public static Scene NewScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        public static Camera Camera(Color background)
        {
            var camera = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.transform.position = new Vector3(0f, 0f, -CanvasDistance);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = CanvasDistance * 2f;
            // The ears of the scene; every screen has exactly one, so the effects and the music are heard.
            camera.gameObject.AddComponent<AudioListener>();
            return camera;
        }

        public static void EventSystem()
        {
            var eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            eventSystem.pixelDragThreshold = DragThresholdPixels;
            // Adding the module in the editor assigns the Input System's default UI actions asset.
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>A screen-space canvas rendered by <paramref name="camera"/>, scaled from the portrait reference resolution.</summary>
        public static RectTransform Canvas(Camera camera)
        {
            var canvasObject = new GameObject("Canvas", typeof(RectTransform)) { layer = 5 };
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = CanvasDistance;
            canvas.pixelPerfect = false;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = LayoutMetrics.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return (RectTransform)canvasObject.transform;
        }

        /// <summary>Full-bleed background (it may extend under notches), then the safe-area root for all content.</summary>
        public static RectTransform SafeContent(UiKit ui, RectTransform canvas)
        {
            var background = ui.Image("Background", canvas, ui.Sprites.Background, Color.white);
            var backgroundFitter = background.gameObject.AddComponent<AspectRatioFitter>();
            backgroundFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundFitter.aspectRatio = background.sprite.rect.width / background.sprite.rect.height;

            var safeArea = UiKit.Rect("SafeArea", canvas).Stretch();
            safeArea.gameObject.AddComponent<SafeAreaFitter>();
            return safeArea;
        }

        /// <summary>The screen's sound effect player: one non-spatial source the views play their cues through.</summary>
        public static SoundPlayer Sounds(SoundLibrary library)
        {
            var player = new GameObject("Sounds").AddComponent<SoundPlayer>().Wire("library", library);
            var source = player.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return player;
        }

        /// <summary>Gives a button that has no game action of its own a click.</summary>
        public static void ClickSound(Button button, SoundPlayer sounds) => button.gameObject.AddComponent<ButtonSound>().Wire("sounds", sounds);

        public static TInstaller SceneContext<TInstaller>() where TInstaller : MonoInstaller
        {
            var contextObject = new GameObject("SceneContext");
            var context = contextObject.AddComponent<SceneContext>();
            var installer = contextObject.AddComponent<TInstaller>();
            context.Installers = new MonoInstaller[] { installer };
            return installer;
        }

        public static void Save(Scene scene, string path)
        {
            AssetFolders.Ensure(Path.GetDirectoryName(path));
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException($"Could not save the scene {path}.");
        }
    }
}
