using System.IO;
using UnityEditor;
using UnityEngine;

namespace MergeGame.Editor
{
    /// <summary>
    /// Short sound effects are decompressed on load so they start instantly; the music is streamed, so the whole track
    /// is never held in memory as raw samples.
    /// </summary>
    internal static class AudioImportSettings
    {
        public static void Apply(string folder, string musicPath)
        {
            foreach (var file in Directory.GetFiles(folder, "*.ogg"))
            {
                var path = file.Replace('\\', '/');
                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
                    continue;

                var isMusic = path == musicPath;
                var settings = importer.defaultSampleSettings;
                var loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                if (settings.loadType == loadType && importer.loadInBackground == isMusic)
                    continue;

                settings.loadType = loadType;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                importer.defaultSampleSettings = settings;
                importer.loadInBackground = isMusic;
                importer.SaveAndReimport();
            }
        }
    }
}
