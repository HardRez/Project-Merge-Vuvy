using System;
using MergeGame.Presentation.Notices;
using UnityEngine;

namespace MergeGame.View.Notices
{
    /// <summary>The wording and look of one <see cref="Notice"/>.</summary>
    [Serializable]
    public sealed class NoticeText
    {
        [SerializeField] private Notice notice;

        [TextArea(1, 3)]
        [SerializeField] private string text;

        [Tooltip("Optional icon shown before the text.")]
        [SerializeField] private Sprite icon;

        [Tooltip("Shown in the highlight colour; for notices the player must not miss.")]
        [SerializeField] private bool emphasized;

        public NoticeText(Notice notice, string text, Sprite icon = null, bool emphasized = false)
        {
            this.notice = notice;
            this.text = text;
            this.icon = icon;
            this.emphasized = emphasized;
        }

        public Notice Notice => notice;
        public string Text => text;
        public Sprite Icon => icon;
        public bool Emphasized => emphasized;
    }
}
