using System.IO;
using UnityEditor;

namespace MergeGame.Editor.Building
{
    internal static class AssetFolders
    {
        /// <summary>Creates the folder and any missing parents, e.g. <c>Assets/A/B</c>.</summary>
        public static void Ensure(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder))
                return;

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            Ensure(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
