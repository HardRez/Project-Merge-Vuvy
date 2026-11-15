using System.Collections;
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
    }
}
