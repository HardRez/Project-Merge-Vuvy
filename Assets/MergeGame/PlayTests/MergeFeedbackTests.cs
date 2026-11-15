using System.Collections;
using System.Linq;
using MergeGame.Core.Grid;
using MergeGame.PlayTests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MergeGame.PlayTests
{
    /// <summary>A merge reads as a hit: the new item pops past its size, squashes and stretches, and a ring spreads out.</summary>
    public sealed class MergeFeedbackTests
    {
        private const float WatchSeconds = 1f;

        private GameScreenDriver game;

        [UnityTest]
        public IEnumerator AMerge_PopsTheNewItem_WithSquashStretchAndARing_ThenRestsExactly()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            GridPosition from, to;
            for (var spawns = 0; !game.TryFindMergePair(out from, out to) && spawns < 20; spawns++)
            {
                game.ClickSpawn();
                yield return null;
            }

            Assert.That(game.TryFindMergePair(out from, out to), Is.True);
            yield return GameScreenDriver.WaitSeconds(0.6f);
            game.Board.TryGetItem(to, out var target);

            yield return game.DragAndDrop(from, to);

            var peak = 0f;
            var widestSquash = 0f;
            var ringSpread = 0f;
            for (var elapsed = 0f; elapsed < WatchSeconds; elapsed += Time.unscaledDeltaTime)
            {
                var scale = target.Body.localScale;
                peak = Mathf.Max(peak, Mathf.Max(scale.x, scale.y));
                widestSquash = Mathf.Max(widestSquash, Mathf.Abs(scale.x - scale.y));
                if (target.MergeRing.gameObject.activeInHierarchy && target.MergeRing.color.a > 0.05f)
                    ringSpread = Mathf.Max(ringSpread, target.MergeRing.rectTransform.localScale.x);
                yield return null;
            }

            Assert.That(peak, Is.GreaterThan(1.15f), "The new item did not pop past its size.");
            Assert.That(widestSquash, Is.GreaterThan(0.05f), "The new item never squashed or stretched.");
            Assert.That(ringSpread, Is.GreaterThan(1.2f), "No ring spread out from the merge.");

            Assert.That(target.Body.localScale, Is.EqualTo(Vector3.one), "The item did not come to rest.");
            Assert.That(target.MergeRing.gameObject.activeSelf, Is.False, "The ring stayed on screen.");
        }

        [UnityTest]
        public IEnumerator LockedBubbles_BreatheSmoothly_AndNotInStep()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            var bubbles = game.Cells()
                .Select(cell => game.Board.TryGetItem(cell, out var item) ? item : null)
                .Where(item => item != null && item.LockOverlay.gameObject.activeInHierarchy)
                .Select(item => item.LockOverlay.rectTransform.parent)
                .ToList();
            Assert.That(bubbles.Count, Is.GreaterThanOrEqualTo(2), "The opening layout has fewer than two bubbles.");

            var lowest = 1f;
            var highest = 1f;
            var everApart = false;
            var previous = bubbles[0].localScale.x;
            for (var elapsed = 0f; elapsed < 2.6f; elapsed += Time.unscaledDeltaTime)
            {
                yield return null;
                var scale = bubbles[0].localScale;
                lowest = Mathf.Min(lowest, scale.x);
                highest = Mathf.Max(highest, scale.x);
                Assert.That(Mathf.Abs(scale.x - previous) / Mathf.Max(Time.unscaledDeltaTime, 1f / 120f), Is.LessThan(1f), "The bubble jumped.");
                previous = scale.x;
                everApart |= Mathf.Abs(scale.x - bubbles[1].localScale.x) > 0.005f;
            }

            Assert.That(highest - lowest, Is.GreaterThan(0.03f), "The bubble does not breathe.");
            Assert.That(highest, Is.LessThan(1.1f), "The breathing is more than a gentle swell.");
            Assert.That(everApart, Is.True, "Two bubbles breathe in step.");
        }

        [UnityTest]
        public IEnumerator FreeingABubbledItem_SwellsAndBurstsTheBubble_AndTheItemComesOutInItsOwnColours()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindCoverOpeningMerge(out var from, out var to), Is.True, "No merge frees a bubbled item.");
            game.Board.TryGetItem(to, out var target);
            Assert.That(target.LockOverlay.gameObject.activeSelf, Is.True, "The locked item has no bubble.");
            Assert.That(target.DisplayedIconColor, Is.Not.EqualTo(Color.white), "A locked item should look a little darker.");

            yield return game.DragAndDrop(from, to);

            var swell = 0f;
            var burst = false;
            for (var elapsed = 0f; elapsed < WatchSeconds; elapsed += Time.unscaledDeltaTime)
            {
                if (target.LockOverlay.gameObject.activeInHierarchy)
                    swell = Mathf.Max(swell, target.LockOverlay.rectTransform.localScale.x);
                burst |= target.BubblePop.IsPlaying;
                yield return null;
            }

            Assert.That(swell, Is.GreaterThan(1.03f), "The bubble did not swell before bursting.");
            Assert.That(burst, Is.True, "The bubble did not burst.");
            Assert.That(target.BubblePop.gameObject.activeSelf, Is.False, "The burst stayed on screen.");
            Assert.That(target.LockOverlay.gameObject.activeSelf, Is.False, "The freed item still has its bubble.");
            Assert.That(target.DisplayedIconColor, Is.EqualTo(Color.white), "The freed item kept the locked tint.");
        }
    }
}
