using System;
using MergeGame.Data.Visuals;
using MergeGame.Presentation.Selection;
using MergeGame.View.Animation;
using MergeGame.View.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace MergeGame.View.Selection
{
    /// <summary>The bar under the board: a hint while nothing is selected, otherwise the selected item's details.</summary>
    public sealed class ItemInfoView : MonoBehaviour, IItemInfoView
    {
        [SerializeField] private GameObject hint;

        [SerializeField] private GameObject details;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private GameObject levelRow;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private string levelFormat = "Seviye {0}/{1}";
        [SerializeField] private GameObject maxLevelTag;
        [SerializeField] private GameObject lockedTag;
        [SerializeField] private GameObject next;
        [SerializeField] private Image nextIcon;
        [SerializeField] private TactileButton discardButton;

        [Header("Closed box")]
        [SerializeField] private Sprite boxIcon;
        [SerializeField] private string boxName = "Kapalı Kutu";

        [Tooltip("One line on how a box opens; shown instead of the level row.")]
        [SerializeField] private GameObject boxExplanation;

        [SerializeField] private float popSeconds = 0.3f;

        private IItemVisuals visuals;
        private TweenChannel pop;

        public event Action DiscardRequested;

        public bool IsShowingHint => hint.activeSelf;
        public string ItemName => nameLabel.text;
        public string LevelText => levelLabel.text;
        public bool CanDiscard => discardButton.isActiveAndEnabled && discardButton.interactable;
        public bool IsShowingBox => details.activeSelf && boxExplanation.activeSelf;

        [Inject]
        public void Construct(IItemVisuals itemVisuals) => visuals = itemVisuals;

        private void Awake()
        {
            pop = new TweenChannel(this);
            discardButton.onClick.AddListener(() => DiscardRequested?.Invoke());
        }

        public void ShowItem(ItemInfo info)
        {
            ShowDetails(visuals.GetIcon(info.ItemId), info.DisplayName, isBox: false);
            levelLabel.text = string.Format(levelFormat, info.Level, info.MaxLevel);
            maxLevelTag.SetActive(info.IsMaxLevel);
            lockedTag.SetActive(info.IsLocked);
            next.SetActive(info.NextItemId.HasValue);
            if (info.NextItemId.HasValue)
                nextIcon.sprite = visuals.GetIcon(info.NextItemId.Value);
            discardButton.interactable = info.CanDiscard;
        }

        /// <summary>A box keeps its secret: only what it is and how it opens; it cannot be discarded.</summary>
        public void ShowHiddenItem()
        {
            ShowDetails(boxIcon, boxName, isBox: true);
            next.SetActive(false);
        }

        private void ShowDetails(Sprite itemIcon, string itemName, bool isBox)
        {
            hint.SetActive(false);
            details.SetActive(true);
            icon.sprite = itemIcon;
            nameLabel.text = itemName;
            levelRow.SetActive(!isBox);
            boxExplanation.SetActive(isBox);
            discardButton.gameObject.SetActive(!isBox);

            var iconTransform = icon.rectTransform;
            pop.Play(Tween.Run(popSeconds, Ease.OutBack, t => iconTransform.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, t)));
        }

        public void ShowHint()
        {
            pop.Stop();
            icon.rectTransform.localScale = Vector3.one;
            details.SetActive(false);
            hint.SetActive(true);
        }
    }
}
