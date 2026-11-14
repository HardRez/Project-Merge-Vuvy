using MergeGame.View.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Orders
{
    /// <summary>One requested item in a speech bubble; a check mark shows that the board currently holds it.</summary>
    public sealed class RequirementSlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private RectTransform checkMark;
        [SerializeField] private float checkPopSeconds = 0.3f;

        private TweenChannel checkPop;

        public bool IsFulfilled => checkMark.gameObject.activeSelf;

        private void Awake() => checkPop = new TweenChannel(this);

        public void Show(Sprite itemIcon, bool isFulfilled)
        {
            icon.sprite = itemIcon;
            var becameFulfilled = isFulfilled && !checkMark.gameObject.activeSelf;
            checkMark.gameObject.SetActive(isFulfilled);
            checkMark.localScale = Vector3.one;
            if (becameFulfilled)
                checkPop.Play(Tween.Run(checkPopSeconds, Ease.OutBack, t => checkMark.localScale = Vector3.one * t));
        }
    }
}
