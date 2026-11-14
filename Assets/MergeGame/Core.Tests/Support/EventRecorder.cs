using System.Collections.Generic;
using MergeGame.Core.Events;
using MergeGame.Core.Session;
using NUnit.Framework;

namespace MergeGame.Core.Tests.Support
{
    internal sealed class EventRecorder
    {
        private readonly List<GameEvent> events = new List<GameEvent>();

        public EventRecorder(MergeGameSession session)
        {
            session.EventRaised += events.Add;
        }

        public IReadOnlyList<GameEvent> Events => events;

        public IReadOnlyList<System.Type> Types => events.ConvertAll(gameEvent => gameEvent.GetType());

        public T Single<T>() where T : GameEvent
        {
            T found = null;
            foreach (var gameEvent in events)
            {
                if (gameEvent is T match)
                {
                    Assert.That(found, Is.Null, $"More than one {typeof(T).Name} was raised.");
                    found = match;
                }
            }

            Assert.That(found, Is.Not.Null, $"No {typeof(T).Name} was raised.");
            return found;
        }
    }
}
