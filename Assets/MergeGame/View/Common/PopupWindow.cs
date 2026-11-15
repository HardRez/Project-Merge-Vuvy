using System.Collections;
using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Common
{
    /// <summary>
    /// A popup over a dimmed screen: it pops open, and closes from its close button or a tap on the dim around the
    /// window. While it is open the dim catches every touch, so the game underneath cannot be played. It sits on the
    /// popup's root, which it hides once it has set itself up; keep that root active in the scene.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PopupWindow : MonoBehaviour
    {
        [Tooltip("The window; pops in and out.")]
        [SerializeField] private RectTransform window;

        [SerializeField] private Button closeButton;

        [Tooltip("The dimmed screen around the window; a tap on it closes the popup.")]
        [SerializeField] private Button dimButton;

        [Header("Timing (seconds)")]
        [SerializeField] private float openSeconds = 0.28f;
        [SerializeField] private float closeSeconds = 0.16f;

        private CanvasGroup group;
        private TweenChannel motion;

        /// <summary>True from the moment the popup starts opening until it starts closing.</summary>
        public bool IsOpen { get; private set; }

        public Button CloseButton => closeButton;
        public Button DimButton => dimButton;

        private TweenChannel Motion => motion ??= new TweenChannel(this);
        private CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

        private void Awake()
        {
            closeButton.onClick.AddListener(Close);
            dimButton.onClick.AddListener(Close);
            if (!IsOpen)
                gameObject.SetActive(false);
        }

        public void Open()
        {
            if (IsOpen)
                return;
            IsOpen = true;
            gameObject.SetActive(true);
            if (!Motion.Play(PlayOpen()))
                SetOpenness(1f, 1f);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            if (!Motion.Play(PlayClose()))
                gameObject.SetActive(false);
        }

        private IEnumerator PlayOpen()
        {
            var fromAlpha = Group.alpha;
            yield return Tween.Run(openSeconds, Ease.Linear, t =>
                SetOpenness(Mathf.Lerp(fromAlpha, 1f, Mathf.Min(1f, t * 2f)), Mathf.LerpUnclamped(0.8f, 1f, Ease.OutBack(t))));
        }

        private IEnumerator PlayClose()
        {
            var fromAlpha = Group.alpha;
            var fromScale = window.localScale.x;
            yield return Tween.Run(closeSeconds, Ease.InQuad, t => SetOpenness(fromAlpha * (1f - t), Mathf.Lerp(fromScale, 0.9f, t)));
            gameObject.SetActive(false);
        }

        private void SetOpenness(float alpha, float scale)
        {
            Group.alpha = alpha;
            window.localScale = Vector3.one * scale;
        }
    }
}
