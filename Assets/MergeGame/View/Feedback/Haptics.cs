using System;
using MergeGame.Presentation.Settings;
using UnityEngine;
using Zenject;

namespace MergeGame.View.Feedback
{
    public enum HapticPulse
    {
        /// <summary>A short tick: a merge.</summary>
        Light,

        /// <summary>A longer buzz: a delivered order.</summary>
        Strong
    }

    /// <summary>
    /// Buzzes the device on a few rewarding moments, unless the player turned vibration off. On Android it asks the
    /// vibrator for a short pulse of the given length; iOS only offers one long buzz, so there only strong pulses vibrate.
    /// The editor and other platforms do not vibrate, but every pulse is still reported through <see cref="Pulsed"/>.
    /// </summary>
    public sealed class Haptics : MonoBehaviour
    {
        [SerializeField] private int lightMilliseconds = 25;
        [SerializeField] private int strongMilliseconds = 70;

        private IGameSettings settings;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject vibrator;
        private bool vibratorLookedUp;
#endif

        /// <summary>Raised for every pulse that is let through (vibration on); lets tests observe it without a device.</summary>
        public event Action<HapticPulse> Pulsed;

        [Inject]
        public void Construct(IGameSettings gameSettings) => settings = gameSettings;

        public void Pulse(HapticPulse pulse)
        {
            if (settings != null && !settings.VibrationOn)
                return;

            Pulsed?.Invoke(pulse);
            Vibrate(pulse);
        }

        private void Vibrate(HapticPulse pulse)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var milliseconds = pulse == HapticPulse.Strong ? strongMilliseconds : lightMilliseconds;
            try
            {
                Vibrator()?.Call("vibrate", (long)milliseconds);
            }
            catch (Exception)
            {
                // Some devices refuse a timed pulse; fall back to the platform's own buzz (this also makes Unity
                // declare the vibrate permission).
                Handheld.Vibrate();
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (pulse == HapticPulse.Strong)
                Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject Vibrator()
        {
            if (vibratorLookedUp)
                return vibrator;
            vibratorLookedUp = true;
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            return vibrator;
        }

        private void OnDestroy() => vibrator?.Dispose();
#endif
    }

    /// <summary>Haptics are optional for every view: a view without them simply does not buzz.</summary>
    public static class HapticsExtensions
    {
        public static void TryPulse(this Haptics haptics, HapticPulse pulse)
        {
            if (haptics != null)
                haptics.Pulse(pulse);
        }
    }
}
