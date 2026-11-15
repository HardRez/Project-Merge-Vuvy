using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MergeGame.PlayTests.Support
{
    /// <summary>
    /// Captures exact frames of an animation that can be posed frame by frame, each cropped to a region of the screen
    /// and magnified, as numbered PNGs (to be assembled into an animation outside the project).
    /// </summary>
    internal static class CroppedFrames
    {
        /// <param name="pose">Poses frame n; returns false once there is nothing more to show after it.</param>
        /// <param name="cropInPixels">The region to keep, in pixels of the <paramref name="screen"/>-sized render (origin bottom-left).</param>
        public static IEnumerator Capture(
            Camera camera,
            Vector2Int screen,
            string directory,
            string name,
            int maxFrames,
            Func<int, bool> pose,
            Func<RectInt> cropInPixels,
            int magnify)
        {
            var target = new RenderTexture(screen.x, screen.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            try
            {
                // Canvas scaler and layout adapt to the render size over the next frames.
                yield return null;
                yield return null;

                var crop = cropInPixels();
                for (var frame = 0; frame < maxFrames; frame++)
                {
                    var more = pose(frame);
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    var pixels = Magnify(ReadCrop(target, crop), crop.width, crop.height, magnify);
                    var png = PngEncoder.Encode(pixels, crop.width * magnify, crop.height * magnify);
                    File.WriteAllBytes(Path.Combine(directory, $"{name}_{frame:000}.png"), png);
                    if (!more)
                        break;
                }
            }
            finally
            {
                camera.targetTexture = null;
                Object.Destroy(target);
            }
        }

        /// <summary>
        /// Films an animation that runs on its own: calls <paramref name="start"/> once the render size has settled,
        /// then saves one cropped frame per game frame. Set <see cref="Time.captureDeltaTime"/> first so every frame
        /// advances the animation by the same time.
        /// </summary>
        public static IEnumerator CaptureLive(
            Camera camera,
            Vector2Int screen,
            string directory,
            string name,
            int frames,
            Action start,
            Func<RectInt> cropInPixels,
            int magnify)
        {
            var target = new RenderTexture(screen.x, screen.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            try
            {
                yield return null;
                yield return null;

                var crop = cropInPixels();
                start();
                for (var frame = 0; frame < frames; frame++)
                {
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    var pixels = Magnify(ReadCrop(target, crop), crop.width, crop.height, magnify);
                    var png = PngEncoder.Encode(pixels, crop.width * magnify, crop.height * magnify);
                    File.WriteAllBytes(Path.Combine(directory, $"{name}_{frame:000}.png"), png);
                }
            }
            finally
            {
                camera.targetTexture = null;
                Object.Destroy(target);
            }
        }

        private static byte[] ReadCrop(RenderTexture source, RectInt crop)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = source;
            var texture = new Texture2D(crop.width, crop.height, TextureFormat.RGB24, mipChain: false);
            try
            {
                texture.ReadPixels(new Rect(crop.x, crop.y, crop.width, crop.height), 0, 0);
                return texture.GetRawTextureData();
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(texture);
            }
        }

        /// <summary>Nearest-neighbour upscale of a bottom-up RGB image.</summary>
        private static byte[] Magnify(byte[] rgb, int width, int height, int factor)
        {
            var bigWidth = width * factor;
            var result = new byte[bigWidth * height * factor * 3];
            for (var y = 0; y < height * factor; y++)
            {
                for (var x = 0; x < bigWidth; x++)
                    Buffer.BlockCopy(rgb, ((y / factor) * width + x / factor) * 3, result, (y * bigWidth + x) * 3, 3);
            }

            return result;
        }
    }
}
