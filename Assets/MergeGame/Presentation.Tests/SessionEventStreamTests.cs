using System.Collections.Generic;
using MergeGame.Core.Events;
using MergeGame.Presentation.Events;
using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;
using static MergeGame.Presentation.Tests.Support.TestContent;

namespace MergeGame.Presentation.Tests
{
    public sealed class SessionEventStreamTests
    {
        [Test]
        public void Subscribe_DeliversOnlyEventsOfTheSubscribedType()
        {
            var session = new GameBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).Build();
            var stream = new SessionEventStream(session);
            var merges = new List<ItemsMerged>();
            stream.Subscribe<ItemsMerged>(merges.Add);

            session.Spawn();
            session.Drop(At(0, 0), At(1, 0));

            Assert.That(merges.Count, Is.EqualTo(1));
            Assert.That(merges[0].ResultItemId, Is.EqualTo(Item(Wood, 2)));
        }

        [Test]
        public void Events_ReachEveryHandlerInSubscriptionOrder_OneEventAfterTheOther()
        {
            var session = new GameBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).Build();
            var stream = new SessionEventStream(session);
            var received = new List<string>();
            stream.Subscribe<GameEvent>(gameEvent => received.Add($"first {gameEvent.GetType().Name}"));
            stream.Subscribe<GameEvent>(gameEvent => received.Add($"second {gameEvent.GetType().Name}"));

            session.Drop(At(0, 0), At(1, 0));

            Assert.That(received, Is.EqualTo(new[]
            {
                "first ItemsMerged", "second ItemsMerged", "first ScoreChanged", "second ScoreChanged"
            }));
        }

        [Test]
        public void CommandIssuedByAHandler_IsDeliveredAfterTheCurrentEventReachedAllHandlers()
        {
            var session = new GameBuilder().Build();
            var stream = new SessionEventStream(session);
            var received = new List<string>();
            stream.Subscribe<ItemSpawned>(spawned =>
            {
                received.Add($"first {spawned.Position}");
                if (received.Count == 1)
                    session.Spawn();
            });
            stream.Subscribe<ItemSpawned>(spawned => received.Add($"second {spawned.Position}"));

            session.Spawn();

            Assert.That(received, Is.EqualTo(new[] { "first (0, 0)", "second (0, 0)", "first (1, 0)", "second (1, 0)" }));
        }

        [Test]
        public void DisposedSubscription_IsNotCalledAgain_EvenForTheEventBeingDispatched()
        {
            var session = new GameBuilder().Build();
            var stream = new SessionEventStream(session);
            var secondCalls = 0;
            System.IDisposable second = null;
            stream.Subscribe<ItemSpawned>(_ => second.Dispose());
            second = stream.Subscribe<ItemSpawned>(_ => secondCalls++);

            session.Spawn();
            session.Spawn();

            Assert.That(secondCalls, Is.Zero);
        }

        [Test]
        public void DisposedStream_StopsListeningToTheSession()
        {
            var session = new GameBuilder().Build();
            var stream = new SessionEventStream(session);
            var calls = 0;
            stream.Subscribe<GameEvent>(_ => calls++);

            stream.Dispose();
            session.Spawn();

            Assert.That(calls, Is.Zero);
        }
    }
}
