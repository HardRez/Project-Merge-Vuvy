using System;

namespace MergeGame.View.Board.Sand
{
    /// <summary>
    /// A falling-sand cellular automaton for one closed cell: a grid of individual grains (row 0 is the bottom) that
    /// pour out through the cell's floor. Each step, scanning from the bottom row up:
    /// <list type="bullet">
    /// <item>a grain with free space below falls: one cell when it starts, then faster each step up to a cap;</item>
    /// <item>a grain resting on another slides one cell diagonally down when that cell is free, so sand flows in from the
    /// sides toward wherever the sand below drains: a funnel opens above each hole and its slopes widen;</item>
    /// <item>otherwise it rests.</item>
    /// </list>
    /// The floor first opens as a few narrow holes that widen until the whole floor is open; a grain falling through an
    /// open floor column leaves the grid. Scanning bottom-up lets a packed column sink as one dense body (a grain falls
    /// into the space the grain below it has just left) while grains with room below them accelerate and pull apart into
    /// streams. Grains only ever move down, the count only ever drops, and for a given seed every run is identical.
    /// </summary>
    public sealed class SandSimulation
    {
        /// <summary>The value of an empty cell; occupied cells hold the grain's id plus one.</summary>
        public const ushort Empty = 0;

        /// <summary>Speed of a grain that just lost its support: one cell per step, so a packed column sinks without gaps.</summary>
        private const float ReleaseSpeed = 1f;

        private readonly SandPourSettings settings;
        private readonly ushort[] grains;
        private readonly float[] speeds;
        private readonly float[] progress;
        private readonly int[] movedOnStep;
        private readonly float[] holeCentres;
        private uint random;

        /// <param name="occupied">Row-major from the bottom row; true where a grain starts. Grain ids are these indices.</param>
        /// <param name="seed">Picks the holes and the grains' sliding choices.</param>
        public SandSimulation(int width, int height, bool[] occupied, int seed, SandPourSettings settings)
        {
            if (width < 2 || height < 2)
                throw new ArgumentOutOfRangeException(nameof(width), "The grid needs at least 2x2 cells.");
            if (occupied == null || occupied.Length != width * height)
                throw new ArgumentException($"Expected {width * height} cells.", nameof(occupied));
            if (width * height >= ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(width), "Grain ids must fit in 16 bits.");

            Width = width;
            Height = height;
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            grains = new ushort[width * height];
            speeds = new float[width * height];
            progress = new float[width * height];
            movedOnStep = new int[width * height];
            random = (uint)seed * 2654435761u ^ 0x9E3779B9u;
            if (random == 0)
                random = 1;

            for (var i = 0; i < occupied.Length; i++)
            {
                if (!occupied[i])
                    continue;
                grains[i] = (ushort)(i + 1);
                GrainCount++;
            }

            // One hole per equal stretch of the floor, at a seeded spot inside it, so the funnels spread over the cell.
            holeCentres = new float[settings.HoleCount];
            for (var h = 0; h < holeCentres.Length; h++)
            {
                var stretch = (h + 0.25f + 0.5f * NextFloat()) / holeCentres.Length;
                holeCentres[h] = (int)(width * (0.1f + 0.8f * stretch)) + 0.5f;
            }
        }

        public int Width { get; }
        public int Height { get; }
        public int GrainCount { get; private set; }
        public int StepCount { get; private set; }
        public bool IsEmpty => GrainCount == 0;

        /// <returns><see cref="Empty"/>, or the grain's id plus one.</returns>
        public ushort GrainAt(int x, int y) => grains[y * Width + x];

        /// <summary>
        /// How much faster than the sinking bulk the grain falls, as a share of the top speed: 0 for resting and sinking
        /// grains, up to 1 for grains in free fall at top speed.
        /// </summary>
        public float SpeedAt(int x, int y) =>
            Math.Max(0f, speeds[y * Width + x] - ReleaseSpeed) / Math.Max(0.0001f, settings.MaxSpeed - ReleaseSpeed);

