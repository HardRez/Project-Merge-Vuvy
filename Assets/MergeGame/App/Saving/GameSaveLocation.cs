using System.IO;
using UnityEngine;

namespace MergeGame.App.Saving
{
    /// <summary>
    /// The folder the save file lives in: a <c>Saves</c> folder under <see cref="Application.persistentDataPath"/>, the
    /// app's private, writable storage that survives app updates on every platform (the app's Documents folder on iOS,
    /// its internal files folder on Android).
    /// </summary>
    public static class GameSaveLocation
    {
        /// <summary>
        /// A folder to use instead of the player's own, set by tests before the first game opens so they never read or
        /// overwrite a real save. Null uses the player's folder.
        /// </summary>
        public static string DirectoryOverride { get; set; }

        /// <summary>Read it on the main thread: Unity's data path is not available from other threads.</summary>
        public static string Directory => DirectoryOverride ?? Path.Combine(Application.persistentDataPath, "Saves");
    }
}
