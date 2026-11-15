using System;

namespace MergeGame.Presentation.Settings
{
    /// <summary>The settings popup: one switch for sound, one for vibration. Opening and closing it is the view's own business.</summary>
    public interface ISettingsView
    {
        event Action SoundToggled;
        event Action VibrationToggled;

        void ShowSettings(bool soundOn, bool vibrationOn);
    }
}
