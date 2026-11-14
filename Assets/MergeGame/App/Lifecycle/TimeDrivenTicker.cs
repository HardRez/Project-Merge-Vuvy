using System;
using System.Collections.Generic;
using MergeGame.Presentation;
using UnityEngine;
using Zenject;

namespace MergeGame.App.Lifecycle
{
    /// <summary>
    /// The one clock of a screen: advances every <see cref="ITimeDriven"/> presenter by the frame's unscaled time, so
    /// presenters stay free of Unity's time APIs and keep running while the game clock is paused.
    /// </summary>
    public sealed class TimeDrivenTicker : ITickable
    {
        private readonly IReadOnlyList<ITimeDriven> timeDriven;

        public TimeDrivenTicker(List<ITimeDriven> timeDriven)
        {
            this.timeDriven = timeDriven ?? throw new ArgumentNullException(nameof(timeDriven));
        }

        public void Tick()
        {
            var deltaSeconds = Time.unscaledDeltaTime;
            foreach (var presenter in timeDriven)
                presenter.Advance(deltaSeconds);
        }
    }
}
