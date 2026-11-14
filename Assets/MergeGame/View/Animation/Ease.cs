using UnityEngine;

namespace MergeGame.View.Animation
{
    /// <summary>Maps linear progress in [0, 1] to eased progress; every function returns 0 at 0 and 1 at 1.</summary>
    public delegate float EaseFunction(float t);

    public static class Ease
    {
        private const float BackOvershoot = 1.70158f;

        public static float Linear(float t) => t;

        public static float InQuad(float t) => t * t;

        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);

        public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static float InBack(float t) => (BackOvershoot + 1f) * t * t * t - BackOvershoot * t * t;

        /// <summary>Overshoots the target a little and settles back; good for things popping into place.</summary>
        public static float OutBack(float t)
        {
            var u = t - 1f;
            return 1f + (BackOvershoot + 1f) * u * u * u + BackOvershoot * u * u;
        }

        /// <summary>0 → 1 → 0: a bump for punches and pulses (not a progress curve; it ends at 0).</summary>
        public static float Bump(float t) => Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);

        /// <summary>A decaying oscillation that starts and ends at 0, for shakes.</summary>
        public static float Shake(float t, int swings) => Mathf.Sin(t * swings * Mathf.PI) * (1f - t);
    }
}
