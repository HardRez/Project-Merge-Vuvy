using System;
using MergeGame.Presentation.Settings;
using UnityEngine;
using Zenject;

namespace MergeGame.App.Settings
{
    /// <summary>
    /// Mutes everything the game plays (sound effects and the music) while sound is off, through the listener's master
    /// volume, from the first scene on and whenever the setting changes.
    /// </summary>
    public sealed class SoundSwitch : IInitializable, IDisposable
    {
        private readonly IGameSettings settings;

        public SoundSwitch(IGameSettings settings) => this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        public void Initialize()
        {
            settings.Changed += Apply;
            Apply();
        }

        public void Dispose() => settings.Changed -= Apply;

        private void Apply() => AudioListener.volume = settings.SoundOn ? 1f : 0f;
    }
}
