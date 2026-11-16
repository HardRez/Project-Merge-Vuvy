using System;
using System.Linq;
using MergeGame.Editor.Building;
using MergeGame.Presentation.Notices;
using MergeGame.View.Audio;
using MergeGame.View.Notices;
using MergeGame.View.Theme;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MergeGame.Editor.ConfigHub
{
    /// <summary>Data the screens read: the colour palette, the sounds and the notice wording.</summary>
    internal static class PresentationPages
    {
        private const string RebuildHint = "The scene builder tints the generated screens with this asset; run Merge Game > Build Scenes after palette changes " +
                                           "to see them on every element (views that read colours at runtime update right away).";

        public static VisualElement Theme(ConfigHubWindow hub)
        {
            var theme = hub.Pick(AssetDatabase.LoadAssetAtPath<UiTheme>(BuildPaths.Theme));
            var page = HubPages.AssetPage(hub, "UI Theme", "The colour palette of the whole UI.", theme,
                () => HubAssets.Create<UiTheme>(BuildPaths.UiDataFolder, "UiTheme"),
                preview: asset =>
                {
                    var card = HubUI.Card("Palette");
                    var body = HubUI.Box();
                    card.Add(body);
                    void Fill()
                    {
                        body.Clear();
                        body.Add(Swatches(asset));
                    }

                    Fill();
                    return (card, (Action)Fill);
                });
            page.Insert(2, HubUI.Banner(RebuildHint));
            return page;
        }

        public static VisualElement Sounds(ConfigHubWindow hub)
        {
            var library = hub.Pick(AssetDatabase.LoadAssetAtPath<SoundLibrary>(BuildPaths.SoundLibrary));
            return HubPages.AssetPage(hub, "Sounds", "Which clip plays for each cue and merge level, and the background music.", library,
                () => HubAssets.Create<SoundLibrary>(BuildPaths.UiDataFolder, "SoundLibrary"),
                preview: asset =>
                {
                    var holder = HubUI.Box();
                    void Fill()
                    {
                        holder.Clear();
                        holder.Add(SoundSummary(asset));
                    }

                    Fill();
                    return (holder, (Action)Fill);
                });
        }

        public static VisualElement Notices(ConfigHubWindow hub)
        {
            var texts = hub.Pick(AssetDatabase.LoadAssetAtPath<NoticeTexts>(BuildPaths.NoticeTexts));
            return HubPages.AssetPage(hub, "Notice Texts", "The short messages the game shows the player, edited as data rather than in code.", texts,
                () => HubAssets.Create<NoticeTexts>(BuildPaths.UiDataFolder, "NoticeTexts"),
                preview: asset =>
                {
                    var card = HubUI.Card("Preview", "As toasts, with the UI theme's colours");
                    var body = HubUI.Box();
                    card.Add(body);
                    void Fill()
                    {
                        body.Clear();
                        body.Add(NoticePreview(asset));
                    }

                    Fill();
                    return (card, (Action)Fill);
                });
        }

        private static VisualElement Swatches(UiTheme theme)
        {
            var root = HubUI.Box();
            using var serialized = new SerializedObject(theme);
            var property = serialized.GetIterator();
            VisualElement group = null;
            var enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyType != SerializedPropertyType.Color)
                    continue;

                var header = HeaderOf(theme, property.name);
                if (header != null || group == null)
                {
                    root.Add(HubUI.Text(header ?? "Colours", "hub-swatch-group"));
                    group = HubUI.Box("hub-swatches");
                    root.Add(group);
                }

                var swatch = HubUI.Box("hub-swatch");
                var color = HubUI.Box("hub-swatch__color");
                color.style.backgroundColor = property.colorValue;
                swatch.Add(color);
                swatch.Add(HubUI.Text(property.displayName, "hub-swatch__name"));
                swatch.Add(HubUI.Text("#" + ColorUtility.ToHtmlStringRGBA(property.colorValue), "hub-swatch__hex"));
                group.Add(swatch);
            }

            return root;
        }

        /// <summary>The [Header] above a serialized field, so the swatches group like the inspector.</summary>
        private static string HeaderOf(UnityEngine.Object owner, string fieldName)
        {
            var field = owner.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            var attribute = field?.GetCustomAttributes(typeof(HeaderAttribute), false).FirstOrDefault() as HeaderAttribute;
            return attribute?.header;
        }

        private static VisualElement SoundSummary(SoundLibrary library)
        {
            var card = HubUI.Card("Coverage");
            var cues = (SoundCue[])Enum.GetValues(typeof(SoundCue));
            var covered = cues.Where(cue => library.TryGet(cue, out _, out _)).ToList();

            var stats = HubUI.Box("hub-stats");
            stats.Add(HubUI.Stat($"{covered.Count}/{cues.Length}", "Cues with a clip"));
            stats.Add(HubUI.Stat(library.MergeClips.Count(clip => clip != null).ToString(), "Merge clips"));
            stats.Add(HubUI.Stat(library.Music != null ? "On" : "None", "Music"));
            stats.Add(HubUI.Stat(Mathf.RoundToInt(library.MusicVolume * 100) + "%", "Music volume"));
            card.Add(stats);

            var missing = cues.Except(covered).ToList();
            if (missing.Count > 0)
                card.Add(HubUI.Banner("Silent cues: " + string.Join(", ", missing.Select(cue => ObjectNames.NicifyVariableName(cue.ToString()))), HubUI.WarningIcon));

            var chips = HubUI.Box("hub-chips");
            foreach (var cue in cues)
            {
                if (!library.TryGet(cue, out var clip, out var volume))
                    continue;
                var chip = HubUI.Chip(clip, () => EditorGUIUtility.PingObject(clip),
                    $"{ObjectNames.NicifyVariableName(cue.ToString())}: {clip.name} · {Mathf.RoundToInt(volume * 100)}%");
                chips.Add(chip);
            }

            card.Add(chips);
            return card;
        }

        private static VisualElement NoticePreview(NoticeTexts texts)
        {
            var root = HubUI.Box();
            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(BuildPaths.Theme);
            var notices = (Notice[])Enum.GetValues(typeof(Notice));
            var missing = notices.Where(notice => !texts.TryGet(notice, out _)).ToList();
            if (missing.Count > 0)
                root.Add(HubUI.Banner("No text for: " + string.Join(", ", missing.Select(notice => ObjectNames.NicifyVariableName(notice.ToString()))), HubUI.WarningIcon));

            foreach (var notice in notices)
            {
                if (!texts.TryGet(notice, out var text))
                    continue;

                var row = HubUI.Box("hub-notice-row");
                row.Add(HubUI.Text(ObjectNames.NicifyVariableName(notice.ToString()), "hub-notice-row__key"));
                var toast = HubUI.Box("hub-toast");
                if (theme != null)
                    toast.style.backgroundColor = theme.ToastBackground;
                if (text.Icon != null)
                    toast.Add(HubUI.SpriteIcon(text.Icon, "hub-toast__icon"));
                var label = HubUI.Text(string.IsNullOrWhiteSpace(text.Text) ? "(empty)" : text.Text, "hub-toast__text");
                if (theme != null)
                    label.style.color = text.Emphasized ? theme.Gold : theme.TextLight;
                toast.Add(label);
                row.Add(toast);
                root.Add(row);
            }

            return root;
        }
    }
}
