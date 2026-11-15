using System;
using MergeGame.Presentation.Cheats;
using MergeGame.View.Common;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Cheats
{
    /// <summary>
    /// The cheats button in the settings popup and the popup it opens (<see cref="PopupWindow"/>), laid over the settings:
    /// for now one cheat, resetting the saved game. Closing it goes back to the settings.
    /// </summary>
    public sealed class CheatsView : MonoBehaviour, ICheatsView
    {
        [Tooltip("The button in the settings popup that opens the cheats.")]
        [SerializeField] private Button openButton;

        [SerializeField] private PopupWindow popup;
        [SerializeField] private Button resetSaveButton;

        public event Action ResetSaveRequested;

        /// <summary>True from the moment the popup starts opening until it starts closing.</summary>
        public bool IsOpen => popup.IsOpen;

        public Button OpenButton => openButton;
        public Button CloseButton => popup.CloseButton;
        public Button ResetSaveButton => resetSaveButton;

        private void Awake()
        {
            openButton.onClick.AddListener(popup.Open);
            resetSaveButton.onClick.AddListener(() => ResetSaveRequested?.Invoke());
        }
    }
}
