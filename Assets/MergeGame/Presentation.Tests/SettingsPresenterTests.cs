using System;
using System.Collections.Generic;
using MergeGame.Presentation.Settings;
using NUnit.Framework;

namespace MergeGame.Presentation.Tests
{
    public sealed class SettingsPresenterTests
    {
        private FakeSettingsView view;
        private InMemorySettings settings;
        private SettingsPresenter presenter;

        [SetUp]
        public void SetUp()
        {
            view = new FakeSettingsView();
            settings = new InMemorySettings { SoundOn = true, VibrationOn = true };
            presenter = new SettingsPresenter(view, settings);
        }

        [Test]
        public void Initialize_ShowsTheCurrentSettings()
        {
            settings.VibrationOn = false;

            presenter.Initialize();

            Assert.That(view.Shown, Is.EqualTo(new[] { (true, false) }));
        }

        [Test]
        public void PressingTheSoundSwitch_TurnsSoundOffAndOnAgain_AndTheSwitchFollows()
        {
            presenter.Initialize();

            view.PressSound();
            Assert.That(settings.SoundOn, Is.False);
            Assert.That(view.Shown[view.Shown.Count - 1], Is.EqualTo((false, true)));

            view.PressSound();
            Assert.That(settings.SoundOn, Is.True);
            Assert.That(view.Shown[view.Shown.Count - 1], Is.EqualTo((true, true)));
        }

        [Test]
        public void PressingTheVibrationSwitch_TurnsVibrationOff_AndLeavesSoundAlone()
        {
            presenter.Initialize();

            view.PressVibration();

            Assert.That(settings.VibrationOn, Is.False);
            Assert.That(settings.SoundOn, Is.True);
            Assert.That(view.Shown[view.Shown.Count - 1], Is.EqualTo((true, false)));
        }

        [Test]
        public void ASettingChangedElsewhere_IsShownToo()
        {
            presenter.Initialize();

            settings.SetSoundOn(false);

            Assert.That(view.Shown[view.Shown.Count - 1], Is.EqualTo((false, true)));
        }

        [Test]
        public void AfterDispose_TheSwitchesNoLongerChangeAnything()
        {
            presenter.Initialize();
            presenter.Dispose();
            var shown = view.Shown.Count;

            view.PressSound();
            settings.SetVibrationOn(false);

            Assert.That(settings.SoundOn, Is.True);
            Assert.That(view.Shown.Count, Is.EqualTo(shown));
        }

        [Test]
        public void Constructor_RejectsMissingDependencies()
        {
            Assert.Throws<ArgumentNullException>(() => new SettingsPresenter(null, settings));
            Assert.Throws<ArgumentNullException>(() => new SettingsPresenter(view, null));
        }

        private sealed class FakeSettingsView : ISettingsView
        {
            public event Action SoundToggled;
            public event Action VibrationToggled;

            public List<(bool sound, bool vibration)> Shown { get; } = new List<(bool, bool)>();

            public void ShowSettings(bool soundOn, bool vibrationOn) => Shown.Add((soundOn, vibrationOn));

            public void PressSound() => SoundToggled?.Invoke();

            public void PressVibration() => VibrationToggled?.Invoke();
        }

        private sealed class InMemorySettings : IGameSettings
        {
            public event Action Changed;

            public bool SoundOn { get; set; }
            public bool VibrationOn { get; set; }

            public void SetSoundOn(bool on)
            {
                SoundOn = on;
                Changed?.Invoke();
            }

            public void SetVibrationOn(bool on)
            {
                VibrationOn = on;
                Changed?.Invoke();
            }
        }
    }
}
