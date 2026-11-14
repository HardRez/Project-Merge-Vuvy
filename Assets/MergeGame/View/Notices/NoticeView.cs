using System.Collections;
using MergeGame.Presentation.Notices;
using MergeGame.View.Animation;
using MergeGame.View.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Notices
{
    /// <summary>A toast that pops in with the notice's text, stays for a moment and fades; a new notice replaces it.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class NoticeView : MonoBehaviour, INoticeView
    {
        [SerializeField] private NoticeTexts texts;
        [SerializeField] private UiTheme theme;
        [SerializeField] private RectTransform toast;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float popSeconds = 0.25f;
        [SerializeField] private float holdSeconds = 1.8f;
        [SerializeField] private float fadeSeconds = 0.35f;

        private CanvasGroup canvasGroup;
        private TweenChannel motion;

        public Notice? ShownNotice { get; private set; }
        public string ShownText => label.text;
        public bool IsVisible => canvasGroup.alpha > 0f;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            motion = new TweenChannel(this);
        }

        public void ShowNotice(Notice notice)
        {
            var hasText = texts.TryGet(notice, out var text);
            label.text = hasText ? text.Text : notice.ToString();
            icon.sprite = hasText ? text.Icon : null;
            icon.gameObject.SetActive(icon.sprite != null);

            var emphasized = hasText && text.Emphasized;
            background.color = emphasized ? theme.Gold : theme.ToastBackground;
            label.color = emphasized ? theme.TextDark : theme.TextLight;

            ShownNotice = notice;
            if (!motion.Play(PlayToast()))
                canvasGroup.alpha = 0f;
        }

        private IEnumerator PlayToast()
        {
            yield return Tween.Run(popSeconds, Ease.Linear, t =>
            {
                canvasGroup.alpha = Mathf.Max(canvasGroup.alpha, Ease.OutQuad(t));
                toast.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, Ease.OutBack(t));
            });
            yield return Tween.Delay(holdSeconds);
            yield return Tween.Run(fadeSeconds, Ease.InQuad, t => canvasGroup.alpha = 1f - t);
            ShownNotice = null;
        }
    }
}
