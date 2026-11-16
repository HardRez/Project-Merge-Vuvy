using System;
using System.Collections;
using UnityEngine;

namespace MergeGame.View.Animation
{
    /// <summary>
    /// Coroutine building blocks for UI animation. They run on unscaled time, so pausing the game clock never freezes
    /// the UI, and always finish by applying progress 1, so the end state is exact whatever the frame rate.
    /// </summary>
    public static class Tween
    {
        /// <param name="apply">Receives eased progress every frame, ending with exactly <c>ease(1) = 1</c>.</param>
        public static IEnumerator Run(float duration, EaseFunction ease, Action<float> apply)
        {
            if (ease == null)
                throw new ArgumentNullException(nameof(ease));
            if (apply == null)
                throw new ArgumentNullException(nameof(apply));

            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                apply(ease(elapsed / duration));
                yield return null;
            }

            apply(1f);
        }

        public static IEnumerator Delay(float seconds)
        {
            for (var elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
                yield return null;
        }

        public static IEnumerator Loop(Func<IEnumerator> animation)
        {
            while (true)
                yield return animation();
        }
    }
}
