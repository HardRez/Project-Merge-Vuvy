using System;
using MergeGame.Presentation.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Navigation
{
    public sealed class GameMenuView : MonoBehaviour, IGameMenuView
    {
        [SerializeField] private Button backButton;

        public event Action BackToMenuRequested;

        private void Awake() => backButton.onClick.AddListener(() => BackToMenuRequested?.Invoke());
    }
}
