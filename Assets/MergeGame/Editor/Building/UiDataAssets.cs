using System.Collections.Generic;
using MergeGame.Presentation.Notices;
using MergeGame.View.Notices;
using MergeGame.View.Theme;
using UnityEditor;
using UnityEngine;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// The UI's data assets. They are created once with defaults and then belong to the designers: rebuilding the
    /// scenes never overwrites a palette or a wording someone has edited.
    /// </summary>
    internal static class UiDataAssets
    {
        public static UiTheme LoadOrCreateTheme() => LoadOrCreate<UiTheme>(BuildPaths.Theme, _ => { });

        public static NoticeTexts LoadOrCreateNoticeTexts(UiSprites sprites) =>
            LoadOrCreate<NoticeTexts>(BuildPaths.NoticeTexts, texts => texts.Entries = DefaultNoticeTexts(sprites));

        private static IReadOnlyList<NoticeText> DefaultNoticeTexts(UiSprites sprites) => new[]
        {
            new NoticeText(Notice.ItemIsLocked, "Kilitli! Açmak için aynısını üstüne sürükle.", sprites.Icon("lock")),
            new NoticeText(Notice.MaxLevelReached, "Bu eşya en yüksek seviyede!", sprites.Icon("crown"), emphasized: true),
            new NoticeText(Notice.DifferentChain, "Farklı türdeki eşyalar birleşmez."),
            new NoticeText(Notice.DifferentLevel, "Sadece aynı seviyedeki eşyalar birleşir."),
            new NoticeText(Notice.BoardFull, "Tahta dolu! Birleştir ya da bir eşyayı at."),
            new NoticeText(Notice.LockedItemCannotBeDiscarded, "Kilitli eşyalar atılamaz.", sprites.Icon("lock")),
            new NoticeText(Notice.OrderNotReady, "Bu sipariş için eşyalar henüz hazır değil.")
        };

        private static T LoadOrCreate<T>(string path, System.Action<T> initialize) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            AssetFolders.Ensure(System.IO.Path.GetDirectoryName(path));
            var created = ScriptableObject.CreateInstance<T>();
            initialize(created);
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
