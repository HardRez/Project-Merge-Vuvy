using MergeGame.Core.Events;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Core.Tests.Support;
using NUnit.Framework;
using static MergeGame.Core.Tests.Support.TestContent;

namespace MergeGame.Core.Tests
{
    public sealed class EventOrderTests
    {
        private static readonly GridPosition Source = new GridPosition(0, 0);
        private static readonly GridPosition Target = new GridPosition(1, 0);

        [Test]
        public void Merge_RaisesItemsMergedThenScoreChanged()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).Build();
            var recorder = new EventRecorder(session);

            session.Drop(Source, Target);

            Assert.That(recorder.Events.Count, Is.EqualTo(2));
            Assert.That(recorder.Events[0], Is.InstanceOf<ItemsMerged>());
            Assert.That(recorder.Events[1], Is.InstanceOf<ScoreChanged>());
        }

        [Test]
        public void Merge_OnFullBoard_RaisesBoardFullChangedLast()
        {
            var session = new SessionBuilder().WithFullBoardOf(Item(Wood, 1)).Build();
            var recorder = new EventRecorder(session);

            session.Drop(Source, Target);

            Assert.That(recorder.Events.Count, Is.EqualTo(3));
            Assert.That(recorder.Events[0], Is.InstanceOf<ItemsMerged>());
            Assert.That(recorder.Events[1], Is.InstanceOf<ScoreChanged>());
            Assert.That(((BoardFullChanged)recorder.Events[2]).IsFull, Is.False);
            Assert.That(session.CanSpawn, Is.True);
        }

        [Test]
        public void ItemsMerged_DescribesTheWholeMerge()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Flower, 3)).WithItem(1, 0, Item(Flower, 3)).Build();
            var recorder = new EventRecorder(session);

            session.Drop(Source, Target);

            var merged = recorder.Single<ItemsMerged>();
            Assert.That(merged.From, Is.EqualTo(Source));
            Assert.That(merged.To, Is.EqualTo(Target));
            Assert.That(merged.ConsumedItemId, Is.EqualTo(Item(Flower, 3)));
            Assert.That(merged.ResultItemId, Is.EqualTo(Item(Flower, 4)));
            Assert.That(merged.AwardedPoints, Is.EqualTo(40));
        }

        [Test]
        public void EventHandlers_SeeTheBoardAlreadyUpdated()
        {
            var session = new SessionBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).Build();
            ItemId? seenOnTarget = null;
            session.EventRaised += gameEvent =>
            {
                if (gameEvent is ItemsMerged)
                    seenOnTarget = session.Board.GetItem(Target).ItemId;
            };

            session.Drop(Source, Target);

            Assert.That(seenOnTarget, Is.EqualTo(Item(Wood, 2)));
        }
    }
}
