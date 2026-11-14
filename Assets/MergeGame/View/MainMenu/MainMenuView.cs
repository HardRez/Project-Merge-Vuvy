using System;
using System.Collections;
using MergeGame.Presentation.MainMenu;
using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.MainMenu
{
    /// <summary>Title screen: the logo drops in, then the buttons pop up one after another.</summary>
    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private RectTransform title;

        [Tooltip("Popped in one after another once the title has landed.")]
        [SerializeField] private RectTransform[] entranceItems = new RectTransform[0];

        [SerializeField] private float entranceSeconds = 0.45f;
        [SerializeField] private float buttonDelaySeconds = 0.12f;

        private TweenChannel entrance;

        public event Action PlayRequested;
        public event Action QuitRequested;

        private void Awake()
        {
            entrance = new TweenChannel(this);
            playButton.onClick.AddListener(() => PlayRequested?.Invoke());
            quitButton.onClick.AddListener(() => QuitRequested?.Invoke());
        }

        private void Start() => entrance.Play(PlayEntrance());

        private IEnumerator PlayEntrance()
        {
            foreach (var item in entranceItems)
                item.localScale = Vector3.zero;

            yield return Tween.Run(entranceSeconds, Ease.OutBack, t => title.localScale = Vector3.one * Mathf.LerpUnclamped(0.5f, 1f, t));
            foreach (var item in entranceItems)
            {
                yield return Tween.Delay(buttonDelaySeconds);
                StartCoroutine(Tween.Run(entranceSeconds, Ease.OutBack, t => item.localScale = Vector3.one * t));
            }
        }
    }
}
