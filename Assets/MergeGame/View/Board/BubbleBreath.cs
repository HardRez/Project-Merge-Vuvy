using UnityEngine;
using UnityEngine.UI;

namespace MergeGame.View.Board
{
    /// <summary>
    /// A locked item's bubble breathing while it waits: a slow, slight swell with the height trailing the width, so it
    /// wobbles a little like a real bubble. It sits on a layer of its own above the bubble, so the bubble's own swell
    /// and burst on a merge compose with it. Each bubble starts at its own point of the cycle, so neighbours never
    /// breathe in step; the motion is a smooth periodic function of time and never jumps.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class BubbleBreath : MonoBehaviour
    {
        [Tooltip("Breathes only while this bubble is shown.")]
        [SerializeField] private Graphic bubble;

        [Tooltip("Largest change of size, as a share of the bubble's size.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float amplitude = 0.035f;

        [Min(0.1f)]
        [SerializeField] private float periodSeconds = 2.4f;

        [Tooltip("How far, as a share of the cycle, the height trails the width.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float jellyLag = 0.12f;

        private float phase;

        private void OnEnable() => phase = Random.value;

        private void OnDisable() => transform.localScale = Vector3.one;

        private void Update()
        {
            if (!bubble.gameObject.activeSelf)
            {
                transform.localScale = Vector3.one;
                return;
            }

            var cycle = (Time.unscaledTime / periodSeconds + phase) * 2f * Mathf.PI;
            transform.localScale = new Vector3(
                1f + amplitude * Mathf.Sin(cycle),
                1f + amplitude * Mathf.Sin(cycle - jellyLag * 2f * Mathf.PI),
                1f);
        }
    }
}
