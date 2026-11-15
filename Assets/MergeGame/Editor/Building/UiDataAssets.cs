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

        /// <summary>
        /// Created with the imported pack audio; afterwards clips and volumes are the designers' to change. A cue added to
        /// the game after the library was created gets its default clip; the cues already there are kept as they are.
        /// </summary>
        public static SoundLibrary LoadOrCreateSoundLibrary()
        {
            var library = LoadOrCreate<SoundLibrary>(BuildPaths.SoundLibrary, created =>
            {
                created.Cues = DefaultCues();
                created.MergeClips = Enumerable.Range(2, 6).Select(level => Clip($"merge_{level:00}")).ToList();
                created.Music = Clip("music_main");
            });

            var missing = DefaultCues().Where(cue => library.Cues.All(entry => entry.Cue != cue.Cue)).ToArray();
            if (missing.Length > 0)
            {
                library.Cues = library.Cues.Concat(missing).ToArray();
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
            }

            return library;
        }

        private static SoundLibrary.CueClip[] DefaultCues() =>
            new[]
            {
                new SoundLibrary.CueClip(SoundCue.ButtonClick, Clip("button_click"), 0.8f),
                new SoundLibrary.CueClip(SoundCue.Spawn, Clip("spawn"), 0.8f),
                new SoundLibrary.CueClip(SoundCue.PickUp, Clip("pick_up"), 0.6f),
                new SoundLibrary.CueClip(SoundCue.Land, Clip("land"), 0.5f),
                new SoundLibrary.CueClip(SoundCue.Unlock, Clip("unlock"), 0.8f),
                new SoundLibrary.CueClip(SoundCue.SandBreak, Clip("sand_break"), 0.8f),
                new SoundLibrary.CueClip(SoundCue.Discard, Clip("discard"), 0.8f),
                new SoundLibrary.CueClip(SoundCue.OrderComplete, Clip("order_complete"), 0.9f),
                new SoundLibrary.CueClip(SoundCue.Notice, Clip("notice"), 0.5f),
                new SoundLibrary.CueClip(SoundCue.CashLand, Clip("cash_land"), 0.6f),
                new SoundLibrary.CueClip(SoundCue.CashBurst, Clip("cash_burst"), 0.6f),
                new SoundLibrary.CueClip(SoundCue.CashSwoosh, Clip("cash_swoosh"), 0.7f),
                new SoundLibrary.CueClip(SoundCue.CustomerAppear, Clip("customer_appear"), 0.6f)
            };

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
            new NoticeText(Notice.ItemIsLocked, "Locked! Drag a matching item onto it to unlock it.", sprites.Icon("lock")),
            new NoticeText(Notice.MaxLevelReached, "This item is already at its max level!", sprites.Icon("crown"), emphasized: true),
            new NoticeText(Notice.DifferentChain, "Items of different kinds don't merge."),
            new NoticeText(Notice.DifferentLevel, "Only items of the same level merge."),
            new NoticeText(Notice.BoardFull, "The board is full! Merge or discard an item."),
            new NoticeText(Notice.LockedItemCannotBeDiscarded, "Locked items can't be discarded.", sprites.Icon("lock")),
            new NoticeText(Notice.OrderNotReady, "The items for this order aren't ready yet."),
            new NoticeText(Notice.ItemIsHidden, "Pop the bubble next to it to free the sand!", sprites.Sand[0])
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
