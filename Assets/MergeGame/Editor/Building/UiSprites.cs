using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// The tintable UI shapes and icons the screens are made of. The 9-slice borders come from
    /// <c>Tools/PlaceholderArt/README.md</c> (left, bottom, right, top), in source pixels.
    /// </summary>
    internal sealed class UiSprites
    {
        private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>(StringComparer.Ordinal)
        {
            ["panel"] = new Vector4(40, 44, 40, 40),
            ["button"] = new Vector4(44, 54, 44, 40),
            ["pill"] = new Vector4(62, 68, 62, 60),
            ["cell"] = new Vector4(32, 32, 32, 32),
            ["shadow"] = new Vector4(50, 50, 50, 50),
            ["ring"] = new Vector4(34, 34, 34, 34),
            ["board_frame"] = new Vector4(40, 40, 40, 40),
            ["badge"] = new Vector4(24, 26, 24, 22),
            ["speech_bubble"] = new Vector4(52, 60, 36, 36),
            ["selection_brackets"] = new Vector4(60, 60, 60, 60),
            ["board_tray"] = new Vector4(30, 30, 30, 30),
            ["sand_01"] = new Vector4(64, 64, 64, 64),
            ["sand_02"] = new Vector4(64, 64, 64, 64),
            ["sand_03"] = new Vector4(64, 64, 64, 64)
        };

        private UiSprites()
        {
        }

        public Sprite Panel { get; private set; }
        public Sprite Button { get; private set; }
        public Sprite Pill { get; private set; }
        public Sprite Cell { get; private set; }
        public Sprite Circle { get; private set; }
        public Sprite Glow { get; private set; }
        public Sprite Shadow { get; private set; }
        public Sprite Ring { get; private set; }
        public Sprite BoardFrame { get; private set; }
        public Sprite SpeechBubble { get; private set; }
        public Sprite Background { get; private set; }

        /// <summary>Four corner brackets around the selected cell, from the asset pack; drawn in its own colours.</summary>
        public Sprite SelectionBrackets { get; private set; }

        /// <summary>The blue tray the board's tiles sit in, with its own rim and soft shadow; from the asset pack.</summary>
        public Sprite BoardTray { get; private set; }

        /// <summary>A gold-white four-point star with a soft glow, for sparkles; from the asset pack.</summary>
        public Sprite SparkleStar { get; private set; }

        /// <summary>A soft white ring that spreads out from a merge; from the asset pack.</summary>
        public Sprite MergeRing { get; private set; }

        /// <summary>Sand covering a closed cell, in several looks; drawn at twice the cell's resolution.</summary>
        public Sprite[] Sand { get; private set; }

        /// <summary>Unsliced shapes get no border; that includes the opaque background.</summary>
        public static Vector4 BorderOf(string shapeName) => Borders.TryGetValue(shapeName, out var border) ? border : Vector4.zero;

        /// <summary>The sand tiles are read at runtime: their pixels colour the grains of a pouring cover.</summary>
        public static bool IsReadAtRuntime(string shapeName) => shapeName.StartsWith("sand_", StringComparison.Ordinal);

        public static UiSprites Load() => new UiSprites
        {
            Panel = LoadShape("panel"),
            Button = LoadShape("button"),
            Pill = LoadShape("pill"),
            Cell = LoadShape("cell"),
            Circle = LoadShape("circle"),
            Glow = LoadShape("glow"),
            Shadow = LoadShape("shadow"),
            Ring = LoadShape("ring"),
            BoardFrame = LoadShape("board_frame"),
            SpeechBubble = LoadShape("speech_bubble"),
            Background = LoadShape("background"),
            SelectionBrackets = LoadShape("selection_brackets"),
            BoardTray = LoadShape("board_tray"),
            SparkleStar = LoadShape("sparkle_star"),
            MergeRing = LoadShape("merge_ring"),
            Sand = new[] { LoadShape("sand_01"), LoadShape("sand_02"), LoadShape("sand_03") }
        };

        /// <param name="name">An icon file name without extension, e.g. <c>star</c> or <c>customer_01</c>.</param>
        public Sprite Icon(string name) => LoadSprite($"{BuildPaths.IconSpritesFolder}/{name}.png");

        private static Sprite LoadShape(string name) => LoadSprite($"{BuildPaths.UiSpritesFolder}/{name}.png");

        private static Sprite LoadSprite(string path) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? throw new InvalidOperationException($"{path} is missing or does not import as a sprite.");
    }
}
