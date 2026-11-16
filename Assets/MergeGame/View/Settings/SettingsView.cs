using System;
using MergeGame.Presentation.Settings;
using MergeGame.View.Common;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Settings
{
    /// <summary>
    /// The settings button and its popup (<see cref="PopupWindow"/>): one switch for sound and one for vibration. A switch
    /// is a green button with its icon; while it is off a red slash lies across the icon.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour, ISettingsView
    {
        [SerializeField] private Button openButton;
        [SerializeField] private PopupWindow popup;

        [Header("Switches")]
        [SerializeField] private Button soundButton;

        [Tooltip("The red slash across the sound icon, shown while sound is off.")]
        [SerializeField] private GameObject soundOffMark;

        [SerializeField] private Button vibrationButton;

        [Tooltip("The red slash across the vibration icon, shown while vibration is off.")]
        [SerializeField] private GameObject vibrationOffMark;

        public event Action SoundToggled;
        public event Action VibrationToggled;

        /// <summary>True from the moment the popup starts opening until it starts closing.</summary>
        public bool IsOpen => popup.IsOpen;

        public bool ShowsSoundOn => !soundOffMark.activeSelf;

        public bool ShowsVibrationOn => !vibrationOffMark.activeSelf;

        public Button OpenButton => openButton;
        public Button CloseButton => popup.CloseButton;
        public Button SoundButton => soundButton;
        public Button VibrationButton => vibrationButton;

        private void Awake()
        {
            openButton.onClick.AddListener(popup.Open);
            soundButton.onClick.AddListener(() => SoundToggled?.Invoke());
            vibrationButton.onClick.AddListener(() => VibrationToggled?.Invoke());
        }

        public void ShowSettings(bool soundOn, bool vibrationOn)
        {
            soundOffMark.SetActive(!soundOn);
            vibrationOffMark.SetActive(!vibrationOn);
        }
    }
}
