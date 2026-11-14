using System.Collections;
using System.Linq;
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
    /// <summary>The idle merge hint, the merge-target shine and closed boxes, through the real Game scene.</summary>
    public sealed class BoardFeedbackTests
    {
        private const float AnimationSettleSeconds = 1.2f;

        private GameScreenDriver game;

        [UnityTest]
        public IEnumerator IdleHint_AppearsOnAMergeablePairAfterTheDelay_AndGoesAwayOnTheNextAction()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            var hintView = Object.FindAnyObjectByType<MergeHintView>();

            // Lets the configured idle time pass at once instead of waiting for it.
            game.Resolve<MergeHintPresenter>().Advance(game.Resolve<MergeHintSettings>().IdleSeconds);
            yield return null;

            Assert.That(hintView.IsShown, Is.True);
            var hinted = game.Cells().Where(cell => game.Board.TryGetItem(cell, out var item) && item.Accent.Current == AccentKind.MergeHint).ToList();
            Assert.That(hinted.Count, Is.EqualTo(2));
            game.Board.TryGetItem(hinted[0], out var first);
            game.Board.TryGetItem(hinted[1], out var second);
            Assert.That(first.ItemId, Is.EqualTo(second.ItemId), "The hint points at two items that do not merge.");

            game.Tap(game.FirstEmptyCell());
            yield return null;

            Assert.That(hintView.IsShown, Is.False);
            foreach (var item in new[] { first, second })
            {
                Assert.That(item.Accent.Current, Is.EqualTo(AccentKind.None));
                Assert.That(item.Accent.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(item.Accent.transform.localScale, Is.EqualTo(Vector3.one));
            }
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
        public IEnumerator OpeningBoxes_ShowNothingOfTheirContent()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);

            var boxes = game.Cells().Where(cell => game.Session.Board.TryGetItem(cell, out var item) && item.IsHidden).ToList();
            Assert.That(boxes, Is.Not.Empty, "The shipped layout has no boxes.");
            foreach (var cell in boxes)
            {
                Assert.That(game.Board.TryGetItem(cell, out var box), Is.True);
                Assert.That(box.IsHidden, Is.True, $"Cell {cell}");
                Assert.That(box.DisplayedIcon, Is.Null, $"The box at {cell} shows an item icon.");
                Assert.That(box.ItemId, Is.EqualTo(default(ItemId)), $"The box at {cell} knows its content.");
            }
        }

        [UnityTest]
        public IEnumerator FreeingTheWebbedNeighbourOfABox_RevealsItAsAWebbedItem()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindBoxOpeningMerge(out var from, out var to), Is.True, "No merge frees a webbed item next to a box.");
            var box = GameScreenDriver.Neighbours(to).First(game.IsBox);

            yield return game.DragAndDrop(from, to);
            yield return GameScreenDriver.WaitSeconds(AnimationSettleSeconds);

            var expected = game.Session.Board.GetItem(box);
            Assert.That(expected.IsHidden, Is.False);
            Assert.That(game.Board.TryGetItem(box, out var revealed), Is.True);
            Assert.That(revealed.IsHidden, Is.False);
            Assert.That(revealed.IsLocked, Is.True);
            Assert.That(revealed.ItemId, Is.EqualTo(expected.ItemId));
            Assert.That(revealed.DisplayedIcon, Is.SameAs(game.Resolve<IItemVisuals>().GetIcon(expected.ItemId)));
        }

        [UnityTest]
        public IEnumerator ABox_CannotBeDragged()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            var box = game.Cells().First(game.IsBox);
            game.Board.TryGetItem(box, out var boxView);

            yield return game.DragAndDrop(box, game.FirstEmptyCell());
            yield return GameScreenDriver.WaitSeconds(0.5f);

            Assert.That(game.Board.TryGetItem(box, out var stillThere), Is.True);
            Assert.That(stillThere, Is.SameAs(boxView));
            Assert.That(stillThere.IsHidden, Is.True);
            Assert.That(stillThere.IsLifted, Is.False);
            Assert.That(game.Notices.ShownNotice, Is.EqualTo(Notice.ItemIsHidden));
        }
    }
}
