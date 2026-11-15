using System;

namespace MergeGame.Presentation.Settings
{
    /// <summary>Flips a setting when its switch is pressed and keeps the switches showing the settings as they are.</summary>
    public sealed class SettingsPresenter : IPresenter
    {
        private readonly ISettingsView view;
        private readonly IGameSettings settings;

        public SettingsPresenter(ISettingsView view, IGameSettings settings)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Initialize()
        {
            view.SoundToggled += OnSoundToggled;
            view.VibrationToggled += OnVibrationToggled;
            settings.Changed += Show;
            Show();
        }

        public void Dispose()
        {
            view.SoundToggled -= OnSoundToggled;
            view.VibrationToggled -= OnVibrationToggled;
            settings.Changed -= Show;
        }

        private void OnSoundToggled() => settings.SetSoundOn(!settings.SoundOn);

        private void OnVibrationToggled() => settings.SetVibrationOn(!settings.VibrationOn);

        private void Show() => view.ShowSettings(settings.SoundOn, settings.VibrationOn);
    }
}
