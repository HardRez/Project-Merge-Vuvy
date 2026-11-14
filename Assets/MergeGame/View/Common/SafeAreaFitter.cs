using UnityEngine;

namespace MergeGame.View.Common
{
    /// <summary>Keeps the RectTransform inside the device safe area (notches, rounded corners, home indicator).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect appliedArea;
        private Vector2Int appliedScreenSize;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (Screen.safeArea != appliedArea || ScreenSize != appliedScreenSize)
                Apply();
        }

        private static Vector2Int ScreenSize => new Vector2Int(Screen.width, Screen.height);

        private void Apply()
        {
            appliedArea = Screen.safeArea;
            appliedScreenSize = ScreenSize;
            if (appliedScreenSize.x <= 0 || appliedScreenSize.y <= 0)
                return;

            var rectTransform = (RectTransform)transform;
            var screenSize = (Vector2)appliedScreenSize;
            rectTransform.anchorMin = appliedArea.min / screenSize;
            rectTransform.anchorMax = appliedArea.max / screenSize;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
