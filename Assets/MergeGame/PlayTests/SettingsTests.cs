using System.Collections;
using System.Collections.Generic;
using MergeGame.App.Settings;
using MergeGame.PlayTests.Support;
using MergeGame.View.Feedback;
using MergeGame.View.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MergeGame.PlayTests
{
    /// <summary>The settings button and popup, through the real Game scene. The player's own settings are put back afterwards.</summary>
    public sealed class SettingsTests
    {
        private const string SoundKey = PlayerPrefsGameSettings.DefaultKeyPrefix + "Sound";
        private const string VibrationKey = PlayerPrefsGameSettings.DefaultKeyPrefix + "Vibration";
        private const float AnimationSeconds = 0.45f;

        private readonly Dictionary<string, int?> savedPrefs = new Dictionary<string, int?>();
        private GameScreenDriver game;
        private SettingsView settings;

        [SetUp]
        public void SetUp()
        {
            foreach (var key in new[] { SoundKey, VibrationKey })
            {
                savedPrefs[key] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : (int?)null;
                PlayerPrefs.DeleteKey(key);
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var pair in savedPrefs)
            {
                if (pair.Value.HasValue)
                    PlayerPrefs.SetInt(pair.Key, pair.Value.Value);
                else
                    PlayerPrefs.DeleteKey(pair.Key);
            }

            PlayerPrefs.Save();
            AudioListener.volume = 1f;
        }

        [UnityTest]
        public IEnumerator TheSettingsButton_OpensThePopup_AndTheCloseButtonOrATapBesideItClosesIt()
        {
            yield return Open();
            Assert.That(settings.IsOpen, Is.False);

            GameScreenDriver.Click(settings.OpenButton);
            yield return GameScreenDriver.WaitSeconds(AnimationSeconds);
            Assert.That(settings.IsOpen, Is.True);
            Assert.That(settings.ShowsSoundOn && settings.ShowsVibrationOn, Is.True, "Everything starts on.");

            GameScreenDriver.Click(settings.CloseButton);
            yield return GameScreenDriver.WaitSeconds(AnimationSeconds);
            Assert.That(settings.IsOpen, Is.False);
            Assert.That(PopupObject().activeSelf, Is.False);

            GameScreenDriver.Click(settings.OpenButton);
            yield return GameScreenDriver.WaitSeconds(AnimationSeconds);
            GameScreenDriver.Click(PopupObject().transform.Find("Dim").GetComponent<UnityEngine.UI.Button>());
            yield return GameScreenDriver.WaitSeconds(AnimationSeconds);
            Assert.That(settings.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator TurningSoundOff_MutesEverything_IsRemembered_AndTurnsBackOn()
        {
            yield return Open();
            GameScreenDriver.Click(settings.OpenButton);
            yield return GameScreenDriver.WaitSeconds(AnimationSeconds);

            GameScreenDriver.Click(settings.SoundButton);
            yield return null;
            Assert.That(settings.ShowsSoundOn, Is.False);
            Assert.That(AudioListener.volume, Is.Zero);
            Assert.That(PlayerPrefs.GetInt(SoundKey, 1), Is.Zero);
            Assert.That(settings.ShowsVibrationOn, Is.True, "Sound and vibration are separate switches.");

            GameScreenDriver.Click(settings.SoundButton);
            yield return null;
            Assert.That(settings.ShowsSoundOn, Is.True);
            Assert.That(AudioListener.volume, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator TurningVibrationOff_StopsMergesFromBuzzing()
        {
            yield return Open();
            var haptics = Object.FindAnyObjectByType<Haptics>();
            var pulses = new List<HapticPulse>();
            haptics.Pulsed += pulses.Add;

            yield return Merge();
            Assert.That(pulses, Does.Contain(HapticPulse.Light), "A merge did not buzz.");

            GameScreenDriver.Click(settings.OpenButton);
            yield return GameScreenDriver.WaitSeconds(AnimationSeconds);
            GameScreenDriver.Click(settings.VibrationButton);
            GameScreenDriver.Click(settings.CloseButton);
            yield return GameScreenDriver.WaitSeconds(AnimationSeconds);
            Assert.That(PlayerPrefs.GetInt(VibrationKey, 1), Is.Zero);

            pulses.Clear();
            yield return Merge();
            Assert.That(pulses, Is.Empty, "A merge buzzed with vibration off.");
        }

        private IEnumerator Open()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            settings = Object.FindAnyObjectByType<SettingsView>();
            Assert.That(settings, Is.Not.Null);
        }

        private GameObject PopupObject() => settings.transform.Find("Popup").gameObject;

        private IEnumerator Merge()
        {
            for (var spawns = 0; !game.TryFindMergePair(out _, out _) && spawns < 20; spawns++)
            {
                game.ClickSpawn();
                yield return null;
            }

            Assert.That(game.TryFindMergePair(out var from, out var to), Is.True, "No merge to make.");
            yield return game.DragAndDrop(from, to);
            yield return GameScreenDriver.WaitSeconds(0.3f);
        }
    }
}
