using System;
using System.Collections.Generic;
using MergeGame.Core.Events;
using MergeGame.Core.Session;

namespace MergeGame.Presentation.Events
{
    /// <summary>
    /// Typed subscriptions over <see cref="MergeGameSession.EventRaised"/>, shared by every presenter of one game screen.
    /// Events are delivered in the order the session raised them, and each event reaches all of its handlers, in
    /// subscription order, before the next one is delivered, even when a handler issues a session command that raises
    /// more events.
    /// </summary>
    public sealed class SessionEventStream : IDisposable
    {
        private readonly MergeGameSession session;
        private readonly List<Subscription> subscriptions = new List<Subscription>();
        private readonly Queue<GameEvent> pending = new Queue<GameEvent>();
        private bool isDispatching;
        private bool isDisposed;

        public SessionEventStream(MergeGameSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            session.EventRaised += Enqueue;
        }

        /// <returns>Disposing it removes the handler; it is not called afterwards, not even for an event being dispatched.</returns>
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : GameEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            if (isDisposed)
                throw new ObjectDisposedException(nameof(SessionEventStream));

            var subscription = new Subscription<TEvent>(this, handler);
            subscriptions.Add(subscription);
            return subscription;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            session.EventRaised -= Enqueue;
            foreach (var subscription in subscriptions)
                subscription.Deactivate();
            subscriptions.Clear();
            pending.Clear();
        }

        private void Enqueue(GameEvent gameEvent)
        {
            pending.Enqueue(gameEvent);
            if (isDispatching)
                return;

            isDispatching = true;
            try
            {
                while (pending.Count > 0)
                    Dispatch(pending.Dequeue());
            }
            finally
            {
                isDispatching = false;
                pending.Clear();
            }
        }

        private void Dispatch(GameEvent gameEvent)
        {
            foreach (var subscription in subscriptions.ToArray())
                subscription.Deliver(gameEvent);
        }

        private void Remove(Subscription subscription) => subscriptions.Remove(subscription);

        private abstract class Subscription : IDisposable
        {
            private readonly SessionEventStream stream;

            protected Subscription(SessionEventStream stream)
            {
                this.stream = stream;
            }

            protected bool IsActive { get; private set; } = true;

            public abstract void Deliver(GameEvent gameEvent);

            public void Deactivate() => IsActive = false;

            public void Dispose()
            {
                if (!IsActive)
                    return;

                Deactivate();
                stream.Remove(this);
            }
        }

        private sealed class Subscription<TEvent> : Subscription where TEvent : GameEvent
        {
            private readonly Action<TEvent> handler;

            public Subscription(SessionEventStream stream, Action<TEvent> handler)
                : base(stream)
            {
                this.handler = handler;
            }

            public override void Deliver(GameEvent gameEvent)
            {
                if (IsActive && gameEvent is TEvent typedEvent)
                    handler(typedEvent);
            }
        }
    }
}
