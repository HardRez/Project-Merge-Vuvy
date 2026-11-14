using System;
using MergeGame.Presentation.Spawning;
using MergeGame.View.Common;
using TMPro;
using UnityEngine;

namespace MergeGame.View.Spawning
{
    public sealed class SpawnView : MonoBehaviour, ISpawnView
    {
        [SerializeField] private TactileButton button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string availableText = "Üret";
        [SerializeField] private string boardFullText = "Tahta Dolu";

        public event Action SpawnRequested;

        public bool IsAvailable => button.interactable;
        public string Label => label.text;

        private void Awake() => button.onClick.AddListener(() => SpawnRequested?.Invoke());

        public void SetSpawnAvailable(bool isAvailable)
        {
            button.interactable = isAvailable;
            label.text = isAvailable ? availableText : boardFullText;
        }
    }
}
