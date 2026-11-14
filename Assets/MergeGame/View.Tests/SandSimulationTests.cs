using System.Collections.Generic;
using MergeGame.View.Board.Sand;
using NUnit.Framework;

namespace MergeGame.View.Tests
{
    public sealed class SandSimulationTests
    {
        private const int Size = 64;

        /// <summary>Generous: the default tuning empties a full cell in roughly 110 steps.</summary>
        private const int StepsToEmpty = 400;

        [Test]
        public void SameSeed_PoursIdentically()
        {
            var first = Full(seed: 7);
            var second = Full(seed: 7);

            for (var step = 0; step < 60; step++)
            {
                first.Step();
                second.Step();
            }

            Assert.That(Snapshot(first), Is.EqualTo(Snapshot(second)));
        }

        [Test]
        public void DifferentSeeds_PourDifferently()
        {
            var first = Full(seed: 7);
            var second = Full(seed: 8);

            for (var step = 0; step < 60; step++)
            {
                first.Step();
                second.Step();
            }

            Assert.That(Snapshot(first), Is.Not.EqualTo(Snapshot(second)));
        }

        [Test]
        public void GrainsOnlyMoveDown_AndOnlyLeaveThroughTheOpenFloor()
        {
            var simulation = Full(seed: 3);
            var positions = Positions(simulation);
            var settings = new SandPourSettings();

            while (!simulation.IsEmpty && simulation.StepCount < StepsToEmpty)
            {
                simulation.Step();
                var next = Positions(simulation);
                Assert.That(next.Count, Is.EqualTo(simulation.GrainCount), "A grain was duplicated or lost track of.");
                Assert.That(next.Count, Is.LessThanOrEqualTo(positions.Count), "Grains appeared.");

                foreach (var (grain, before) in positions)
                {
                    if (next.TryGetValue(grain, out var after))
                    {
                        Assert.That(after.y, Is.LessThanOrEqualTo(before.y), $"Grain {grain} moved up at step {simulation.StepCount}.");
                        Assert.That(System.Math.Abs(after.x - before.x), Is.LessThanOrEqualTo(1), $"Grain {grain} jumped sideways.");
                        continue;
                    }

                    // Gone: it can only have fallen out of the bottom rows through an open floor column next to it.
                    Assert.That(before.y, Is.LessThanOrEqualTo((int)settings.MaxSpeed + 1), $"Grain {grain} vanished mid-cell.");
                    Assert.That(IsFloorOpenNear(simulation, before.x), Is.True, $"Grain {grain} left through a closed floor.");
                }

                positions = next;
            }
        }

        [Test]
        public void AFullCell_IsEmptyWithinTheStepBudget()
        {
            var simulation = Full(seed: 11);

            while (!simulation.IsEmpty && simulation.StepCount < StepsToEmpty)
                simulation.Step();

            Assert.That(simulation.IsEmpty, Is.True, $"{simulation.GrainCount} grains left after {StepsToEmpty} steps.");
        }

        [Test]
        public void TheFloorOpensAsNarrowHolesFirst_ThenCompletely()
        {
            var settings = new SandPourSettings();
            var simulation = Full(seed: 5);
            for (var step = 0; step < settings.HolesOpenStep; step++)
                simulation.Step();

            var openAtFirst = CountOpenColumns(simulation);
            Assert.That(openAtFirst, Is.InRange(1, settings.HoleCount));

            for (var step = 0; step < settings.WidenSteps; step++)
                simulation.Step();
            Assert.That(CountOpenColumns(simulation), Is.EqualTo(Size));
        }

        [Test]
        public void EmptyCells_StayEmpty()
        {
            var occupied = new bool[Size * Size];
            occupied[(Size - 1) * Size + Size / 2] = true;
            var simulation = new SandSimulation(Size, Size, occupied, 1, new SandPourSettings());

            Assert.That(simulation.GrainCount, Is.EqualTo(1));
            simulation.Step();
            Assert.That(simulation.GrainCount, Is.EqualTo(1));
        }

        private static SandSimulation Full(int seed)
        {
            var occupied = new bool[Size * Size];
            for (var i = 0; i < occupied.Length; i++)
                occupied[i] = true;
            return new SandSimulation(Size, Size, occupied, seed, new SandPourSettings());
        }

        private static ushort[] Snapshot(SandSimulation simulation)
        {
            var cells = new ushort[simulation.Width * simulation.Height];
            for (var y = 0; y < simulation.Height; y++)
            {
                for (var x = 0; x < simulation.Width; x++)
                    cells[y * simulation.Width + x] = simulation.GrainAt(x, y);
            }

            return cells;
        }

        private static Dictionary<ushort, (int x, int y)> Positions(SandSimulation simulation)
        {
            var positions = new Dictionary<ushort, (int x, int y)>();
            for (var y = 0; y < simulation.Height; y++)
            {
                for (var x = 0; x < simulation.Width; x++)
                {
                    var grain = simulation.GrainAt(x, y);
                    if (grain != SandSimulation.Empty)
                        positions.Add(grain, (x, y));
                }
            }

            return positions;
        }

        private static bool IsFloorOpenNear(SandSimulation simulation, int x) =>
            simulation.IsFloorOpen(x)
            || (x > 0 && simulation.IsFloorOpen(x - 1))
            || (x < simulation.Width - 1 && simulation.IsFloorOpen(x + 1));

        private static int CountOpenColumns(SandSimulation simulation)
        {
            var open = 0;
            for (var x = 0; x < simulation.Width; x++)
            {
                if (simulation.IsFloorOpen(x))
                    open++;
            }

            return open;
        }
    }
}
