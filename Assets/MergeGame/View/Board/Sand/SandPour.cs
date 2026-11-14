using System;

namespace MergeGame.View.Board.Sand
{
    /// <summary>
    /// Sand letting go of a closed cell and pouring out of it, as a closed-form function of time. Every grain rests at its
    /// grid spot until its own release time, then falls freely: <c>y = y0 − ½·g·τ²</c> and <c>x = x0 + drift·τ²/(τ + onset)</c> (sideways speed builds up from 0 to drift), with
    /// <c>τ = t − release</c>. Release times form a fast, uneven wave: grains near the seeded give-way points on the floor
    /// go first, and each row lets go a moment after the row below it, so the surface caves in, lower sand leaves first
    /// and the falling mass stretches and breaks up into grains (gravity and drift vary slightly per grain).
    /// Positions are in grains, row 0 at the bottom; nothing here depends on frames, so the motion is perfectly smooth at
    /// any frame rate and identical for the same seed. Gravity is chosen so every grain is out of the cell by
    /// <see cref="SandPourSettings.TotalSeconds"/>.
    /// </summary>
    public sealed class SandPour
    {
        private readonly SandPourSettings settings;
        private readonly float[] restX;
        private readonly float[] restY;
        private readonly float[] release;
        private readonly float[] gravity;
        private readonly float[] drift;
        private readonly int[] cell;
        private uint random;

        /// <param name="occupied">Row-major from the bottom row; true where a grain rests at the start.</param>
        /// <param name="seed">Picks the give-way points and every grain's variation.</param>
        public SandPour(int columns, int rows, bool[] occupied, int seed, SandPourSettings settings)
        {
            if (columns < 1 || rows < 1)
                throw new ArgumentOutOfRangeException(nameof(columns), "The grid needs at least one cell.");
            if (occupied == null || occupied.Length != columns * rows)
                throw new ArgumentException($"Expected {columns * rows} cells.", nameof(occupied));

            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Columns = columns;
            Rows = rows;
            random = (uint)seed * 2654435761u ^ 0x9E3779B9u;
            if (random == 0)
                random = 1;

            var count = 0;
            foreach (var isGrain in occupied)
                count += isGrain ? 1 : 0;
            Count = count;
            restX = new float[count];
            restY = new float[count];
            release = new float[count];
            gravity = new float[count];
            drift = new float[count];
            cell = new int[count];

            var giveWay = new float[settings.GiveWayPoints];
            for (var i = 0; i < giveWay.Length; i++)
                giveWay[i] = columns * (0.2f + 0.6f * (i + NextFloat()) / giveWay.Length);
            var farthest = 0f;
            for (var x = 0; x < columns; x++)
                farthest = Math.Max(farthest, DistanceToGiveWay(x + 0.5f, giveWay));

            // The row climb leaves room for each grain's random extra delay, so the wave never overruns its time.
            var sidewaysTime = settings.ReleaseWaveSeconds * settings.SidewaysShare;
            var rowTime = settings.ReleaseWaveSeconds * (1f - settings.SidewaysShare) / (rows + settings.ReleaseNoise);

            // Slow enough that the slowest grain released last still clears the cell in time.
            var fallTime = settings.TotalSeconds - settings.ReleaseWaveSeconds;
            var baseGravity = 2f * (rows + 1) / (fallTime * fallTime * (1f - settings.GravityVariation));

            var grain = 0;
            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < columns; x++)
                {
                    if (!occupied[y * columns + x])
                        continue;

                    var centreX = x + 0.5f;
                    restX[grain] = centreX;
                    restY[grain] = y + 0.5f;
                    cell[grain] = y * columns + x;
                    var side = farthest > 0f ? DistanceToGiveWay(centreX, giveWay) / farthest : 0f;
                    release[grain] = sidewaysTime * side + rowTime * (y + settings.ReleaseNoise * NextFloat());
                    gravity[grain] = baseGravity * (1f - settings.GravityVariation * NextFloat());
                    // Grains drift a little to either side, a bit more often away from the nearest give-way point.
                    var away = Math.Sign(centreX - NearestGiveWay(centreX, giveWay));
                    drift[grain] = settings.Drift * (NextFloat() * 2f - 1f + 0.3f * away);
                    grain++;
                }
            }
        }

        public int Columns { get; }
        public int Rows { get; }

        /// <summary>Number of grains, live or gone.</summary>
        public int Count { get; }

        /// <summary>When every grain has fallen out of the cell.</summary>
        public float Duration => settings.TotalSeconds;

        /// <summary>The grid cell (row-major from the bottom) the grain rests in at the start; it picks the grain's colour.</summary>
        public int RestCell(int grain) => cell[grain];

        public float ReleaseTime(int grain) => release[grain];

        /// <summary>How far the grain has fallen since it let go, in grains.</summary>
        public float FallenDistance(int grain, float y) => restY[grain] - y;

        /// <summary>Length of a falling grain's motion streak, in grains.</summary>
        public float StreakLength(float speed) => Math.Min(settings.MaxStreak, speed * settings.StreakSeconds);

        /// <summary>The grain's centre at <paramref name="time"/>, in grains (row 0 at the bottom).</summary>
        /// <param name="fallTime">Seconds since the grain let go; 0 or less while it still rests.</param>
        /// <param name="speed">Its falling speed, in grains per second.</param>
        public void Locate(int grain, float time, out float x, out float y, out float fallTime, out float speed)
        {
            fallTime = time - release[grain];
            if (fallTime <= 0f)
            {
                x = restX[grain];
                y = restY[grain];
                speed = 0f;
                return;
            }

            speed = gravity[grain] * fallTime;
            // Sideways speed builds up from 0 to the grain's drift over the onset time.
            x = restX[grain] + drift[grain] * fallTime * fallTime / (fallTime + settings.DriftOnsetSeconds);
            y = restY[grain] - 0.5f * gravity[grain] * fallTime * fallTime;
        }

        /// <summary>True once the whole grain is below the cell's floor.</summary>
        public bool IsGone(int grain, float time)
        {
            Locate(grain, time, out _, out var y, out _, out _);
            return y + 0.5f < 0f;
        }

        public int LiveCount(float time)
        {
            var live = 0;
            for (var i = 0; i < Count; i++)
                live += IsGone(i, time) ? 0 : 1;
            return live;
        }

        private static float DistanceToGiveWay(float x, float[] giveWay) => Math.Abs(x - NearestGiveWay(x, giveWay));

        private static float NearestGiveWay(float x, float[] giveWay)
        {
            var nearest = giveWay[0];
            foreach (var point in giveWay)
            {
                if (Math.Abs(x - point) < Math.Abs(x - nearest))
                    nearest = point;
            }

            return nearest;
        }

        private float NextFloat()
        {
            // xorshift32: tiny, fast and identical on every platform.
            random ^= random << 13;
            random ^= random >> 17;
            random ^= random << 5;
            return (random >> 8) / 16777216f;
        }
    }
}
