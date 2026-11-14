using System;
using UnityEngine;

namespace MergeGame.View.Board.Sand
{
    /// <summary>Tuning of a <see cref="SandPour"/>. Distances are in grains, times in seconds.</summary>
    [Serializable]
    public sealed class SandPourSettings
    {
        [Tooltip("Seconds until the last grain (top row, far from the give-way points) has let go.")]
        [Min(0.01f)]
        [SerializeField] private float releaseWaveSeconds = 0.18f;

        [Tooltip("Share of the release wave spent spreading sideways from the give-way points; the rest climbs the rows.")]
        [Range(0f, 1f)]
        [SerializeField] private float sidewaysShare = 0.5f;

        [Tooltip("How unevenly the grains let go: random extra delay per grain, as a share of the time between rows.")]
        [Range(0f, 2f)]
        [SerializeField] private float releaseNoise = 1f;

        [Tooltip("Spots on the floor where the sand gives way first, so the surface caves in above them.")]
        [Min(1)]
        [SerializeField] private int giveWayPoints = 2;

        [Tooltip("Seconds from the start until the last grain has fallen out of the cell; gravity follows from it.")]
        [Min(0.05f)]
        [SerializeField] private float totalSeconds = 0.8f;

        [Tooltip("Per-grain variation of gravity, so the stream breaks up into single grains as it speeds up.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float gravityVariation = 0.05f;

        [Tooltip("Largest sideways speed of a falling grain, in grains per second.")]
        [Min(0f)]
        [SerializeField] private float drift = 3f;

        [Tooltip("Seconds over which a grain picks up its sideways drift; it first drops straight down, so the sand " +
                 "does not split into hairline gaps the moment it lets go.")]
        [Min(0.001f)]
        [SerializeField] private float driftOnsetSeconds = 0.06f;

        [Tooltip("How long a falling grain's streak is, in seconds of its fall (motion blur).")]
        [Min(0f)]
        [SerializeField] private float streakSeconds = 0.008f;

        [Tooltip("Longest streak, in grains.")]
        [Min(0f)]
        [SerializeField] private float maxStreak = 2.5f;

        public SandPourSettings()
        {
        }

        public SandPourSettings(float releaseWaveSeconds, float totalSeconds, int giveWayPoints)
        {
            this.releaseWaveSeconds = releaseWaveSeconds;
            this.totalSeconds = totalSeconds;
            this.giveWayPoints = giveWayPoints;
        }

        public float ReleaseWaveSeconds => releaseWaveSeconds;
        public float SidewaysShare => sidewaysShare;
        public float ReleaseNoise => releaseNoise;
        public int GiveWayPoints => Math.Max(1, giveWayPoints);
        public float TotalSeconds => Math.Max(totalSeconds, releaseWaveSeconds + 0.05f);
        public float GravityVariation => gravityVariation;
        public float Drift => drift;
        public float DriftOnsetSeconds => Math.Max(0.001f, driftOnsetSeconds);
        public float StreakSeconds => streakSeconds;
        public float MaxStreak => maxStreak;
    }
}
