using System;
using MergeGame.Presentation.Selection;

namespace MergeGame.Presentation.Tests.Support
{
    internal sealed class FakeItemInfoView : IItemInfoView
    {
        private readonly ViewLog log;

        public FakeItemInfoView(ViewLog log)
        {
            this.log = log;
        }

        public event Action DiscardRequested;

        /// <summary>Null while the hint is shown (or before anything was shown).</summary>
        public ItemInfo Info { get; private set; }

        public bool IsShowingHint { get; private set; }
        public bool IsShowingHiddenItem { get; private set; }
        public bool HasInputListeners => DiscardRequested != null;

        public void RequestDiscard() => DiscardRequested?.Invoke();

        public void ShowItem(ItemInfo info)
        {
            Info = info;
            IsShowingHint = false;
            IsShowingHiddenItem = false;
            log.Add($"Info.ShowItem {info}");
        }

        public void ShowHiddenItem()
        {
            Info = null;
            IsShowingHint = false;
            IsShowingHiddenItem = true;
            log.Add("Info.ShowHiddenItem");
        }

        public void ShowHint()
        {
            Info = null;
            IsShowingHint = true;
            IsShowingHiddenItem = false;
            log.Add("Info.ShowHint");
        }
    }
}
