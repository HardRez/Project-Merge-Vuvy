using System;
using System.Collections.Generic;

namespace MergeGame.Presentation.Events
{
    /// <summary>Collects a presenter's stream subscriptions so <see cref="IDisposable.Dispose"/> can end them all at once.</summary>
    internal sealed class SubscriptionBag : IDisposable
    {
        private readonly List<IDisposable> subscriptions = new List<IDisposable>();

        public void Add(IDisposable subscription) =>
            subscriptions.Add(subscription ?? throw new ArgumentNullException(nameof(subscription)));

        public void Dispose()
        {
            foreach (var subscription in subscriptions)
                subscription.Dispose();
            subscriptions.Clear();
        }
    }
}
