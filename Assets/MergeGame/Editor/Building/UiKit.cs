using MergeGame.View.Common;
using MergeGame.View.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.Editor.Building
{
    /// <summary>How a text is drawn; each tone pairs a colour of the theme with a font material.</summary>
    internal enum TextTone
    {
        /// <summary>Dark brown on light surfaces.</summary>
        Dark,

        /// <summary>Secondary information on light surfaces.</summary>
        Muted,

        /// <summary>White with a dark outline, on coloured surfaces.</summary>
        Light,

        /// <summary>Headlines.</summary>
        Title
    }

    /// <summary>
    /// The building blocks every generated screen is assembled from: rects, tinted sprites, texts and buttons, all
    /// styled from the theme so the screens stay consistent.
    /// </summary>
    internal sealed class UiKit
    {
        private const int UiLayer = 5;

        public UiKit(UiTheme theme, UiSprites sprites, UiFonts fonts)
        {
            Theme = theme;
            Sprites = sprites;
            Fonts = fonts;
        }

        /// <summary>Loads the theme, sprites and fonts; call it after opening the scene they are used in.</summary>
        public static UiKit Load() => new UiKit(UiDataAssets.LoadOrCreateTheme(), UiSprites.Load(), UiFonts.Load());

        public UiTheme Theme { get; }
        public UiSprites Sprites { get; }
        public UiFonts Fonts { get; }

        public static RectTransform Rect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, worldPositionStays: false);
            return rect;
        }

        /// <param name="cornerScale">
        /// Scales the 9-slice corners of sliced sprites (0.5 halves the radius), keeping small elements in proportion.
        /// </param>
        public Image Image(string name, Transform parent, Sprite sprite, Color color, float cornerScale = 1f) =>
            AddImage(Rect(name, parent).gameObject, sprite, color, cornerScale);

        public static Image AddImage(GameObject target, Sprite sprite, Color color, float cornerScale = 1f)
        {
            var image = target.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            if (sprite != null && sprite.border != Vector4.zero)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f / cornerScale;
            }
            else
            {
                image.preserveAspect = true;
            }

            return image;
        }

        /// <summary>
        /// A texture repeated over the rect, each repeat <paramref name="tileWidth"/> canvas units wide (and in proportion
        /// high), starting from the rect's bottom left corner.
        /// </summary>
        public Image Tiled(string name, Transform parent, Sprite sprite, float tileWidth)
        {
            var image = Image(name, parent, sprite, Color.white);
            image.type = UnityEngine.UI.Image.Type.Tiled;
            image.preserveAspect = false;
            // A sprite pixel is one canvas unit at the reference resolution (both are 100 per unit).
            image.pixelsPerUnitMultiplier = sprite.rect.width / tileWidth;
            return image;
        }

        /// <summary>An icon that keeps its aspect ratio inside its rect.</summary>
        public Image Icon(string name, Transform parent, string iconName) => Image(name, parent, Sprites.Icon(iconName), Color.white);

        public TextMeshProUGUI Text(string name, Transform parent, string value, float size, TextTone tone,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var text = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Fonts.Font;
            text.fontSharedMaterial = MaterialFor(tone);
            text.color = ColorFor(tone);
            text.fontSize = size;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        /// <summary>Lets the text shrink down to <paramref name="minimumSize"/> when it does not fit its rect.</summary>
        public static TextMeshProUGUI ShrinkToFit(TextMeshProUGUI text, float minimumSize)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = minimumSize;
            return text;
        }

        /// <summary>
        /// A tactile button: <paramref name="color"/> body with depth edge and gloss, sinking when pressed, grey when
        /// disabled. Put its icon and label under <see cref="ButtonParts.Content"/>.
        /// </summary>
        public ButtonParts Button(string name, Transform parent, Color color, float cornerScale = 1f)
        {
            var rect = Rect(name, parent);
            var background = AddImage(rect.gameObject, Sprites.Button, color, cornerScale);
            background.raycastTarget = true;

            var content = Rect("Content", rect).Stretch(0f, 12f * cornerScale, 0f, 0f);
            var contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            contentGroup.blocksRaycasts = false;

            var button = rect.gameObject.AddComponent<TactileButton>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.Wire("background", background)
                .Wire("enabledColor", color)
                .Wire("disabledColor", Theme.Disabled)
                .Wire("content", contentGroup);
            return new ButtonParts(button, content);
        }

        /// <summary>
        /// A tactile button drawn with coloured art rather than a tinted shape: it keeps the art's own colours, shows
        /// <paramref name="disabledFace"/> (or a grey tint without one) while disabled, and lifts its content off the
        /// art's bottom rim by <paramref name="contentLift"/>. Art sliced only across (no top or bottom border) keeps its
        /// proportions only at one height: pass that height as <paramref name="height"/> to draw the art scaled to it.
        /// </summary>
        public ButtonParts ArtButton(string name, Transform parent, Sprite face, Sprite disabledFace = null, float contentLift = 12f, float height = 0f)
        {
            var rect = Rect(name, parent);
            var cornerScale = height > 0f ? height / face.rect.height : 1f;
            var background = AddImage(rect.gameObject, face, Color.white, cornerScale);
            background.raycastTarget = true;

            var content = Rect("Content", rect).Stretch(0f, contentLift, 0f, 0f);
            var contentGroup = content.gameObject.AddComponent<CanvasGroup>();
            contentGroup.blocksRaycasts = false;

            var button = rect.gameObject.AddComponent<TactileButton>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = background;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.Wire("background", background)
                .Wire("enabledColor", Color.white)
                .Wire("disabledColor", Theme.Disabled)
                .Wire("content", contentGroup);
            if (disabledFace != null)
                button.Wire("enabledSprite", face).Wire("disabledSprite", disabledFace);
            return new ButtonParts(button, content);
        }

        /// <summary>A soft drop shadow behind a panel of the same rect.</summary>
        public Image DropShadow(Transform parent, float spread = 18f, float drop = 14f)
        {
            var shadow = Image("Shadow", parent, Sprites.Shadow, Theme.Shadow);
            shadow.rectTransform.Stretch(-spread, -spread - drop, -spread, -spread + drop);
            return shadow;
        }

        private Material MaterialFor(TextTone tone)
        {
            switch (tone)
            {
                case TextTone.Light:
                    return Fonts.Outline;
                case TextTone.Title:
                    return Fonts.Title;
                default:
                    return Fonts.Plain;
            }
        }

        private Color ColorFor(TextTone tone)
        {
            switch (tone)
            {
                case TextTone.Dark:
                    return Theme.TextDark;
                case TextTone.Muted:
                    return Theme.TextMuted;
                default:
                    return Theme.TextLight;
            }
        }
    }
}
