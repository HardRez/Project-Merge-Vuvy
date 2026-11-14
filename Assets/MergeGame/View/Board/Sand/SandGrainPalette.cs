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
                var sourceY = (int)(spriteRect.y + SourceOffset((y + 0.5f) / rows * size.y, size.y, border.y, border.w, spriteRect.height, pixelsPerUnit));
                for (var x = 0; x < columns; x++)
                {
                    var sourceX = (int)(spriteRect.x + SourceOffset((x + 0.5f) / columns * size.x, size.x, border.x, border.z, spriteRect.width, pixelsPerUnit));
                    var colour = pixels[Mathf.Clamp(sourceY, 0, texture.height - 1) * texture.width + Mathf.Clamp(sourceX, 0, texture.width - 1)];
                    var index = y * columns + x;
                    occupied[index] = colour.a >= 128;
                    colours[index] = new Color32(colour.r, colour.g, colour.b, 255);
                }
            }
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
