using System;
using MergeGame.Core.Rules;
using MergeGame.Core.Session;

namespace MergeGame.Core.Hints
{
    /// <summary>
    /// Finds a merge the player could make now. Every candidate pair is judged by <see cref="MergeGameSession.PreviewDrop"/>,
    /// so the session's drop rules stay the only authority: locked sources and max-level pairs are never suggested,
    /// merges onto locked targets are. Side-effect free; uses only the session's public surface.
    /// </summary>
    public sealed class MergeHintFinder
    {
        private readonly MergeGameSession session;

        public MergeHintFinder(MergeGameSession session)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Prefers a merge onto a locked target, since it frees a board cell. Within each preference the first pair wins,
        /// ordered row-major by source and then row-major by target, so the same board always gives the same hint.
        /// </summary>
        public bool TryFind(out MergeHint hint)
        {
            var board = session.Board;
            MergeHint? firstMerge = null;

            foreach (var from in board.Positions)
            {
                if (board.IsEmpty(from))
                    continue;

                foreach (var to in board.Positions)
                {
                    if (!board.TryGetItem(to, out var target))
                        continue;

                    var outcome = session.PreviewDrop(from, to);
                    if (outcome.Kind != DropOutcomeKind.Merge)
                        continue;

                    var candidate = new MergeHint(from, to, outcome.ResultItemId);
                    if (target.IsLocked)
                    {
                        hint = candidate;
                        return true;
                    }

                    firstMerge ??= candidate;
                }
            }

            hint = firstMerge.GetValueOrDefault();
            return firstMerge.HasValue;
        }
    }
}
