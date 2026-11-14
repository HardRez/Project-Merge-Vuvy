using System;
using MergeGame.Data.Visuals;
using NUnit.Framework;

namespace MergeGame.Data.Tests
{
    public sealed class CustomerAnimationSetTests
    {
        private static readonly string[] Mark =
        {
            "Mark_appear", "Mark_appear_repeat", "Mark_progress", "Mark_progress_repeat",
            "Mark_finished", "Mark_finished_repeat", "Mark_leave"
        };

        [Test]
        public void PrefixedNames_ResolveByTheirSuffix()
        {
            Assert.That(CustomerAnimationSet.TryResolve(Mark, out var set, out var missing), Is.True);

            Assert.That(missing, Is.Empty);
            Assert.That(set.Appear, Is.EqualTo("Mark_appear"));
            Assert.That(set.AppearLoop, Is.EqualTo("Mark_appear_repeat"));
            Assert.That(set.Progress, Is.EqualTo("Mark_progress"));
            Assert.That(set.ProgressLoop, Is.EqualTo("Mark_progress_repeat"));
            Assert.That(set.Finished, Is.EqualTo("Mark_finished"));
            Assert.That(set.FinishedLoop, Is.EqualTo("Mark_finished_repeat"));
            Assert.That(set.Leave, Is.EqualTo("Mark_leave"));
        }

        [Test]
        public void SeveralNamesWithTheSameSuffix_TheShortestWins()
        {
            var names = new[]
            {
                "Lily_story_appear_repeat", "Lily_appear", "Lily_appear_repeat", "Lily_progress", "Lily_progress_repeat",
                "Lily_story_finished", "Lily_finished", "Lily_finished_repeat", "Lily_story_leave2", "Lily_story_leave", "Lily_leave"
            };

            Assert.That(CustomerAnimationSet.TryResolve(names, out var set, out _), Is.True);

            Assert.That(set.AppearLoop, Is.EqualTo("Lily_appear_repeat"));
            Assert.That(set.Finished, Is.EqualTo("Lily_finished"));
            Assert.That(set.Leave, Is.EqualTo("Lily_leave"));
        }

        [Test]
        public void UnprefixedNames_Resolve()
        {
            var names = Array.ConvertAll(Mark, name => name.Substring("Mark_".Length));

            Assert.That(CustomerAnimationSet.TryResolve(names, out var set, out _), Is.True);
            Assert.That(set.Leave, Is.EqualTo("leave"));
        }

        [Test]
        public void ASuffixInsideAWord_DoesNotCount()
        {
            var names = Array.ConvertAll(Mark, name => name == "Mark_leave" ? "Markleave" : name);

            Assert.That(CustomerAnimationSet.TryResolve(names, out var set, out var missing), Is.False);
            Assert.That(set, Is.Null);
            Assert.That(missing, Is.EqualTo(new[] { "leave" }));
        }

        [Test]
        public void MissingStates_AreListed()
        {
            Assert.That(CustomerAnimationSet.TryResolve(new[] { "Mark_appear", "Mark_leave" }, out _, out var missing), Is.False);

            Assert.That(missing, Is.EqualTo(new[] { "appear_repeat", "progress", "progress_repeat", "finished", "finished_repeat" }));
        }
    }
}
