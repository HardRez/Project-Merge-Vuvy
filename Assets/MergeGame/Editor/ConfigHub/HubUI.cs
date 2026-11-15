using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace MergeGame.Editor.ConfigHub
{
    /// <summary>Small building blocks of the Config Hub; every look lives in <c>ConfigHub.uss</c>.</summary>
    internal static class HubUI
    {
        public static VisualElement Page(string title, string description)
        {
            var page = Box("hub-page");
            page.Add(Text(title, "hub-page__title"));
            if (!string.IsNullOrEmpty(description))
                page.Add(Text(description, "hub-page__desc"));
            return page;
        }

        public static VisualElement Card(string title = null, string subtitle = null)
        {
            var card = Box("hub-card");
            if (title == null)
                return card;

            var header = Box("hub-card__header");
            header.Add(Text(title, "hub-card__title"));
            if (!string.IsNullOrEmpty(subtitle))
                header.Add(Text(subtitle, "hub-card__subtitle"));
            card.Add(header);
            return card;
        }

        public static VisualElement Box(params string[] classes)
        {
            var element = new VisualElement();
            foreach (var cls in classes)
                element.AddToClassList(cls);
            return element;
        }

        public static Label Text(string text, params string[] classes)
        {
            var label = new Label(text);
            foreach (var cls in classes)
                label.AddToClassList(cls);
            return label;
        }

        public static Button Button(string text, Action onClick, params string[] classes)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("hub-button");
            foreach (var cls in classes)
                button.AddToClassList(cls);
            return button;
        }

        public static Image Icon(Texture texture, params string[] classes)
        {
            var image = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit };
            foreach (var cls in classes)
                image.AddToClassList(cls);
            return image;
        }

        public static Image SpriteIcon(Sprite sprite, params string[] classes)
        {
            var image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit };
            foreach (var cls in classes)
                image.AddToClassList(cls);
            return image;
        }

        /// <summary>The editor icon of a type, e.g. the AudioClip icon; never logs, unlike looking icons up by name.</summary>
        public static Texture TypeIcon(Type type) => EditorGUIUtility.ObjectContent(null, type).image;

        public static Texture Thumbnail(Object asset) =>
            asset != null ? AssetPreview.GetMiniThumbnail(asset) : TypeIcon(typeof(ScriptableObject));

        public static Texture WarningIcon => EditorGUIUtility.IconContent("console.warnicon.sml").image;
        public static Texture InfoIcon => EditorGUIUtility.IconContent("console.infoicon.sml").image;
        public static Texture OkIcon => EditorGUIUtility.IconContent("TestPassed").image;

        /// <summary>A rounded label; <paramref name="kind"/> is <c>ok</c>, <c>warn</c> or <c>info</c>.</summary>
        public static VisualElement Pill(string text, string kind)
        {
            var pill = Box("hub-pill", "hub-pill--" + kind);
            pill.Add(Text(text, "hub-pill__text"));
            return pill;
        }

        public static VisualElement Stat(string value, string label)
        {
            var stat = Box("hub-stat");
            stat.Add(Text(value, "hub-stat__value"));
            stat.Add(Text(label, "hub-stat__label"));
            return stat;
        }

        public static VisualElement Banner(string text, Texture icon = null)
        {
            var banner = Box("hub-banner");
            banner.Add(Icon(icon ? icon : InfoIcon, "hub-banner__icon"));
            banner.Add(Text(text, "hub-banner__text"));
            return banner;
        }

        public static VisualElement EmptyState(string message, string buttonText = null, Action action = null)
        {
            var box = Box("hub-empty");
            box.Add(Text(message, "hub-empty__text"));
            if (buttonText != null && action != null)
                box.Add(Button(buttonText, action, "hub-button--primary"));
            return box;
        }

        /// <summary>A clickable reference to an asset; the click goes to <paramref name="onClick"/>.</summary>
        public static VisualElement Chip(Object asset, Action onClick, string caption = null)
        {
            var chip = Box("hub-chip");
            chip.Add(Icon(Thumbnail(asset), "hub-chip__icon"));
            chip.Add(Text(caption ?? (asset != null ? asset.name : "None"), "hub-chip__text"));
            if (asset != null)
                chip.tooltip = AssetDatabase.GetAssetPath(asset);
            if (onClick != null)
                chip.RegisterCallback<ClickEvent>(_ => onClick());
            return chip;
        }

        public static VisualElement Problems(System.Collections.Generic.IReadOnlyList<string> problems)
        {
            var list = Box("hub-problems");
            if (problems.Count == 0)
            {
                var ok = Box("hub-ok");
                ok.Add(Icon(OkIcon, "hub-ok__icon"));
                ok.Add(Text("Everything checks out: the game config builds and every sprite, portrait and reference is in place.", "hub-ok__text"));
                list.Add(ok);
                return list;
            }

            foreach (var problem in problems)
            {
                var row = Box("hub-problem");
                row.Add(Icon(WarningIcon, "hub-problem__icon"));
                row.Add(Text(problem, "hub-problem__text"));
                list.Add(row);
            }

            return list;
        }

        /// <summary>Bars for per-level values; <paramref name="caption"/> labels each bar under it.</summary>
        public static VisualElement Bars(System.Collections.Generic.IReadOnlyList<int> values, Func<int, string> caption)
        {
            var bars = Box("hub-bars");
            if (values.Count == 0)
            {
                bars.Add(Text("No values yet.", "hub-muted"));
                return bars;
            }

            var max = 1;
            foreach (var value in values)
                max = Mathf.Max(max, value);

            for (var i = 0; i < values.Count; i++)
            {
                var bar = Box("hub-bar");
                bar.Add(Text(values[i].ToString(), "hub-bar__value"));
                var fill = Box("hub-bar__fill");
                fill.style.height = Length.Percent(Mathf.Max(3f, 72f * values[i] / max));
                if (i > 0 && values[i] < values[i - 1])
                    fill.AddToClassList("hub-bar__fill--bad");
                bar.Add(fill);
                bar.Add(Text(caption(i), "hub-bar__label"));
                bars.Add(bar);
            }

            return bars;
        }
    }
}