        /// <summary>Whether grains in floor column <paramref name="x"/> fall out at the current step.</summary>
        public bool IsFloorOpen(int x)
        {
            var radius = FloorOpenRadius(StepCount);
            foreach (var centre in holeCentres)
            {
                if (Math.Abs(x + 0.5f - centre) <= radius)
                    return true;
            }

            return false;
        }

        public void Step()
        {
            StepCount++;
            for (var y = 0; y < Height; y++)
            {
                // Alternating the scan direction per row keeps the sliding free of a left or right bias.
                var leftToRight = (NextUInt() & 1) == 0;
                for (var n = 0; n < Width; n++)
                {
                    var x = leftToRight ? n : Width - 1 - n;
                    var index = y * Width + x;
                    if (grains[index] != Empty && movedOnStep[index] != StepCount)
                        Update(x, y);
                }
            }
        }

        private void Update(int x, int y)
        {
            var index = y * Width + x;
            if (y == 0 ? IsFloorOpen(x) : grains[index - Width] == Empty)
                Fall(x, y);
            else if (NextFloat() < settings.SlideChance)
                Slide(x, y);
            else
                Rest(index);
        }

        /// <summary>Accelerates the grain and moves it down as far as its accumulated fall allows.</summary>
        private void Fall(int x, int y)
        {
            var index = y * Width + x;
            var speed = speeds[index] <= 0f ? ReleaseSpeed : Math.Min(speeds[index] + settings.Gravity, settings.MaxSpeed);
            var fallen = progress[index] + speed;
            var cells = (int)fallen;
            fallen -= cells;

            var target = y;
            for (; cells > 0; cells--)
            {
                if (target == 0)
                {
                    if (IsFloorOpen(x))
                    {
                        Remove(index);
                        return;
                    }

                    break;
                }

                if (grains[(target - 1) * Width + x] != Empty)
                {
                    // Landed on something: the grain stops here.
                    speed = 0f;
                    fallen = 0f;
                    break;
                }

                target--;
            }

            MoveTo(index, target * Width + x, speed, fallen);
        }

        /// <summary>Slides one cell diagonally down, or out through an open floor column next to it.</summary>
        private void Slide(int x, int y)
        {
            var index = y * Width + x;
            var first = (NextUInt() & 1) == 0 ? -1 : 1;
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var side = attempt == 0 ? first : -first;
                var nx = x + side;
                if (nx < 0 || nx >= Width)
                    continue;

                if (y == 0)
                {
                    if (!IsFloorOpen(nx))
                        continue;
                    Remove(index);
                    return;
                }

                var target = (y - 1) * Width + nx;
                if (grains[target] != Empty)
                    continue;

                MoveTo(index, target, ReleaseSpeed, 0f);
                return;
            }

            Rest(index);
        }

        private void Rest(int index)
        {
            speeds[index] = 0f;
            progress[index] = 0f;
        }

        private void MoveTo(int from, int to, float speed, float fallen)
        {
            if (from != to)
            {
                grains[to] = grains[from];
                grains[from] = Empty;
                speeds[from] = 0f;
                progress[from] = 0f;
            }

            speeds[to] = speed;
            progress[to] = fallen;
            movedOnStep[to] = StepCount;
        }

        private void Remove(int index)
        {
            grains[index] = Empty;
            speeds[index] = 0f;
            progress[index] = 0f;
            GrainCount--;
        }

        /// <summary>Half-width of each floor hole in columns: one column at first, then widening ever faster.</summary>
        private float FloorOpenRadius(int step)
        {
            if (step < settings.HolesOpenStep)
                return -1f;

            var t = Math.Min(1f, (step - settings.HolesOpenStep) / (float)settings.WidenSteps);
            return 0.5f + t * t * Width;
        }

        private uint NextUInt()
        {
            // xorshift32: tiny, fast and identical on every platform.
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            return random;
        }

        private float NextFloat() => (NextUInt() >> 8) / 16777216f;
    }
}
