using System;

namespace MergeGame.Presentation.Settings
{
    /// <summary>The player's preferences, kept between sessions by the app.</summary>
    public interface IGameSettings
    {
        /// <summary>Whether anything is heard: sound effects and music.</summary>
        bool SoundOn { get; }

        /// <summary>Whether the device vibrates on merges and deliveries.</summary>
        bool VibrationOn { get; }

        /// <summary>Raised after a setting changed.</summary>
        event Action Changed;

        void SetSoundOn(bool on);

        void SetVibrationOn(bool on);
    }
}
