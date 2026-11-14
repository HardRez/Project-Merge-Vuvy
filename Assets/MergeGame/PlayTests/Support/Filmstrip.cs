using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MergeGame.PlayTests.Support
{
    /// <summary>
    /// Captures exact frames of an animation that can be posed by normalised time: each frame is cropped to a region of
    /// the screen, magnified, saved on its own and laid side by side into one strip image.
    /// </summary>
    internal static class Filmstrip
    {
        private const int Gap = 8;

        /// <param name="poseAt">Shows the animation at the given normalised time.</param>
        /// <param name="cropInPixels">The region to keep, in pixels of the <paramref name="screen"/>-sized render (origin bottom-left).</param>
        public static IEnumerator Capture(
            Camera camera,
            Vector2Int screen,
            string directory,
            string name,
            IReadOnlyList<float> stops,
            Action<float> poseAt,
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
                var width = crop.width * magnify;
                var height = crop.height * magnify;
                var frames = new List<byte[]>();
                foreach (var stop in stops)
                {
                    poseAt(stop);
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    var frame = Magnify(ReadCrop(target, crop), crop.width, crop.height, magnify);
                    File.WriteAllBytes(Path.Combine(directory, $"{name}_{Mathf.RoundToInt(stop * 100f):000}.png"), PngEncoder.Encode(frame, width, height));
                    frames.Add(frame);
                }

                var stripWidth = frames.Count * width + (frames.Count - 1) * Gap;
                File.WriteAllBytes(Path.Combine(directory, $"{name}_strip.png"), PngEncoder.Encode(Compose(frames, width, height, stripWidth), stripWidth, height));
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

        /// <summary>Frames side by side with white gaps, bottom-up rows like the frames.</summary>
        private static byte[] Compose(IReadOnlyList<byte[]> frames, int width, int height, int stripWidth)
        {
            var strip = new byte[stripWidth * height * 3];
            for (var i = 0; i < strip.Length; i++)
                strip[i] = 255;

            for (var f = 0; f < frames.Count; f++)
            {
                for (var y = 0; y < height; y++)
                    Buffer.BlockCopy(frames[f], y * width * 3, strip, (y * stripWidth + f * (width + Gap)) * 3, width * 3);
            }

            return strip;
        }
    }
}
