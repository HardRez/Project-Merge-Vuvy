using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MergeGame.PlayTests.Support;
using MergeGame.View.Board;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MergeGame.PlayTests
{
    /// <summary>
    /// From the moment a closed cell is opened until its grains start to fall, the sand must look exactly like the
    /// resting sand: same outer edges, same rounded corners, no band of the cell tile showing inside it. Checked by
    /// rendering the cell at several screen sizes, including aspects other than the reference one.
    /// </summary>
    public sealed class SandCoverFootprintTests
    {
        private static readonly Vector2Int[] Resolutions =
        {
            new Vector2Int(1080, 1920),
            new Vector2Int(1080, 2400),
            new Vector2Int(1536, 2048),
            new Vector2Int(720, 1280),
            new Vector2Int(1170, 2532),
            new Vector2Int(1440, 3200),
            new Vector2Int(843, 1500)
        };

        /// <summary>A pixel counts as sand where it differs this much (0–255, any channel) from the bare cell.</summary>
        private const int SandDifference = 24;

        /// <summary>How far, in screen pixels, the sand's edge on any row or column may move (antialiasing, rounding).</summary>
        private const int EdgeTolerance = 2;

        /// <summary>Lines along each side of the cover's rect that are not compared line by line.</summary>
        private const int OuterLines = 2;

        /// <summary>How much the area inside the sand's outline may change, as a share of the resting area.</summary>
        private const float CountTolerance = 0.01f;

        /// <summary>Saves the compared crops when set (see <see cref="ScreenshotCaptureTests"/>).</summary>
        private const string OutputVariable = "MERGE_CAPTURE_DIR";

        private const int Magnify = 3;

        [UnityTest]
        public IEnumerator FromTheTriggerUntilTheGrainsFall_TheSandCoversExactlyTheRestingSandsFootprint()
        {
            GameScreenDriver game = null;
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            yield return GameScreenDriver.WaitSeconds(0.8f);

            Assert.That(game.TryFindCoverOpeningMerge(out var from, out var to), Is.True, "No merge opens a closed cell.");
            var closed = GameScreenDriver.Neighbours(to).First(game.IsCovered);
            Assert.That(game.Board.TryGetCover(closed, out var cover), Is.True);

            var failures = new List<string>();
            var bare = new Frame[Resolutions.Length];
            var resting = new Frame[Resolutions.Length];
            for (var i = 0; i < Resolutions.Length; i++)
            {
                var index = i;
                yield return Render(game.Camera, Resolutions[i], cover, frames =>
                {
                    cover.gameObject.SetActive(false);
                    bare[index] = frames();
                    cover.gameObject.SetActive(true);
                    resting[index] = frames();
                });
            }

            // Back at the screen's own size before aiming the drag.
            yield return null;
            yield return null;
            yield return game.DragAndDrop(from, to);
            Assert.That(game.IsCovered(closed), Is.False, "The cell did not open.");
            Assert.That(cover.GrainCount, Is.Zero, "The sand let go without a delay.");

            // Hold the cover in its delay before the pour (it has moved above the items), and let the merge itself
            // finish, so only the sand is compared.
            cover.StopAllCoroutines();
            yield return GameScreenDriver.WaitSeconds(1f);
            Assert.That(game.Board.TryGetItem(closed, out var revealed), Is.True);

            // The cell as it is now (the revealed item, the merged neighbour's marks) without the sand on top.
            Frame Bare(Func<Frame> frames)
            {
                cover.gameObject.SetActive(false);
                revealed.gameObject.SetActive(false);
                var frame = frames();
                revealed.gameObject.SetActive(true);
                cover.gameObject.SetActive(true);
                return frame;
            }

            for (var i = 0; i < Resolutions.Length; i++)
            {
                var index = i;
                yield return Render(game.Camera, Resolutions[i], cover, frames =>
                {
                    Assert.That(cover.GrainCount, Is.Zero);
                    failures.Add(Compare(bare[index], resting[index], Bare(frames), frames(), index, "delay"));
                });
            }

            for (var i = 0; i < Resolutions.Length; i++)
            {
                var index = i;
                yield return Render(game.Camera, Resolutions[i], cover, frames =>
                {
                    cover.ShowPourAt(0f);
                    Assert.That(cover.GrainCount, Is.GreaterThan(0));
                    failures.Add(Compare(bare[index], resting[index], Bare(frames), frames(), index, "pour_t0"));
                });
            }

            cover.Finish();
            Assert.That(failures.Where(failure => failure != null), Is.Empty);
        }

        /// <returns>What differs from the resting sand, or null when the footprints match.</returns>
        /// <param name="bare">The cell without its sand before it opened; <paramref name="bareNow"/> the same at the moment of <paramref name="shown"/>.</param>
        private static string Compare(Frame bare, Frame resting, Frame bareNow, Frame shown, int resolution, string state)
        {
            var name = $"{Resolutions[resolution].x}x{Resolutions[resolution].y} {state}";
            var file = $"footprint_{Resolutions[resolution].x}x{Resolutions[resolution].y}";
            Save(bare, file + "_0_bare");
            Save(resting, file + "_1_rest");
            Save(shown, $"{file}_2_{state}");

            var restMask = SandMask(bare, resting);
            var shownMask = SandMask(bareNow, shown);
            var restCount = Area(restMask, bare.Width, bare.Height);
            var shownCount = Area(shownMask, bare.Width, bare.Height);
            if (restCount < bare.Width * bare.Height / 2)
                return $"{name}: no resting sand found.";

            // Every row and every column must start and end where the resting sand does and be unbroken in between:
            // an inset or a rounder corner moves these edges, a band of cell tile inside the sand splits them.
            var worst = 0;
            string worstLine = null;
            // The outermost lines of the crop only hold the antialiased rim and are left out.
            for (var y = OuterLines; y < bare.Height - OuterLines; y++)
                Measure(restMask, shownMask, bare.Width, x => y * bare.Width + x, $"row {y}", ref worst, ref worstLine);
            for (var x = OuterLines; x < bare.Width - OuterLines; x++)
                Measure(restMask, shownMask, bare.Height, y => y * bare.Width + x, $"column {x}", ref worst, ref worstLine);

            var summary = $"{name}: sand covers {shownCount} pixels against {restCount} at rest; worst edge {worst} px ({worstLine}).";
            TestContext.WriteLine(summary);
            var countOff = Math.Abs(shownCount - restCount) > restCount * CountTolerance;
            return countOff || worst > EdgeTolerance ? summary : null;
        }

        /// <summary>Keeps the largest edge shift, or gap inside the sand, of any line.</summary>
        private static void Measure(bool[] rest, bool[] shown, int length, Func<int, int> index, string line, ref int worst, ref string worstLine)
        {
            Span(rest, length, index, out var restStart, out var restEnd);
            Span(shown, length, index, out var shownStart, out var shownEnd);
            if (restStart < 0 && shownStart < 0)
                return;

            var shift = restStart < 0 || shownStart < 0
                ? length
                : Math.Max(Math.Abs(shownStart - restStart), Math.Abs(shownEnd - restEnd));
            var gap = 0;
            for (int i = Math.Max(restStart, shownStart) + EdgeTolerance + 1, run = 0; i < Math.Min(restEnd, shownEnd) - EdgeTolerance; i++)
            {
                run = rest[index(i)] && !shown[index(i)] ? run + 1 : 0;
                gap = Math.Max(gap, run);
            }

            var off = Math.Max(shift, gap);
            if (off <= worst)
                return;
            worst = off;
            worstLine = $"{line}: {shownStart}..{shownEnd} against {restStart}..{restEnd}, gap {gap}";
        }

        /// <summary>
        /// The pixels inside the sand's outline (row by row, first to last sand pixel), so light specks in the sand
        /// that happen to match the cell's colour do not count as gaps.
        /// </summary>
        private static int Area(bool[] mask, int width, int height)
        {
            var area = 0;
            for (var y = 0; y < height; y++)
            {
                Span(mask, width, x => y * width + x, out var start, out var end);
                area += start < 0 ? 0 : end - start + 1;
            }

            return area;
        }

        private static void Span(bool[] mask, int length, Func<int, int> index, out int start, out int end)
        {
            start = -1;
            end = -1;
            for (var i = 0; i < length; i++)
            {
                if (!mask[index(i)])
                    continue;
                if (start < 0)
                    start = i;
                end = i;
            }
        }

        private static bool[] SandMask(Frame bare, Frame frame)
        {
            var mask = new bool[bare.Width * bare.Height];
            for (var i = 0; i < mask.Length; i++)
            {
                var difference = 0;
                for (var channel = 0; channel < 3; channel++)
                    difference = Math.Max(difference, Math.Abs(bare.Rgb[i * 3 + channel] - frame.Rgb[i * 3 + channel]));
                mask[i] = difference > SandDifference;
            }

            return mask;
        }

        /// <summary>
        /// Renders the camera at <paramref name="size"/> once the layout has adapted, and hands <paramref name="check"/>
        /// a function that renders the current state and returns the pixels under the cover's rect.
        /// </summary>
        private static IEnumerator Render(Camera camera, Vector2Int size, CellCover cover, Action<Func<Frame>> check)
        {
            var target = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            try
            {
                yield return null;
                yield return null;

                var crop = ScreenRect(camera, (RectTransform)cover.transform);
                check(() =>
                {
                    Canvas.ForceUpdateCanvases();
                    camera.Render();
                    return Read(target, crop);
                });
            }
            finally
            {
                camera.targetTexture = null;
                Object.Destroy(target);
            }
        }

        /// <summary>The pixels the rect covers, rounded inwards.</summary>
        private static RectInt ScreenRect(Camera camera, RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            var x = Mathf.CeilToInt(min.x);
            var y = Mathf.CeilToInt(min.y);
            return new RectInt(x, y, Mathf.FloorToInt(max.x) - x, Mathf.FloorToInt(max.y) - y);
        }

        private static Frame Read(RenderTexture source, RectInt crop)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = source;
            var texture = new Texture2D(crop.width, crop.height, TextureFormat.RGB24, mipChain: false);
            try
            {
                texture.ReadPixels(new Rect(crop.x, crop.y, crop.width, crop.height), 0, 0);
                return new Frame(texture.GetRawTextureData(), crop.width, crop.height);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(texture);
            }
        }

        private static void Save(Frame frame, string name)
        {
            var directory = Environment.GetEnvironmentVariable(OutputVariable);
            if (string.IsNullOrEmpty(directory))
                return;

            Directory.CreateDirectory(directory);
            var big = new byte[frame.Rgb.Length * Magnify * Magnify];
            var bigWidth = frame.Width * Magnify;
            for (var y = 0; y < frame.Height * Magnify; y++)
            {
                for (var x = 0; x < bigWidth; x++)
                    Buffer.BlockCopy(frame.Rgb, ((y / Magnify) * frame.Width + x / Magnify) * 3, big, (y * bigWidth + x) * 3, 3);
            }

            File.WriteAllBytes(Path.Combine(directory, name + ".png"), PngEncoder.Encode(big, bigWidth, frame.Height * Magnify));
        }

        /// <summary>Bottom-up RGB pixels.</summary>
        private readonly struct Frame
        {
            public Frame(byte[] rgb, int width, int height)
            {
                Rgb = rgb;
                Width = width;
                Height = height;
            }

            public byte[] Rgb { get; }
            public int Width { get; }
            public int Height { get; }
        }
    }
}
