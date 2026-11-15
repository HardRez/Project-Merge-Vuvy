using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Chains
{
    /// <summary>One level of a chain in the chains popup: the item on its tile, its level on a badge, its name under it, and an arrow on to the next level beside it.</summary>
    public sealed class ChainLevelCellView : MonoBehaviour
    {
        /// <summary>The badge of a chain's last level: nothing merges beyond it.</summary>
        public const string MaxLevelText = "MAX";

        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text levelLabel;

        [Tooltip("The level's badge, drawn larger on the chain's last level so its MAX reads as well as a number.")]
        [SerializeField] private RectTransform badge;
        [SerializeField] private float maxBadgeScale = 1.3f;
        [SerializeField] private TMP_Text nameLabel;

        [Tooltip("Points on to the next level; hidden when no level follows it in the same row.")]
        [SerializeField] private GameObject arrow;

        public Sprite Icon => icon.sprite;
        public string LevelText => levelLabel.text;
        public string NameText => nameLabel.text;
        public bool ShowsArrow => arrow.activeSelf;
        public float BadgeScale => badge.localScale.x;

        /// <param name="isLast">Whether this is the chain's last level, whose badge reads <see cref="MaxLevelText"/> instead of its number.</param>
        /// <param name="pointsOn">Whether the next level stands beside this one, so the arrow points on to it.</param>
        public void Show(Sprite itemIcon, int level, bool isLast, string displayName, bool pointsOn)
        {
            icon.sprite = itemIcon;
            levelLabel.text = isLast ? MaxLevelText : level.ToString();
            badge.localScale = Vector3.one * (isLast ? maxBadgeScale : 1f);
            nameLabel.text = displayName;
            arrow.SetActive(pointsOn);
        }
    }
}
