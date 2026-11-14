using System.Collections;
using UnityEngine;

namespace MergeGame.View.Animation
{
    /// <summary>
    /// At most one running animation per channel: playing a new one stops the previous one where it is. Animations
    /// therefore start from the current visual state and end in a well-defined rest state, which makes them safe to
    /// interrupt at any time.
    /// </summary>
    public sealed class TweenChannel
    {
        private readonly MonoBehaviour host;
        private Coroutine running;

        public TweenChannel(MonoBehaviour host)
        {
            this.host = host;
        }

        /// <returns>False when the host is inactive and the animation was skipped; callers then apply its end state.</returns>
        public bool Play(IEnumerator animation)
        {
            Stop();
            if (!host.isActiveAndEnabled)
                return false;

            running = host.StartCoroutine(animation);
            return true;
        }

        public void Stop()
        {
            if (running != null)
                host.StopCoroutine(running);
            running = null;
        }
    }
}
