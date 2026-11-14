using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace MergeGame.Editor
{
    /// <summary>
    /// Makes every Spine atlas page under a folder import with mipmaps: the portraits are drawn at a small fraction of
    /// their texture size, and without mipmaps their outlines shimmer and break into jagged pixels.
    /// </summary>
    internal static class PortraitTextureImportSettings
    {
        /// <returns>The asset paths whose import settings had to change and were re-imported.</returns>
        public static IReadOnlyList<string> Apply(string folder)
        {
            var changed = new List<string>();
            if (!Directory.Exists(folder))
                return changed;

            foreach (var file in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
            {
                var path = file.Replace(Path.DirectorySeparatorChar, '/');
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    throw new InvalidOperationException($"{path} has no texture importer; has Unity imported it?");
                if (importer.mipmapEnabled && importer.alphaIsTransparency && importer.sRGBTexture)
                    continue;

                importer.mipmapEnabled = true;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.SaveAndReimport();
                changed.Add(path);
            }

            return changed;
        }
    }
}
