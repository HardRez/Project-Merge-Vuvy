using System.Collections.Generic;
using MergeGame.Presentation.Notices;
using UnityEngine;

namespace MergeGame.View.Notices
{
    /// <summary>The player-facing text of every notice, so wording is edited as data rather than in code.</summary>
    [CreateAssetMenu(menuName = "Merge Game/Notice Texts", fileName = "NoticeTexts")]
    public sealed class NoticeTexts : ScriptableObject
    {
        [SerializeField] private List<NoticeText> entries = new List<NoticeText>();

        public IReadOnlyList<NoticeText> Entries
        {
            get => entries;
            internal set => entries = new List<NoticeText>(value);
        }

        public bool TryGet(Notice notice, out NoticeText text)
        {
            foreach (var entry in entries)
            {
                if (entry != null && entry.Notice == notice)
                {
                    text = entry;
                    return true;
                }
            }

            text = null;
            return false;
        }
    }
}
