using System;
using MergeGame.View.Theme;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace MergeGame.Editor.Building
{
    /// <summary>
    /// The Baloo 2 TextMesh Pro font asset and its material presets. The atlas is static: it holds exactly the
    /// characters the game can show, baked once by the builder, so playing never modifies the asset. Any other
    /// character falls back through TextMesh Pro's default fallback chain.
    /// </summary>
    internal sealed class UiFonts
    {
        private const int AtlasSize = 1024;
        private const string SdfShader = "TextMeshPro/Distance Field";

        /// <summary>Printable ASCII, the Turkish letters and the symbols the UI texts use.</summary>
        private const string GameCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÇçĞğİıÖöŞşÜü•×";

        /// <summary>Tried from the largest down: the first size whose glyphs all fit the atlas is kept, for the crispest text.</summary>
        private static readonly int[] SamplingPointSizes = { 90, 80, 72, 64, 56 };

        private UiFonts(TMP_FontAsset font, Material outline, Material title)
        {
            Font = font;
            Outline = outline;
            Title = title;
        }

        public TMP_FontAsset Font { get; }

        /// <summary>Dark text on light surfaces.</summary>
        public Material Plain => Font.material;

        /// <summary>Light text on coloured buttons and badges: a dark outline and a soft drop shadow.</summary>
        public Material Outline { get; }

        /// <summary>Big headline text: a thick outline and a deeper shadow.</summary>
        public Material Title { get; }

        /// <summary>Creates the font asset if it is missing and (re)writes the material presets from the theme.</summary>
        public static void Generate(UiTheme theme)
        {
            var font = LoadOrCreateFontAsset();
            WritePreset(font, BuildPaths.FontOutlineMaterial, theme.TextOutline, outlineWidth: 0.22f, shadowOffset: -0.55f);
            WritePreset(font, BuildPaths.FontTitleMaterial, theme.TextOutline, outlineWidth: 0.3f, shadowOffset: -0.8f);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Loads what <see cref="Generate"/> wrote.</summary>
        public static UiFonts Load() =>
            new UiFonts(
                Require<TMP_FontAsset>(BuildPaths.FontAsset),
                Require<Material>(BuildPaths.FontOutlineMaterial),
                Require<Material>(BuildPaths.FontTitleMaterial));

        private static TMP_FontAsset LoadOrCreateFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BuildPaths.FontAsset);
            if (existing != null && existing.atlasPopulationMode == AtlasPopulationMode.Static)
                return existing;

            // A dynamic atlas would grow while playing and dirty the asset; replace it with a static one.
            if (existing != null)
                AssetDatabase.DeleteAsset(BuildPaths.FontAsset);

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(BuildPaths.FontFile)
                             ?? throw new InvalidOperationException($"{BuildPaths.FontFile} is missing.");
            var font = BakeLargestFittingAtlas(sourceFont);
            font.name = System.IO.Path.GetFileNameWithoutExtension(BuildPaths.FontAsset);
            font.atlasPopulationMode = AtlasPopulationMode.Static;

            AssetDatabase.CreateAsset(font, BuildPaths.FontAsset);
            font.atlasTexture.name = font.name + " Atlas";
            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.atlasTexture, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return font;
        }

        private static TMP_FontAsset BakeLargestFittingAtlas(Font sourceFont)
        {
            foreach (var pointSize in SamplingPointSizes)
            {
                // Padding of about a tenth of the sampling size leaves room for the outline and shadow presets.
                var font = TMP_FontAsset.CreateFontAsset(
                    sourceFont, pointSize, Mathf.RoundToInt(pointSize * 0.1f), GlyphRenderMode.SDFAA, AtlasSize, AtlasSize,
                    AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: false);
                if (font == null)
                    throw new InvalidOperationException($"TextMesh Pro could not read {BuildPaths.FontFile}.");

                font.material.shader = FindShader();
                if (font.TryAddCharacters(GameCharacters, out var missing))
                    return font;

                Object.DestroyImmediate(font.atlasTexture);
                Object.DestroyImmediate(font.material);
                Object.DestroyImmediate(font);
                if (pointSize == SamplingPointSizes[SamplingPointSizes.Length - 1])
                    throw new InvalidOperationException($"{sourceFont.name}: these characters are missing or do not fit a {AtlasSize}² atlas: {missing}");
            }

            throw new InvalidOperationException("No sampling sizes configured.");
        }

        /// <summary>Writes a material preset that shares the font's atlas; an existing preset keeps its GUID.</summary>
        private static Material WritePreset(TMP_FontAsset font, string path, Color outlineColor, float outlineWidth, float shadowOffset)
        {
            var preset = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (preset == null)
            {
                preset = new Material(font.material);
                AssetDatabase.CreateAsset(preset, path);
            }
            else
            {
                preset.shader = font.material.shader;
                preset.CopyPropertiesFromMaterial(font.material);
            }

            preset.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
            preset.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            preset.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            preset.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.6f));
            preset.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, shadowOffset);
            preset.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);
            preset.SetFloat(ShaderUtilities.ID_UnderlayDilate, outlineWidth * 2f);
            EditorUtility.SetDirty(preset);
            return preset;
        }

        private static T Require<T>(string path) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException($"{path} is missing; generate the fonts first.");

        private static Shader FindShader() =>
            Shader.Find(SdfShader) ?? throw new InvalidOperationException($"Shader '{SdfShader}' not found; are the TMP Essential Resources imported?");
    }
}
