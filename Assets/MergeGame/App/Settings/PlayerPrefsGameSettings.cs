using System;
using MergeGame.Presentation.Settings;
using UnityEngine;

namespace MergeGame.App.Settings
{
    /// <summary>
    /// The player's settings, kept in <see cref="PlayerPrefs"/> so they survive restarts. Everything is on until the
    /// player turns it off.
    /// </summary>
    public sealed class PlayerPrefsGameSettings : IGameSettings
    {
        public const string DefaultKeyPrefix = "MergeGame.Settings.";

        private readonly string soundKey;
        private readonly string vibrationKey;

        /// <param name="keyPrefix">Prefixes the stored keys; tests pass their own so they never touch the player's settings.</param>
        public PlayerPrefsGameSettings(string keyPrefix = DefaultKeyPrefix)
        {
            soundKey = keyPrefix + "Sound";
            vibrationKey = keyPrefix + "Vibration";
        }

        public event Action Changed;

        public bool SoundOn => Read(soundKey);
        public bool VibrationOn => Read(vibrationKey);

        public void SetSoundOn(bool on) => Write(soundKey, on);

        public void SetVibrationOn(bool on) => Write(vibrationKey, on);

        private static bool Read(string key) => PlayerPrefs.GetInt(key, 1) != 0;

        private void Write(string key, bool on)
        {
            if (Read(key) == on)
                return;
            PlayerPrefs.SetInt(key, on ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
