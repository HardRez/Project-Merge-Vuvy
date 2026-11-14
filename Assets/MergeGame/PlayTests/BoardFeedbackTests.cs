using System.Collections;
using System.Linq;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;
using MergeGame.Data.Visuals;
using MergeGame.PlayTests.Support;
using MergeGame.Presentation.Hints;
using MergeGame.Presentation.Notices;
using MergeGame.View.Board;
using MergeGame.View.Hints;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MergeGame.PlayTests
{
    /// <summary>The idle merge hint, the merge-target shine and sand-covered closed cells, through the real Game scene.</summary>
    public sealed class BoardFeedbackTests
    {
        private const float AnimationSettleSeconds = 1.2f;

        /// <summary>Longer than one hint pulse, shorter than the default repeat interval.</summary>
        private const float HintPulseSeconds = 1.8f;

        private GameScreenDriver game;

        [UnityTest]
        public IEnumerator IdleHint_PulsesOnceOnAMergeablePair_ThenAgainAfterTheRepeatInterval()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            var hintView = Object.FindAnyObjectByType<MergeHintView>();
            var presenter = game.Resolve<MergeHintPresenter>();
            var settings = game.Resolve<MergeHintSettings>();

            // Lets the configured idle time pass at once instead of waiting for it.
            presenter.Advance(settings.IdleSeconds);
            yield return null;

            Assert.That(hintView.IsPlaying, Is.True);
            var hinted = game.Cells().Where(cell => game.Board.TryGetItem(cell, out var item) && item.Accent.Current == AccentKind.MergeHint).ToList();
            Assert.That(hinted.Count, Is.EqualTo(2));
            game.Board.TryGetItem(hinted[0], out var first);
            game.Board.TryGetItem(hinted[1], out var second);
            Assert.That(first.ItemId, Is.EqualTo(second.ItemId), "The hint points at two items that do not merge.");

            // The pulse ends on its own, well before the next check is due.
            yield return GameScreenDriver.WaitSeconds(HintPulseSeconds);
            Assert.That(hintView.IsPlaying, Is.False);
            AssertAtRest(first, second);

            presenter.Advance(settings.RepeatSeconds);
            yield return null;
            Assert.That(hintView.IsPlaying, Is.True, "No second pulse after the repeat interval.");

            game.Tap(game.FirstEmptyCell());
            yield return null;
            Assert.That(hintView.IsPlaying, Is.False, "A player action did not cancel the pulse.");
            AssertAtRest(first, second);
        }

        [UnityTest]
        public IEnumerator DraggingOverAMergeTarget_MakesItShine_UntilThePointerLeavesIt()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindMergePair(out var from, out var to), Is.True);
            game.Board.TryGetItem(to, out var target);
            var elsewhere = game.FirstEmptyCell();

            PointerGesture held = null;
            yield return game.DragOnto(from, to, gesture => held = gesture);
            Assert.That(target.Accent.Current, Is.EqualTo(AccentKind.MergeTarget));

            held.MoveTo(game.ScreenPointOf(elsewhere));
            yield return null;
            Assert.That(target.Accent.Current, Is.EqualTo(AccentKind.None));
            Assert.That(target.Accent.transform.localScale, Is.EqualTo(Vector3.one));

            held.Release();
        }

        [UnityTest]
        public IEnumerator OpeningClosedCells_AreCoveredEdgeToEdge_AndShowNothingOfTheirContent()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);

            var closed = game.Cells().Where(cell => game.Session.Board.TryGetItem(cell, out var item) && item.IsHidden).ToList();
            Assert.That(closed, Is.Not.Empty, "The shipped layout has no closed cells.");
            foreach (var cell in closed)
            {
                Assert.That(game.Board.TryGetCover(cell, out var cover), Is.True, $"Cell {cell} has no cover.");
                AssertSameWorldRect((RectTransform)cover.transform, game.Board.GetCellTile(cell), cell);
                Assert.That(game.Board.TryGetItem(cell, out var hidden), Is.True);
                Assert.That(hidden.IsHidden, Is.True, $"Cell {cell}");
                Assert.That(hidden.DisplayedIcon, Is.Null, $"The closed cell {cell} shows an item icon.");
                Assert.That(hidden.ItemId, Is.EqualTo(default(ItemId)), $"The closed cell {cell} knows its content.");
            }
        }

        [UnityTest]
        public IEnumerator FreeingTheWebbedNeighbourOfAClosedCell_BlowsTheSandAway_AndRevealsAWebbedItem()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindCoverOpeningMerge(out var from, out var to), Is.True, "No merge frees a webbed item next to a closed cell.");
            var closed = GameScreenDriver.Neighbours(to).First(game.IsCovered);

            yield return game.DragAndDrop(from, to);
            yield return GameScreenDriver.WaitSeconds(AnimationSettleSeconds);

            var expected = game.Session.Board.GetItem(closed);
            Assert.That(expected.IsHidden, Is.False);
            Assert.That(game.Board.TryGetCover(closed, out _), Is.False, "The sand is still there.");
            Assert.That(game.Board.TryGetItem(closed, out var revealed), Is.True);
            Assert.That(revealed.IsHidden, Is.False);
            Assert.That(revealed.IsLocked, Is.True);
            Assert.That(revealed.ItemId, Is.EqualTo(expected.ItemId));
            Assert.That(revealed.DisplayedIcon, Is.SameAs(game.Resolve<IItemVisuals>().GetIcon(expected.ItemId)));
        }

        [UnityTest]
        public IEnumerator AClosedCell_CannotBeDragged()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            var closed = game.Cells().First(game.IsCovered);
            game.Board.TryGetItem(closed, out var hidden);

            yield return game.DragAndDrop(closed, game.FirstEmptyCell());
            yield return GameScreenDriver.WaitSeconds(0.5f);

            Assert.That(game.Board.TryGetCover(closed, out _), Is.True);
            Assert.That(game.Board.TryGetItem(closed, out var stillThere), Is.True);
            Assert.That(stillThere, Is.SameAs(hidden));
            Assert.That(stillThere.IsHidden, Is.True);
            Assert.That(stillThere.IsLifted, Is.False);
            Assert.That(game.Notices.ShownNotice, Is.EqualTo(Notice.ItemIsHidden));
        }

        private static void AssertAtRest(params ItemView[] items)
        {
            foreach (var item in items)
            {
                Assert.That(item.Accent.Current, Is.EqualTo(AccentKind.None));
                Assert.That(item.Accent.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(item.Accent.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(item.Accent.transform.localRotation, Is.EqualTo(Quaternion.identity));
            }
        }

        private static void AssertSameWorldRect(RectTransform actual, RectTransform expected, GridPosition cell)
        {
            var actualCorners = new Vector3[4];
            var expectedCorners = new Vector3[4];
            actual.GetWorldCorners(actualCorners);
            expected.GetWorldCorners(expectedCorners);
            for (var i = 0; i < 4; i++)
                Assert.That(Vector3.Distance(actualCorners[i], expectedCorners[i]), Is.LessThan(0.001f), $"Cell {cell}, corner {i}");
        }
    }
}
