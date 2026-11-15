using System;
using System.Collections;
using MergeGame.Presentation.Settings;
using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Settings
{
    /// <summary>
    /// The settings button and its popup: a window over a dimmed screen with one switch for sound and one for vibration.
    /// A switch is a green button with its icon; while it is off a red slash lies across the icon. The popup pops open, and closes from its close button or a tap on the dimmed screen around it; while it is
    /// open the dim catches every touch, so the game underneath cannot be played.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour, ISettingsView
    {
        [SerializeField] private Button openButton;

        [Tooltip("The whole popup, the dim included; fades in and out.")]
        [SerializeField] private CanvasGroup popup;

        [Tooltip("The window; pops in and out.")]
        [SerializeField] private RectTransform window;

        [SerializeField] private Button closeButton;

        [Tooltip("The dimmed screen around the window; a tap on it closes the popup.")]
        [SerializeField] private Button dimButton;

        [Header("Switches")]
        [SerializeField] private Button soundButton;

        [Tooltip("The red slash across the sound icon, shown while sound is off.")]
        [SerializeField] private GameObject soundOffMark;

        [SerializeField] private Button vibrationButton;

        [Tooltip("The red slash across the vibration icon, shown while vibration is off.")]
        [SerializeField] private GameObject vibrationOffMark;

        [Header("Timing (seconds)")]
        [SerializeField] private float openSeconds = 0.28f;
        [SerializeField] private float closeSeconds = 0.16f;

        private TweenChannel motion;

        public event Action SoundToggled;
        public event Action VibrationToggled;

        /// <summary>True from the moment the popup starts opening until it starts closing.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>What the sound switch shows now.</summary>
        public bool ShowsSoundOn => !soundOffMark.activeSelf;

        /// <summary>What the vibration switch shows now.</summary>
        public bool ShowsVibrationOn => !vibrationOffMark.activeSelf;

        public Button OpenButton => openButton;
        public Button CloseButton => closeButton;
        public Button SoundButton => soundButton;
        public Button VibrationButton => vibrationButton;

        private TweenChannel Motion => motion ??= new TweenChannel(this);

        private void Awake()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            dimButton.onClick.AddListener(Close);
            soundButton.onClick.AddListener(() => SoundToggled?.Invoke());
            vibrationButton.onClick.AddListener(() => VibrationToggled?.Invoke());
            popup.gameObject.SetActive(false);
        }

        public void ShowSettings(bool soundOn, bool vibrationOn)
        {
            soundOffMark.SetActive(!soundOn);
            vibrationOffMark.SetActive(!vibrationOn);
        }

        public void Open()
        {
            if (IsOpen)
                return;
            IsOpen = true;
            popup.gameObject.SetActive(true);
            if (!Motion.Play(PlayOpen()))
                SetOpenness(1f, 1f);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            if (!Motion.Play(PlayClose()))
                popup.gameObject.SetActive(false);
        }

        private IEnumerator PlayOpen()
        {
            var fromAlpha = popup.alpha;
            yield return Tween.Run(openSeconds, Ease.Linear, t =>
                SetOpenness(Mathf.Lerp(fromAlpha, 1f, Mathf.Min(1f, t * 2f)), Mathf.LerpUnclamped(0.8f, 1f, Ease.OutBack(t))));
        }

        private IEnumerator PlayClose()
        {
            var fromAlpha = popup.alpha;
            var fromScale = window.localScale.x;
            yield return Tween.Run(closeSeconds, Ease.InQuad, t => SetOpenness(fromAlpha * (1f - t), Mathf.Lerp(fromScale, 0.9f, t)));
            popup.gameObject.SetActive(false);
        }

        private void SetOpenness(float alpha, float scale)
        {
            popup.alpha = alpha;
            window.localScale = Vector3.one * scale;
        }
    }
}
