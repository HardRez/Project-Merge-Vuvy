using System;
using System.IO;
using System.Text;
using MergeGame.Core.Session;
using UnityEngine;

namespace MergeGame.App.Saving
{
    /// <summary>
    /// Keeps the save as a JSON file (<see cref="GameSaveData"/>) in a folder of its own. Writing is crash safe: the new
    /// contents go to a temporary file which is flushed to disk before it takes the save's place, and the previous save
    /// stays as a backup, so an app killed mid-write (common on iOS and Android, where the system ends background apps
    /// without warning) still finds the old or the new save. Loading falls back to the backup when the save itself is
    /// missing or unreadable. Uses only plain file calls (create, move, delete) that behave the same on every platform.
    /// </summary>
    public sealed class JsonFileSaveStore : IGameSaveStore
    {
        public const string FileName = "game.json";

        private const string LogPrefix = "[Save]";
        private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private readonly string tempPath;

        public JsonFileSaveStore(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("A save folder is required.", nameof(directory));

            Directory = directory;
            FilePath = Path.Combine(directory, FileName);
            BackupPath = FilePath + ".bak";
            tempPath = FilePath + ".tmp";
        }

        public string Directory { get; }
        public string FilePath { get; }
        public string BackupPath { get; }

        public bool TryLoad(out GameSnapshot snapshot) => TryRead(FilePath, out snapshot) || TryRead(BackupPath, out snapshot);

        public void Save(GameSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                WriteFlushed(tempPath, JsonUtility.ToJson(GameSaveData.From(snapshot), prettyPrint: true));

                if (File.Exists(FilePath))
                {
                    if (File.Exists(BackupPath))
                        File.Delete(BackupPath);
                    File.Move(FilePath, BackupPath);
                }

                File.Move(tempPath, FilePath);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"{LogPrefix} Could not write {FilePath}; the previous save is kept. {exception.Message}");
            }
        }

        public void Delete()
        {
            foreach (var path in new[] { FilePath, BackupPath, tempPath })
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    Debug.LogWarning($"{LogPrefix} Could not delete {path}. {exception.Message}");
                }
            }
        }

        private static bool TryRead(string path, out GameSnapshot snapshot)
        {
            snapshot = null;
            if (!File.Exists(path))
                return false;

            try
            {
                var data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path, Utf8));
                if (data == null)
                    throw new FormatException("The file is empty.");
                snapshot = data.ToSnapshot();
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                                              exception is FormatException || exception is ArgumentException)
            {
                Debug.LogWarning($"{LogPrefix} Ignoring unreadable save {path}. {exception.Message}");
                return false;
            }
        }

        private static void WriteFlushed(string path, string contents)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var bytes = Utf8.GetBytes(contents);
                stream.Write(bytes, 0, bytes.Length);
                // Down to the disk, not just the system's cache, before the file replaces the save.
                stream.Flush(flushToDisk: true);
            }
        }
    }
}
