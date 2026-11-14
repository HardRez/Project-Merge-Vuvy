using System;
using UnityEngine;

namespace MergeGame.View.Board.Sand
{
    /// <summary>Tuning of a <see cref="SandSimulation"/>, in grid cells and simulation steps.</summary>
    [Serializable]
    public sealed class SandPourSettings
    {
        [Tooltip("Speed a falling grain gains every step, in cells per step.")]
        [SerializeField] private float gravity = 0.08f;

        [Tooltip("Top speed of a falling grain, in cells per step.")]
        [SerializeField] private float maxSpeed = 3f;

        [Tooltip("Chance per step that a grain resting on another slides diagonally down when that cell is free; " +
                 "below 1 the slopes stay a little rougher and the sand pours less mechanically.")]
        [Range(0f, 1f)]
        [SerializeField] private float slideChance = 0.8f;

        [Tooltip("Narrow openings in the floor that the sand first funnels through.")]
        [Min(1)]
        [SerializeField] private int holeCount = 2;

        [Tooltip("Step at which the holes open, one column wide.")]
        [Min(0)]
        [SerializeField] private int holesOpenStep = 3;

        [Tooltip("Steps from the holes opening until the whole floor is open.")]
        [Min(1)]
        [SerializeField] private int widenSteps = 70;

        public SandPourSettings()
        {
        }

        public SandPourSettings(float gravity, float maxSpeed, float slideChance, int holeCount, int holesOpenStep, int widenSteps)
        {
            this.gravity = gravity;
            this.maxSpeed = maxSpeed;
            this.slideChance = slideChance;
            this.holeCount = holeCount;
            this.holesOpenStep = holesOpenStep;
            this.widenSteps = widenSteps;
        }

        public float Gravity => gravity;
        public float MaxSpeed => maxSpeed;
        public float SlideChance => slideChance;
        public int HoleCount => Math.Max(1, holeCount);
        public int HolesOpenStep => Math.Max(0, holesOpenStep);
        public int WidenSteps => Math.Max(1, widenSteps);
    }
}
