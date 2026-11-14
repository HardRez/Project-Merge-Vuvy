using System;
using MergeGame.View.Board.Sand;
using NUnit.Framework;

namespace MergeGame.View.Tests
{
    public sealed class SandPourTests
    {
        /// <summary>The cover's default grid.</summary>
        private const int Size = 42;

        private const float FrameSeconds = 1f / 240f;

        private readonly SandPourSettings settings = new SandPourSettings();

        [Test]
        public void AtTheStart_EveryGrainRestsAtItsCell()
        {
            var pour = Full(seed: 3);

            for (var grain = 0; grain < pour.Count; grain++)
            {
                pour.Locate(grain, 0f, out var x, out var y, out _, out var speed);
                var cell = pour.RestCell(grain);
                Assert.That(x, Is.EqualTo(cell % Size + 0.5f));
                Assert.That(y, Is.EqualTo(cell / Size + 0.5f));
                Assert.That(speed, Is.EqualTo(0f));
            }
        }

        [Test]
        public void BeforeItsReleaseTime_AGrainIsExactlyAtRest()
        {
            var pour = Full(seed: 3);

            for (var grain = 0; grain < pour.Count; grain++)
            {
                pour.Locate(grain, 0f, out var restX, out var restY, out _, out _);
                pour.Locate(grain, pour.ReleaseTime(grain), out var x, out var y, out _, out _);
                Assert.That((x, y), Is.EqualTo((restX, restY)), $"Grain {grain} moved before letting go.");
            }
        }

        [Test]
        public void SameSeed_PoursIdentically_DifferentSeedsDiffer()
        {
            var first = Full(seed: 7);
            var twin = Full(seed: 7);
            var other = Full(seed: 8);

            var differs = false;
            for (var grain = 0; grain < first.Count; grain++)
            {
                first.Locate(grain, 0.3f, out var x, out var y, out _, out _);
                twin.Locate(grain, 0.3f, out var twinX, out var twinY, out _, out _);
                other.Locate(grain, 0.3f, out var otherX, out var otherY, out _, out _);
                Assert.That((twinX, twinY), Is.EqualTo((x, y)));
                differs |= otherX != x || otherY != y;
            }

            Assert.That(differs, Is.True);
        }

        [Test]
        public void GrainsNeverRise_AndMoveContinuously()
        {
            var pour = Full(seed: 11);

            for (var grain = 0; grain < pour.Count; grain++)
            {
                pour.Locate(grain, 0f, out var lastX, out var lastY, out _, out _);
                for (var time = FrameSeconds; time <= pour.Duration; time += FrameSeconds)
                {
                    pour.Locate(grain, time, out var x, out var y, out _, out var speed);
                    Assert.That(y, Is.LessThanOrEqualTo(lastY), $"Grain {grain} rose at {time:0.000} s.");

                    // Within one frame a grain covers at most its speed times the frame, plus its sideways drift.
                    var limit = (speed + settings.Drift * 1.3f) * FrameSeconds + 1e-4f;
                    Assert.That(Math.Abs(y - lastY) + Math.Abs(x - lastX), Is.LessThanOrEqualTo(limit * 1.5f), $"Grain {grain} jumped at {time:0.000} s.");
                    lastX = x;
                    lastY = y;
                }
            }
        }

        [Test]
        public void EveryGrain_LetsGoWithinTheReleaseWave_AndIsOutOfTheCellByTheEnd()
        {
            var pour = Full(seed: 5);

            for (var grain = 0; grain < pour.Count; grain++)
            {
                Assert.That(pour.ReleaseTime(grain), Is.InRange(0f, settings.ReleaseWaveSeconds), $"Grain {grain}");
                Assert.That(pour.IsGone(grain, settings.TotalSeconds), Is.True, $"Grain {grain} is still in the cell at the end.");
            }

            Assert.That(pour.LiveCount(settings.TotalSeconds), Is.EqualTo(0));
            Assert.That(pour.LiveCount(0f), Is.EqualTo(pour.Count));
        }

        [Test]
        public void TheLiveCount_OnlyDrops()
        {
            var pour = Full(seed: 9);
            var last = pour.Count;

            for (var time = 0f; time <= pour.Duration; time += 1f / 60f)
            {
                var live = pour.LiveCount(time);
                Assert.That(live, Is.LessThanOrEqualTo(last), $"Grains reappeared at {time:0.000} s.");
                last = live;
            }
        }

        [Test]
        public void EmptyCells_HaveNoGrains()
        {
            var occupied = new bool[Size * Size];
            occupied[5] = true;
            occupied[Size * Size - 1] = true;

            Assert.That(new SandPour(Size, Size, occupied, 1, settings).Count, Is.EqualTo(2));
        }

        private SandPour Full(int seed)
        {
            var occupied = new bool[Size * Size];
            for (var i = 0; i < occupied.Length; i++)
                occupied[i] = true;
            return new SandPour(Size, Size, occupied, seed, settings);
        }
    }
}
