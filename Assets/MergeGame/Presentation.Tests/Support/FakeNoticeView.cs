using System.Collections.Generic;
using MergeGame.Presentation.Notices;

namespace MergeGame.Presentation.Tests.Support
{
    internal sealed class FakeNoticeView : INoticeView
    {
        private readonly ViewLog log;

        public FakeNoticeView(ViewLog log)
        {
            this.log = log;
        }

        public List<Notice> Notices { get; } = new List<Notice>();

        public void ShowNotice(Notice notice)
        {
            Notices.Add(notice);
            log.Add($"Notice.ShowNotice {notice}");
        }
    }
}
