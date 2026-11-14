using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MergeGame.Editor
{
    /// <summary>Makes every PNG in the given folders load as a single 100 PPU sprite without mipmaps.</summary>
    internal static class SpriteImportSettings
    {
        private const float PixelsPerUnit = 100f;

        /// <returns>The asset paths whose import settings had to change and were re-imported.</returns>
        public static IReadOnlyList<string> Apply(params string[] folders)
        {
            var changed = new List<string>();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var folder in folders)
                {
                    foreach (var file in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
                    {
                        var path = file.Replace('\\', '/');
                        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                            throw new InvalidOperationException($"{path} has no texture importer; has Unity imported it?");
                        if (!Configure(importer))
                            continue;

                        importer.SaveAndReimport();
                        changed.Add(path);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return changed;
        }

        /// <returns>True when a setting changed; untouched textures are not re-imported.</returns>
        private static bool Configure(TextureImporter importer)
        {
            var changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit))
            {
                importer.spritePixelsPerUnit = PixelsPerUnit;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            return changed;
        }
    }
}
