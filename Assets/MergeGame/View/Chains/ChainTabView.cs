using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Chains
{
    /// <summary>A chain's tab in the chains popup: its best item and its name; the chosen tab glows yellow.</summary>
    public sealed class ChainTabView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;

        [Tooltip("The glowing yellow square laid over the tab while it is chosen.")]
        [SerializeField] private GameObject chosenMark;

        [SerializeField] private Color labelColor = new Color(0.6f, 0.47f, 0.35f);
        [SerializeField] private Color chosenLabelColor = new Color(0.43f, 0.27f, 0.16f);

        public event Action Clicked;

        public Button Button => button;
        public bool IsChosen => chosenMark.activeSelf;
        public string LabelText => label.text;

        private void Awake() => button.onClick.AddListener(() => Clicked?.Invoke());

        public void Show(Sprite chainIcon, string chainName)
        {
            icon.sprite = chainIcon;
            label.text = chainName;
        }

        public void SetChosen(bool chosen)
        {
            chosenMark.SetActive(chosen);
            label.color = chosen ? chosenLabelColor : labelColor;
        }
    }
}
