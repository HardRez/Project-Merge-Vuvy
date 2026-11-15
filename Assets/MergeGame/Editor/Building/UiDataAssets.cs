using System.Collections.Generic;
using System.Linq;
using MergeGame.Presentation.Notices;
using MergeGame.View.Audio;
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
        /// <summary>
        /// Default wording that no longer matches the game (closed cells used to be boxes). Only an entry still holding
        /// exactly this text is replaced, so a designer's own wording survives.
        /// </summary>
        private static readonly (Notice notice, string text)[] RetiredDefaults =
        {
            (Notice.ItemIsHidden, "Kutuyu açmak için yanındaki ağı temizle!"),
            // Locked items were webbed before they sat in bubbles.
            (Notice.ItemIsHidden, "Kumu açmak için yanındaki ağı temizle!")
        };

        public static UiTheme LoadOrCreateTheme() => LoadOrCreate<UiTheme>(BuildPaths.Theme, _ => { });

        /// <summary>Created with the imported pack audio; afterwards clips and volumes are the designers' to change.</summary>
        public static SoundLibrary LoadOrCreateSoundLibrary() => LoadOrCreate<SoundLibrary>(BuildPaths.SoundLibrary, library =>
        {
            library.Cues = new[]
            {
                new SoundLibrary.CueClip(SoundCue.ButtonClick, Clip("button_click"), 0.8f),
                new SoundLibrary.CueClip(SoundCue.Spawn, Clip("spawn"), 0.8f),
                new SoundLibrary.CueClip(SoundCue.PickUp, Clip("pick_up"), 0.6f),
                new SoundLibrary.CueClip(SoundCue.Land, Clip("land"), 0.5f),
                new SoundLibrary.CueClip(SoundCue.Unlock, Clip("unlock"), 0.8f),
                new SoundLibrary.CueClip(SoundCue.SandBreak, Clip("sand_break"), 0.8f),
                new SoundLibrary.CueClip(SoundCue.Discard, Clip("discard"), 0.8f),
                new SoundLibrary.CueClip(SoundCue.OrderComplete, Clip("order_complete"), 0.9f),
                new SoundLibrary.CueClip(SoundCue.Notice, Clip("notice"), 0.5f)
            };
            library.MergeClips = Enumerable.Range(2, 6).Select(level => Clip($"merge_{level:00}")).ToList();
            library.Music = Clip("music_main");
        });

        private static AudioClip Clip(string name)
        {
            var path = $"{BuildPaths.AudioFolder}/{name}.ogg";
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path)
                   ?? throw new System.InvalidOperationException($"{path} is missing; run Tools/PackArt/import_pack_art.py.");
        }

        /// <summary>Also adds the default entry of any notice the asset has no text for yet; existing wording is kept.</summary>
        public static NoticeTexts LoadOrCreateNoticeTexts(UiSprites sprites)
        {
            var defaults = DefaultNoticeTexts(sprites);
            var texts = LoadOrCreate<NoticeTexts>(BuildPaths.NoticeTexts, created => created.Entries = defaults);
            var missing = defaults.Where(entry => !texts.TryGet(entry.Notice, out _)).ToList();
            var replaced = ReplaceRetiredWording(texts, defaults);
            if (missing.Count > 0 || replaced)
            {
                texts.Entries = texts.Entries.Concat(missing).ToList();
                EditorUtility.SetDirty(texts);
                AssetDatabase.SaveAssets();
            }

            return texts;
        }

        private static bool ReplaceRetiredWording(NoticeTexts texts, IReadOnlyList<NoticeText> defaults)
        {
            var replaced = false;
            var entries = texts.Entries.Select(entry =>
            {
                if (entry == null || !RetiredDefaults.Any(retired => retired.notice == entry.Notice && retired.text == entry.Text))
                    return entry;

                replaced = true;
                return defaults.First(fresh => fresh.Notice == entry.Notice);
            }).ToList();

            if (replaced)
                texts.Entries = entries;
            return replaced;
        }

        private static IReadOnlyList<NoticeText> DefaultNoticeTexts(UiSprites sprites) => new[]
        {
            new NoticeText(Notice.ItemIsLocked, "Kilitli! Açmak için aynısını üstüne sürükle.", sprites.Icon("lock")),
            new NoticeText(Notice.MaxLevelReached, "Bu eşya en yüksek seviyede!", sprites.Icon("crown"), emphasized: true),
            new NoticeText(Notice.DifferentChain, "Farklı türdeki eşyalar birleşmez."),
            new NoticeText(Notice.DifferentLevel, "Sadece aynı seviyedeki eşyalar birleşir."),
            new NoticeText(Notice.BoardFull, "Tahta dolu! Birleştir ya da bir eşyayı at."),
            new NoticeText(Notice.LockedItemCannotBeDiscarded, "Kilitli eşyalar atılamaz.", sprites.Icon("lock")),
            new NoticeText(Notice.OrderNotReady, "Bu sipariş için eşyalar henüz hazır değil."),
            new NoticeText(Notice.ItemIsHidden, "Kumu açmak için yanındaki baloncuğu patlat!", sprites.Sand[0])
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
