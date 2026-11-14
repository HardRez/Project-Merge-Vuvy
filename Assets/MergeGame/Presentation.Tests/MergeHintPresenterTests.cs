using System;
using MergeGame.Presentation.Events;
using MergeGame.Presentation.Hints;
using MergeGame.Presentation.Tests.Support;
using NUnit.Framework;
using static MergeGame.Presentation.Tests.Support.TestContent;

namespace MergeGame.Presentation.Tests
{
    public sealed class MergeHintPresenterTests
    {
        private const float Delay = GameScreen.HintDelaySeconds;
        private const float Repeat = GameScreen.HintRepeatSeconds;

        private static GameBuilder BoardWithAPair() =>
            new GameBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Wood, 1)).WithItem(4, 4, Item(Stone, 1));

        [Test]
        public void Hint_AppearsExactlyWhenTheIdleDelayIsReached()
        {
            using var screen = GameScreen.For(BoardWithAPair());

            screen.Advance(Delay - 0.5f);
            Assert.That(screen.Hint.Shown, Is.Null);

            screen.Advance(0.5f);
            Assert.That(screen.Hint.Shown, Is.EqualTo((At(0, 0), At(1, 0))));
        }

        [Test]
        public void Hint_PlaysAgainEveryRepeatInterval_WhileThePlayerStaysIdle()
        {
            using var screen = GameScreen.For(BoardWithAPair());
            screen.Advance(Delay);

            screen.Advance(Repeat - 0.5f);
            Assert.That(screen.Hint.ShowCount, Is.EqualTo(1));
            screen.Advance(0.5f);
            Assert.That(screen.Hint.ShowCount, Is.EqualTo(2));
            screen.Advance(Repeat);
            Assert.That(screen.Hint.ShowCount, Is.EqualTo(3));

            Assert.That(screen.Hint.Shown, Is.EqualTo((At(0, 0), At(1, 0))));
            Assert.That(screen.Hint.HideCount, Is.Zero);
        }

        [Test]
        public void HugeTimeStep_PlaysAtMostOneHint_AndTheNextComesAFullRepeatIntervalLater()
        {
            using var screen = GameScreen.For(BoardWithAPair());

            screen.Advance(Delay + Repeat * 10);
            Assert.That(screen.Hint.ShowCount, Is.EqualTo(1));

            screen.Advance(Repeat - 0.5f);
            Assert.That(screen.Hint.ShowCount, Is.EqualTo(1));
            screen.Advance(0.5f);
            Assert.That(screen.Hint.ShowCount, Is.EqualTo(2));
        }

        [Test]
        public void ActivityBetweenPulses_CancelsTheHint_AndRestartsTheFullIdleDelay()
        {
            using var screen = GameScreen.For(BoardWithAPair());
            screen.Advance(Delay);
            screen.Advance(Repeat - 1f);

            screen.Selection.Tap(4, 4);

            Assert.That(screen.Hint.HideCount, Is.EqualTo(1));
            screen.Advance(Repeat);
            Assert.That(screen.Hint.ShowCount, Is.EqualTo(1), "The repeat interval no longer applies after activity.");
            screen.Advance(Delay - Repeat);
            Assert.That(screen.Hint.ShowCount, Is.EqualTo(2));
        }

        [Test]
        public void Activity_WithoutAHintSinceTheLastActivity_DoesNotCancelAnything()
        {
            using var screen = GameScreen.For(BoardWithAPair());
            screen.Advance(Delay);
            screen.Selection.Tap(4, 4);

            screen.Selection.Tap(4, 4);
            screen.Spawn.PressSpawn();

            Assert.That(screen.Hint.HideCount, Is.EqualTo(1));
        }

        [Test]
        public void NoPairAtTheFirstCheck_IsCheckedAgainAfterTheRepeatInterval()
        {
            var session = new GameBuilder().WithItem(1, 0, Item(Wood, 1)).Build();
            var log = new ViewLog();
            var hintView = new FakeMergeHintView(log);
            using var unrelatedEvents = new SessionEventStream(new GameBuilder().Build());
            using var presenter = new MergeHintPresenter(
                session, unrelatedEvents, new FakeBoardView(log), new FakeSelectionView(log), hintView, new MergeHintSettings(Delay, Repeat));
            presenter.Initialize();
            presenter.Advance(Delay);

            // Unreachable in the real wiring, where every board change is activity: the presenter here listens to
            // another session's events, so this board change is not seen as activity and the re-query is observable.
            session.Spawn();
            presenter.Advance(Repeat - 0.5f);
            Assert.That(hintView.ShowCount, Is.Zero);
            presenter.Advance(0.5f);

            Assert.That(hintView.Shown, Is.EqualTo((At(0, 0), At(1, 0))));
        }

        [Test]
        public void EveryPulse_QueriesTheBoardAgain()
        {
            var session = BoardWithAPair().WithItem(2, 0, Item(Stone, 2)).WithItem(3, 0, Item(Stone, 2)).Build();
            var log = new ViewLog();
            var hintView = new FakeMergeHintView(log);
            using var unrelatedEvents = new SessionEventStream(new GameBuilder().Build());
            using var presenter = new MergeHintPresenter(
                session, unrelatedEvents, new FakeBoardView(log), new FakeSelectionView(log), hintView, new MergeHintSettings(Delay, Repeat));
            presenter.Initialize();
            presenter.Advance(Delay);
            Assert.That(hintView.Shown, Is.EqualTo((At(0, 0), At(1, 0))));

            // Defensive in the real wiring (see above): a board change between pulses is always activity there.
            session.Drop(At(0, 0), At(1, 0));
            presenter.Advance(Repeat);

            Assert.That(hintView.Shown, Is.EqualTo((At(2, 0), At(3, 0))));
        }

        [Test]
        public void NoHint_WhenNothingCanBeMerged()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(0, 0, Item(Wood, 1)).WithItem(1, 0, Item(Stone, 1)));

            screen.Advance(Delay);
            screen.Advance(Delay * 2);

            Assert.That(screen.Hint.ShowCount, Is.Zero);
        }

        [Test]
        public void BoardWithoutPair_GetsAHint_OneIdlePeriodAfterAMergeBecamePossible()
        {
            using var screen = GameScreen.For(new GameBuilder().WithItem(1, 0, Item(Wood, 1)));
            screen.Advance(Delay);

            screen.Spawn.PressSpawn();
            screen.Advance(Delay - 1f);
            Assert.That(screen.Hint.Shown, Is.Null);

            screen.Advance(1f);
            Assert.That(screen.Hint.Shown, Is.EqualTo((At(0, 0), At(1, 0))));
        }

        [Test]
        public void Hint_ShowsThePreferredMergeOntoALockedItem()
        {
            using var screen = GameScreen.For(BoardWithAPair().WithLockedItem(3, 3, Item(Stone, 1)));

            screen.Advance(Delay);

            Assert.That(screen.Hint.Shown, Is.EqualTo((At(4, 4), At(3, 3))));
        }

        [Test]
        public void Tap_HidesTheHint_AndRestartsTheIdleTime()
        {
            using var screen = GameScreen.For(BoardWithAPair());
            screen.Advance(Delay);

            screen.Selection.Tap(4, 4);

            Assert.That(screen.Hint.Shown, Is.Null);
            screen.Advance(Delay - 0.5f);
            Assert.That(screen.Hint.Shown, Is.Null);
            screen.Advance(0.5f);
            Assert.That(screen.Hint.Shown, Is.Not.Null);
        }

        [Test]
        public void SessionEvent_HidesTheHint_AndRestartsTheIdleTime()
        {
            using var screen = GameScreen.For(BoardWithAPair());
            screen.Advance(Delay);

            screen.Spawn.PressSpawn();

            Assert.That(screen.Hint.Shown, Is.Null);
            Assert.That(screen.Hint.HideCount, Is.EqualTo(1));
            screen.Advance(Delay - 0.5f);
            Assert.That(screen.Hint.Shown, Is.Null);
            screen.Advance(0.5f);
            Assert.That(screen.Hint.Shown, Is.Not.Null);
        }

        [Test]
        public void ActivityBeforeTheDelay_RestartsTheIdleTime()
        {
            using var screen = GameScreen.For(BoardWithAPair());
            screen.Advance(Delay - 0.5f);

            screen.Selection.Tap(0, 0);
            screen.Advance(0.5f);

            Assert.That(screen.Hint.Shown, Is.Null);
        }

        [Test]
        public void DragStart_HidesTheHint_AndNoHintAppearsDuringTheDrag()
        {
            using var screen = GameScreen.For(BoardWithAPair());
            screen.Advance(Delay);

            screen.Board.StartDrag(4, 4);
            screen.Board.MoveFingerTo(3, 3);
            screen.Advance(Delay * 10);

            Assert.That(screen.Hint.Shown, Is.Null);
            Assert.That(screen.Hint.ShowCount, Is.EqualTo(1));
        }

        [Test]
        public void RefusedDragOfALockedItem_StillPausesTheIdleTimeUntilRelease()
        {
            using var screen = GameScreen.For(BoardWithAPair().WithLockedItem(2, 2, Item(Wood, 3)));

            screen.Board.StartDrag(2, 2);
            screen.Advance(Delay);
            Assert.That(screen.Hint.Shown, Is.Null);

            screen.Board.Release(2, 3);
            screen.Advance(Delay);
            Assert.That(screen.Hint.Shown, Is.Not.Null);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DragReleased_RestartsTheIdleTime(bool releaseOffBoard)
        {
            using var screen = GameScreen.For(BoardWithAPair());
            screen.Board.StartDrag(4, 4);
            screen.Advance(Delay);

            if (releaseOffBoard)
                screen.Board.ReleaseOffBoard();
            else
                screen.Board.Release(4, 4);

            screen.Advance(Delay - 0.5f);
            Assert.That(screen.Hint.Shown, Is.Null);
            screen.Advance(0.5f);
            Assert.That(screen.Hint.Shown, Is.Not.Null);
        }

        [Test]
        public void DropThatMovesTheItem_RestartsTheIdleTime()
        {
            using var screen = GameScreen.For(BoardWithAPair());

            screen.Board.DragAndDrop(4, 4, 3, 3);
            screen.Advance(Delay - 0.5f);
            Assert.That(screen.Hint.Shown, Is.Null);

            screen.Advance(0.5f);
            Assert.That(screen.Hint.Shown, Is.EqualTo((At(0, 0), At(1, 0))));
        }

        [Test]
        public void DraggedItemRemovedByDiscard_EndsTheDrag()
        {
            using var screen = GameScreen.For(BoardWithAPair());
            screen.Selection.Tap(4, 4);
            screen.Board.StartDrag(4, 4);

            screen.Info.RequestDiscard();
            screen.Advance(Delay);

            Assert.That(screen.Hint.Shown, Is.EqualTo((At(0, 0), At(1, 0))));
        }

        [Test]
        public void DraggedItemRemovedByDelivery_EndsTheDrag()
        {
            using var screen = GameScreen.For(BoardWithAPair().WithOrders(1, new[] { Item(Stone, 1) }));
            screen.Board.StartDrag(4, 4);

            screen.Orders.RequestDelivery(0);
            screen.Advance(Delay);

            Assert.That(screen.Hint.Shown, Is.EqualTo((At(0, 0), At(1, 0))));
        }

        [Test]
        public void DeliveryOfOtherItems_DuringADrag_KeepsTheTimerPaused()
        {
            using var screen = GameScreen.For(BoardWithAPair().WithOrders(1, new[] { Item(Stone, 1) }));
            screen.Board.StartDrag(0, 0);

            screen.Orders.RequestDelivery(0);
            screen.Advance(Delay);

            Assert.That(screen.Hint.Shown, Is.Null);
        }

        [Test]
        public void Hint_ShowsAgainAfterAFurtherIdlePeriod()
        {
            using var screen = GameScreen.For(BoardWithAPair().WithItem(2, 0, Item(Stone, 2)).WithItem(3, 0, Item(Stone, 2)));
            screen.Advance(Delay);

            screen.Board.DragAndDrop(0, 0, 1, 0);
            screen.Advance(Delay);

            Assert.That(screen.Hint.ShowCount, Is.EqualTo(2));
            Assert.That(screen.Hint.Shown, Is.EqualTo((At(2, 0), At(3, 0))));
        }

        [Test]
        public void ShownHint_IsHiddenRightAfterTheBoardReactedToTheDragStart()
        {
            using var screen = GameScreen.For(BoardWithAPair());
            screen.Advance(Delay);
            screen.Log.Clear();

            screen.Board.DragAndDrop(0, 0, 1, 0);

            Assert.That(screen.Log.Entries[0], Is.EqualTo("Board.BeginDrag (0, 0)"));
            Assert.That(screen.Log.Entries[1], Is.EqualTo("Hint.HideMergeHint"));
            Assert.That(screen.Log.Only("Hint."), Is.EqualTo(new[] { "Hint.HideMergeHint" }));
        }

        [Test]
        public void AdvanceBeforeInitializeOrAfterDispose_DoesNothing()
        {
            var session = BoardWithAPair().Build();
            var log = new ViewLog();
            var hintView = new FakeMergeHintView(log);
            using var events = new SessionEventStream(session);
            var presenter = new MergeHintPresenter(
                session, events, new FakeBoardView(log), new FakeSelectionView(log), hintView, new MergeHintSettings(1f, 1f));

            presenter.Advance(5f);
            Assert.That(hintView.ShowCount, Is.Zero);

            presenter.Initialize();
            presenter.Dispose();
            presenter.Advance(5f);
            session.Spawn();

            Assert.That(log.Entries, Is.Empty);
        }

        [Test]
        public void Dispose_DoesNotHideAShownHint_ButStopsReacting()
        {
            var screen = GameScreen.For(BoardWithAPair());
            screen.Advance(Delay);

            screen.Dispose();
            screen.Selection.Tap(0, 0);
            screen.Session.Spawn();
            screen.Advance(Repeat * 3);

            Assert.That(screen.Hint.Shown, Is.Not.Null);
            Assert.That(screen.Hint.HideCount, Is.Zero);
            Assert.That(screen.Hint.ShowCount, Is.EqualTo(1));
        }

        [Test]
        public void Settings_KeepBothTimings()
        {
            var settings = new MergeHintSettings(4f, 2.5f);

            Assert.That(settings.IdleSeconds, Is.EqualTo(4f));
            Assert.That(settings.RepeatSeconds, Is.EqualTo(2.5f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Settings_RejectANonPositiveOrNonFiniteIdleDelay(float idleSeconds)
        {
            Assert.That(() => new MergeHintSettings(idleSeconds, 1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Settings_RejectANonPositiveOrNonFiniteRepeatInterval(float repeatSeconds)
        {
            Assert.That(() => new MergeHintSettings(1f, repeatSeconds), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
