using System;
using System.Collections.Generic;

namespace MergeGame.Data.Visuals
{
    /// <summary>
    /// The animations of a customer portrait, found by name suffix so every character can prefix its own
    /// (<c>Mark_appear</c>, <c>Lily_appear</c>). Each state has a one-shot entry and a loop (<c>_repeat</c>):
    /// <c>appear</c> when the customer arrives and while it waits, <c>progress</c> while some of the order is on the board,
    /// <c>finished</c> while it can be delivered, and the one-shot <c>leave</c> once it was delivered.
    /// When several names share a suffix (<c>Lily_appear_repeat</c>, <c>Lily_story_appear_repeat</c>) the shortest wins.
    /// </summary>
    public sealed class CustomerAnimationSet
    {
        public static readonly IReadOnlyList<string> Suffixes = new[]
        {
            "appear", "appear_repeat", "progress", "progress_repeat", "finished", "finished_repeat", "leave"
        };

        private CustomerAnimationSet(IReadOnlyList<string> names)
        {
            Appear = names[0];
            AppearLoop = names[1];
            Progress = names[2];
            ProgressLoop = names[3];
            Finished = names[4];
            FinishedLoop = names[5];
            Leave = names[6];
        }

        public string Appear { get; }
        public string AppearLoop { get; }
        public string Progress { get; }
        public string ProgressLoop { get; }
        public string Finished { get; }
        public string FinishedLoop { get; }
        public string Leave { get; }

        /// <param name="animationNames">Every animation of the skeleton.</param>
        /// <param name="missing">The suffixes no animation ends with; empty on success.</param>
        public static bool TryResolve(IEnumerable<string> animationNames, out CustomerAnimationSet set, out IReadOnlyList<string> missing)
        {
            if (animationNames == null)
                throw new ArgumentNullException(nameof(animationNames));

            var available = new List<string>(animationNames);
            var resolved = new string[Suffixes.Count];
            var absent = new List<string>();
            for (var i = 0; i < Suffixes.Count; i++)
            {
                resolved[i] = Find(available, Suffixes[i]);
                if (resolved[i] == null)
                    absent.Add(Suffixes[i]);
            }

            missing = absent;
            set = absent.Count == 0 ? new CustomerAnimationSet(resolved) : null;
            return set != null;
        }

        private static string Find(IEnumerable<string> names, string suffix)
        {
            string best = null;
            foreach (var name in names)
            {
                if (name == null || !(name == suffix || name.EndsWith("_" + suffix, StringComparison.Ordinal)))
                    continue;
                if (best == null || name.Length < best.Length || (name.Length == best.Length && string.CompareOrdinal(name, best) < 0))
                    best = name;
            }

            return best;
        }
    }
}
