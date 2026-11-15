using System;
using System.Collections.Generic;
using MergeGame.Presentation.Cheats;
using MergeGame.Presentation.Navigation;
using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;

namespace MergeGame.Presentation.Tests
{
    public sealed class CheatsPresenterTests
    {
        private FakeCheatsView view;
        private FakeAppNavigator navigator;
        private RecordingProgress progress;
        private CheatsPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            view = new FakeCheatsView();
            navigator = new FakeAppNavigator();
            progress = new RecordingProgress(navigator);
            presenter = new CheatsPresenter(view, progress, navigator);
        }

        [Test]
        public void ResetSave_ForgetsTheProgress_ThenOpensANewGame()
        {
            presenter.Initialize();

            view.PressResetSave();

            Assert.That(navigator.Requests, Is.EqualTo(new[] { nameof(IGameProgress.ResetProgress), nameof(IAppNavigator.OpenGame) }));
        }

        [Test]
        public void NothingHappens_BeforeInitializeOrAfterDispose()
        {
            view.PressResetSave();
            presenter.Initialize();
            presenter.Dispose();
            view.PressResetSave();

            Assert.That(navigator.Requests, Is.Empty);
        }

        [Test]
        public void Constructor_RejectsMissingDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new CheatsPresenter(null, progress, navigator));
            Assert.Throws<ArgumentNullException>(() => new CheatsPresenter(view, null, navigator));
            Assert.Throws<ArgumentNullException>(() => new CheatsPresenter(view, progress, null));
        }

        private sealed class FakeCheatsView : ICheatsView
        {
            public event Action ResetSaveRequested;

            public void PressResetSave() => ResetSaveRequested?.Invoke();
        }

        /// <summary>Logs the reset into the navigator's request list, so one list shows the order of both.</summary>
        private sealed class RecordingProgress : IGameProgress
        {
            private readonly List<string> log;

            public RecordingProgress(FakeAppNavigator navigator) => log = navigator.Requests;

            public void ResetProgress() => log.Add(nameof(ResetProgress));
        }
    }
}
