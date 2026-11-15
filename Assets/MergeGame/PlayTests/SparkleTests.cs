using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MergeGame.PlayTests.Support;
using MergeGame.View.Board;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MergeGame.PlayTests
{
    /// <summary>
    /// The sparkles over a merge target follow the animation frame by frame: nothing may jump, not when the target's
    /// pulse starts a new cycle and not when the accent itself starts over.
    /// </summary>
    public sealed class SparkleTests
    {
        /// <summary>Several pulses of the merge target (0.8 s each), so the loop restarts more than once.</summary>
        private const float WatchSeconds = 2.6f;

        /// <summary>Frames shorter than this count as this long, so a fast editor frame never looks like a jump.</summary>
        private const float MinFrameSeconds = 1f / 120f;

        // Fastest change per second each value may show; the animation stays well below, a snap is far above.
        private const float MaxScaleRate = 4f;
        private const float MaxDegreesRate = 120f;
        private const float MaxAlphaRate = 7f;

        /// <summary>The cluster fades in; on its first frames after a (re)start it must still be nearly invisible.</summary>
        private const float MaxFirstFrameAlpha = 0.3f;

        private GameScreenDriver game;

        [UnityTest]
        public IEnumerator WhileHoveringAMergeTarget_TheSparklesNeverJump()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindMergePair(out var from, out var to), Is.True);
            game.Board.TryGetItem(to, out var target);

            PointerGesture held = null;
            yield return game.DragOnto(from, to, gesture => held = gesture);
            Assert.That(target.Accent.Current, Is.EqualTo(AccentKind.MergeTarget));

            var jumps = new List<string>();
            var previous = Sample(target.Accent);
            for (var elapsed = 0f; elapsed < WatchSeconds; elapsed += Time.unscaledDeltaTime)
            {
                yield return null;
                var current = Sample(target.Accent);
                jumps.AddRange(Jumps(previous, current, Mathf.Max(Time.unscaledDeltaTime, MinFrameSeconds), elapsed));
                previous = current;
            }

            held.Release();
            Assert.That(jumps, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ReenteringAMergeTarget_FadesTheSparklesInInsteadOfPoppingThem()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindMergePair(out var from, out var to), Is.True);
            game.Board.TryGetItem(to, out var target);
            var elsewhere = game.FirstEmptyCell();

            PointerGesture held = null;
            yield return game.DragOnto(from, to, gesture => held = gesture);
            yield return GameScreenDriver.WaitSeconds(0.5f);
            Assert.That(target.Accent.Sparkles.alpha, Is.GreaterThan(0.9f), "The sparkles did not fade in.");

            held.MoveTo(game.ScreenPointOf(elsewhere));
            yield return null;
            Assert.That(target.Accent.Sparkles.gameObject.activeSelf, Is.False);

            held.MoveTo(game.ScreenPointOf(to));
            yield return null;
            Assert.That(target.Accent.Current, Is.EqualTo(AccentKind.MergeTarget));
            Assert.That(target.Accent.Sparkles.alpha, Is.LessThanOrEqualTo(MaxFirstFrameAlpha), "The sparkles popped back in.");

            held.Release();
        }

        private static Snapshot Sample(ItemAccent accent) => new Snapshot
        {
            AccentScale = accent.transform.localScale.x,
            ClusterAlpha = accent.Sparkles.alpha,
            Stars = accent.Twinkles.Select(star => (star.rectTransform.localScale.x, star.rectTransform.localEulerAngles.z, star.color.a)).ToArray()
        };

        private static IEnumerable<string> Jumps(Snapshot before, Snapshot after, float seconds, float at)
        {
            if (Mathf.Abs(after.AccentScale - before.AccentScale) / seconds > MaxScaleRate)
                yield return $"{at:0.00}s: the target's scale jumped {before.AccentScale:0.000} -> {after.AccentScale:0.000}";
            if (Mathf.Abs(after.ClusterAlpha - before.ClusterAlpha) / seconds > MaxAlphaRate)
                yield return $"{at:0.00}s: the sparkles' alpha jumped {before.ClusterAlpha:0.00} -> {after.ClusterAlpha:0.00}";

            for (var i = 0; i < after.Stars.Length; i++)
            {
                var (scaleBefore, degreesBefore, alphaBefore) = before.Stars[i];
                var (scaleAfter, degreesAfter, alphaAfter) = after.Stars[i];
                if (Mathf.Abs(scaleAfter - scaleBefore) / seconds > MaxScaleRate)
                    yield return $"{at:0.00}s: star {i + 1} scale jumped {scaleBefore:0.000} -> {scaleAfter:0.000}";
                if (Mathf.Abs(Mathf.DeltaAngle(degreesBefore, degreesAfter)) / seconds > MaxDegreesRate)
                    yield return $"{at:0.00}s: star {i + 1} turned {degreesBefore:0.0} -> {degreesAfter:0.0} degrees";
                if (Mathf.Abs(alphaAfter - alphaBefore) / seconds > MaxAlphaRate)
                    yield return $"{at:0.00}s: star {i + 1} alpha jumped {alphaBefore:0.00} -> {alphaAfter:0.00}";
            }
        }

        private struct Snapshot
        {
            public float AccentScale;
            public float ClusterAlpha;
            public (float scale, float degrees, float alpha)[] Stars;
        }
    }
}
