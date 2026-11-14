using System.Collections.Generic;

namespace MergeGame.Presentation.Tests.Support
{
    /// <summary>Every call any fake view received, in order, so tests can check what the player sees and when.</summary>
    internal sealed class ViewLog
    {
        private readonly List<string> entries = new List<string>();

        public IReadOnlyList<string> Entries => entries;

        public void Add(string entry) => entries.Add(entry);

        public void Clear() => entries.Clear();

        /// <summary>The entries that start with one of the prefixes, in order.</summary>
        public List<string> Only(params string[] prefixes) =>
            entries.FindAll(entry => System.Array.Exists(prefixes, prefix => entry.StartsWith(prefix, System.StringComparison.Ordinal)));
    }
}
