using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board.Sand
{
    /// <summary>
    /// Gives every grain the colour of the sand tile at its spot, as the tile is actually drawn (9-sliced, with its
    /// corner scale), so the first frame of a pour looks like the resting tile. Needs Read/Write on the sand textures.
    /// </summary>
    public static class SandGrainPalette
    {
        /// <summary>Texture pixels, read once per sand texture.</summary>
        private static readonly Dictionary<Texture2D, Color32[]> Pixels = new Dictionary<Texture2D, Color32[]>();

        /// <summary>
        /// Fills <paramref name="colours"/> and <paramref name="occupied"/> (row-major from the bottom row) for a grid of
        /// <paramref name="columns"/> × <paramref name="rows"/> grains laid over <paramref name="tile"/>. Transparent parts
        /// of the tile (its rounded corners) get no grain.
        /// </summary>
        public static void Sample(Image tile, int columns, int rows, Color32[] colours, bool[] occupied)
        {
            var sprite = tile.sprite;
            var texture = sprite.texture;
            if (!Pixels.TryGetValue(texture, out var pixels))
            {
                pixels = texture.GetPixels32();
                Pixels.Add(texture, pixels);
            }

            var size = tile.rectTransform.rect.size;
            var spriteRect = sprite.rect;
            var border = sprite.border;
            var pixelsPerUnit = tile.pixelsPerUnit;
            for (var y = 0; y < rows; y++)
            {
                var fromY = SourcePixel(y, rows, size.y, border.y, border.w, spriteRect.y, spriteRect.height, pixelsPerUnit);
                var toY = SourcePixel(y + 1, rows, size.y, border.y, border.w, spriteRect.y, spriteRect.height, pixelsPerUnit);
                for (var x = 0; x < columns; x++)
                {
                    var fromX = SourcePixel(x, columns, size.x, border.x, border.z, spriteRect.x, spriteRect.width, pixelsPerUnit);
                    var toX = SourcePixel(x + 1, columns, size.x, border.x, border.z, spriteRect.x, spriteRect.width, pixelsPerUnit);
                    var colour = Average(pixels, texture.width, texture.height, fromX, toX, fromY, toY);
                    var index = y * columns + x;
                    occupied[index] = colour.a >= 128;
                    colours[index] = new Color32(colour.r, colour.g, colour.b, 255);
                }
            }
        }

        /// <summary>The first sprite pixel (in texture coordinates) under grain edge <paramref name="edge"/> of <paramref name="count"/>.</summary>
        private static int SourcePixel(int edge, int count, float drawnSize, float borderStart, float borderEnd, float spriteStart, float spriteSize, float pixelsPerUnit) =>
            (int)(spriteStart + SourceOffset(edge / (float)count * drawnSize, drawnSize, borderStart, borderEnd, spriteSize, pixelsPerUnit));

        /// <summary>
        /// The mean of the pixels a grain covers (colour weighted by alpha), so grains look like the tile does at its
        /// drawn size rather than like single, high-contrast specks of it.
        /// </summary>
        private static Color32 Average(Color32[] pixels, int width, int height, int fromX, int toX, int fromY, int toY)
        {
            fromX = Mathf.Clamp(fromX, 0, width - 1);
            fromY = Mathf.Clamp(fromY, 0, height - 1);
            toX = Mathf.Clamp(Mathf.Max(toX, fromX + 1), 1, width);
            toY = Mathf.Clamp(Mathf.Max(toY, fromY + 1), 1, height);

            long r = 0, g = 0, b = 0, a = 0;
            for (var y = fromY; y < toY; y++)
            {
                for (var x = fromX; x < toX; x++)
                {
                    var pixel = pixels[y * width + x];
                    r += pixel.r * pixel.a;
                    g += pixel.g * pixel.a;
                    b += pixel.b * pixel.a;
                    a += pixel.a;
                }
            }

            var count = (toX - fromX) * (toY - fromY);
            if (a == 0)
                return default;
            return new Color32((byte)(r / a), (byte)(g / a), (byte)(b / a), (byte)(a / count));
        }

        /// <summary>
        /// Maps a position along one axis of the drawn tile to the sprite pixel drawn there, the way a sliced Image does:
        /// the borders keep their size (shrunk together if the rect is too small for them) and the centre stretches.
        /// </summary>
        private static float SourceOffset(float position, float drawnSize, float borderStart, float borderEnd, float spriteSize, float pixelsPerUnit)
        {
            var drawnStart = borderStart / pixelsPerUnit;
            var drawnEnd = borderEnd / pixelsPerUnit;
            var squeeze = Mathf.Min(1f, drawnSize / Mathf.Max(0.0001f, drawnStart + drawnEnd));
            drawnStart *= squeeze;
            drawnEnd *= squeeze;

            if (position < drawnStart)
                return position / drawnStart * borderStart;
            if (position > drawnSize - drawnEnd)
                return spriteSize - (drawnSize - position) / drawnEnd * borderEnd;

            var centre = (position - drawnStart) / Mathf.Max(0.0001f, drawnSize - drawnStart - drawnEnd);
            return borderStart + centre * (spriteSize - borderStart - borderEnd);
        }
    }
}
