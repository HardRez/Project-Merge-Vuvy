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
    /// The merge glow round a merge target follows the animation frame by frame: nothing may jump, not when the target's
    /// pulse starts a new cycle and not when the glow itself starts over.
    /// </summary>
    public sealed class MergeGlowTests
    {
        /// <summary>Several pulses of the merge target (0.8 s each), so the loops restart more than once.</summary>
        private const float WatchSeconds = 2.6f;

        /// <summary>Frames shorter than this count as this long, so a fast editor frame never looks like a jump.</summary>
        private const float MinFrameSeconds = 1f / 120f;

        // Fastest change per second each value may show; the animation stays well below, a snap is far above.
        private const float MaxScaleRate = 4f;
        private const float MaxDegreesRate = 120f;
        private const float MaxAlphaRate = 7f;

        /// <summary>Fastest a star may move, in cell widths per second: its orbit is far slower, a snap far faster.</summary>
        private const float MaxStarCellsRate = 3f;

        /// <summary>The glow fades in; on its first frames after a (re)start it must still be nearly invisible.</summary>
        private const float MaxFirstFrameAlpha = 0.3f;

        private GameScreenDriver game;

        [UnityTest]
        public IEnumerator WhileHoveringAMergeTarget_TheGlowNeverJumps()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindMergePair(out var from, out var to), Is.True);
            game.Board.TryGetItem(to, out var target);

            PointerGesture held = null;
            yield return game.DragOnto(from, to, gesture => held = gesture);
            Assert.That(target.Accent.Current, Is.EqualTo(AccentKind.MergeTarget));
            var glow = game.Board.MergeGlow;
            Assert.That(glow.IsShown, Is.True, "No glow round the merge target.");
            var cellWidth = ((RectTransform)glow.transform).rect.width;

            var jumps = new List<string>();
            var previous = Sample(target.Accent, glow);
            for (var elapsed = 0f; elapsed < WatchSeconds; elapsed += Time.unscaledDeltaTime)
            {
                yield return null;
                var current = Sample(target.Accent, glow);
                jumps.AddRange(Jumps(previous, current, Mathf.Max(Time.unscaledDeltaTime, MinFrameSeconds), cellWidth, elapsed));
                previous = current;
            }

            held.Release();
            Assert.That(jumps, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ReenteringAMergeTarget_FadesTheGlowInInsteadOfPoppingIt()
        {
            yield return GameScreenDriver.OpenGame(driver => game = driver);
            Assert.That(game.TryFindMergePair(out var from, out var to), Is.True);
            game.Board.TryGetItem(to, out var target);
            var elsewhere = game.FirstEmptyCell();
            var glow = game.Board.MergeGlow;

            PointerGesture held = null;
            yield return game.DragOnto(from, to, gesture => held = gesture);
            yield return GameScreenDriver.WaitSeconds(0.5f);
            Assert.That(glow.Alpha, Is.GreaterThan(0.9f), "The glow did not fade in.");

            held.MoveTo(game.ScreenPointOf(elsewhere));
            yield return null;
            Assert.That(glow.IsShown, Is.False, "The glow stayed after the pointer left the target.");

            held.MoveTo(game.ScreenPointOf(to));
            yield return null;
            Assert.That(target.Accent.Current, Is.EqualTo(AccentKind.MergeTarget));
            Assert.That(glow.IsShown, Is.True);
            Assert.That(glow.Alpha, Is.LessThanOrEqualTo(MaxFirstFrameAlpha), "The glow popped back in.");

            held.Release();
        }

        private static Snapshot Sample(ItemAccent accent, MergeTargetGlow glow) => new Snapshot
        {
            AccentScale = accent.transform.localScale.x,
            GlowAlpha = glow.Alpha,
            RaysDegrees = glow.Rays.localEulerAngles.z,
            RingScale = glow.Ring.rectTransform.localScale.x,
            RingAlpha = glow.Ring.color.a,
            Stars = glow.Stars.Select(star => (star.rectTransform.anchoredPosition, star.rectTransform.localScale.x, star.color.a)).ToArray()
        };

        private static IEnumerable<string> Jumps(Snapshot before, Snapshot after, float seconds, float cellWidth, float at)
        {
            if (Mathf.Abs(after.AccentScale - before.AccentScale) / seconds > MaxScaleRate)
                yield return $"{at:0.00}s: the target's scale jumped {before.AccentScale:0.000} -> {after.AccentScale:0.000}";
            if (Mathf.Abs(after.GlowAlpha - before.GlowAlpha) / seconds > MaxAlphaRate)
                yield return $"{at:0.00}s: the glow's alpha jumped {before.GlowAlpha:0.00} -> {after.GlowAlpha:0.00}";
            if (Mathf.Abs(Mathf.DeltaAngle(before.RaysDegrees, after.RaysDegrees)) / seconds > MaxDegreesRate)
                yield return $"{at:0.00}s: the rays turned {before.RaysDegrees:0.0} -> {after.RaysDegrees:0.0} degrees";
            if (Mathf.Abs(after.RingScale - before.RingScale) / seconds > MaxScaleRate)
                yield return $"{at:0.00}s: the ring's scale jumped {before.RingScale:0.000} -> {after.RingScale:0.000}";
            if (Mathf.Abs(after.RingAlpha - before.RingAlpha) / seconds > MaxAlphaRate)
                yield return $"{at:0.00}s: the ring's alpha jumped {before.RingAlpha:0.00} -> {after.RingAlpha:0.00}";

            for (var i = 0; i < after.Stars.Length; i++)
            {
                var (positionBefore, scaleBefore, alphaBefore) = before.Stars[i];
                var (positionAfter, scaleAfter, alphaAfter) = after.Stars[i];
                if (Vector2.Distance(positionAfter, positionBefore) / cellWidth / seconds > MaxStarCellsRate)
                    yield return $"{at:0.00}s: star {i + 1} jumped {positionBefore} -> {positionAfter}";
                if (Mathf.Abs(scaleAfter - scaleBefore) / seconds > MaxScaleRate)
                    yield return $"{at:0.00}s: star {i + 1} scale jumped {scaleBefore:0.000} -> {scaleAfter:0.000}";
                if (Mathf.Abs(alphaAfter - alphaBefore) / seconds > MaxAlphaRate)
                    yield return $"{at:0.00}s: star {i + 1} alpha jumped {alphaBefore:0.00} -> {alphaAfter:0.00}";
            }
        }

        private struct Snapshot
        {
            public float AccentScale;
            public float GlowAlpha;
            public float RaysDegrees;
            public float RingScale;
            public float RingAlpha;
            public (Vector2 position, float scale, float alpha)[] Stars;
        }
    }
}
