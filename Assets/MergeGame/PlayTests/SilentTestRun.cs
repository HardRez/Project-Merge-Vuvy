using NUnit.Framework;
using UnityEngine;

namespace MergeGame.PlayTests
{
    /// <summary>
    /// Keeps every PlayMode test (screenshot captures included) silent: the listener is paused for the whole run, so
    /// neither the music nor the sound effects reach the speakers, and unpaused afterwards. No test listens to the
    /// sound itself: the sound tests observe <c>SoundPlayer.ClipPlayed</c>, the settings tests the listener's volume,
    /// which pausing leaves alone.
    /// </summary>
    [SetUpFixture]
    public sealed class SilentTestRun
    {
        [OneTimeSetUp]
        public void Mute() => AudioListener.pause = true;

        [OneTimeTearDown]
        public void Unmute() => AudioListener.pause = false;
    }
}
