using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MergeGame.PlayTests.Support
{
    /// <summary>
    /// Renders the UI camera into an off-screen texture of a chosen size and saves it as a PNG. The canvas follows
    /// the camera's target size, so one running scene can be captured at several device resolutions, even in batch mode.
    /// </summary>
    internal static class Snapshot
    {
        public static readonly Vector2Int[] DeviceResolutions =
        {
            new Vector2Int(1080, 1920),
            new Vector2Int(1080, 2400),
            new Vector2Int(1536, 2048)
        };

        /// <param name="beforeRender">Runs once the layout has adapted to each size, e.g. to re-aim a held drag.</param>
        /// <param name="settleSeconds">Waited after <paramref name="beforeRender"/>, e.g. to catch an animation it started.</param>
        public static IEnumerator CaptureAll(Camera camera, string directory, string name, Action beforeRender = null, float settleSeconds = 0f)
        {
            foreach (var resolution in DeviceResolutions)
                yield return Capture(camera, Path.Combine(directory, $"{name}_{resolution.x}x{resolution.y}.png"), resolution, beforeRender, settleSeconds);
        }

        private static IEnumerator Capture(Camera camera, string path, Vector2Int size, Action beforeRender, float settleSeconds)
        {
            var target = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            try
            {
                // Canvas scaler, layout groups and fitters adapt to the new size over the next frames.
                yield return null;
                yield return null;
                beforeRender?.Invoke();
                yield return null;
                for (var end = Time.realtimeSinceStartup + settleSeconds; Time.realtimeSinceStartup < end;)
                    yield return null;

                Canvas.ForceUpdateCanvases();
                camera.Render();
                File.WriteAllBytes(path, ReadPixels(target));
            }
            finally
            {
                camera.targetTexture = null;
                Object.Destroy(target);
            }
        }

        private static byte[] ReadPixels(RenderTexture source)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = source;
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGB24, mipChain: false);
            try
            {
                texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                return PngEncoder.Encode(texture.GetRawTextureData(), source.width, source.height);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(texture);
            }
        }
    }
}
