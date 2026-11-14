using MergeGame.View.Common;
using UnityEngine;

namespace MergeGame.Editor.Building
{
    /// <summary>A generated button and the rect its icon and label go into.</summary>
    internal readonly struct ButtonParts
    {
        public ButtonParts(TactileButton button, RectTransform content)
        {
            Button = button;
            Content = content;
        }

        public TactileButton Button { get; }
        public RectTransform Content { get; }
        public RectTransform Rect => (RectTransform)Button.transform;
    }
}
