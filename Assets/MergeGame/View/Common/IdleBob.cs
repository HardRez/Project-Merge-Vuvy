using UnityEngine;

namespace MergeGame.View.Common
{
    /// <summary>A gentle endless float and sway for decorations, so a static screen still feels alive.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class IdleBob : MonoBehaviour
    {
        [SerializeField] private float amplitude = 14f;
        [SerializeField] private float swayDegrees = 4f;
        [SerializeField] private float periodSeconds = 2.6f;

        [Tooltip("Offsets this decoration's cycle so neighbours do not move in lockstep.")]
        [Range(0f, 1f)]
        [SerializeField] private float phase;

        private RectTransform rectTransform;
        private Vector2 restPosition;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            restPosition = rectTransform.anchoredPosition;
        }

        private void Update()
        {
            var angle = (Time.unscaledTime / periodSeconds + phase) * 2f * Mathf.PI;
            rectTransform.anchoredPosition = restPosition + Vector2.up * (Mathf.Sin(angle) * amplitude);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(angle * 0.5f) * swayDegrees);
        }
    }
}
