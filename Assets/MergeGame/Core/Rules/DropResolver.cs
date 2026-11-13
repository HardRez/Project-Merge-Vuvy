using System;
using System.Collections.Generic;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Rules
{
    /// <summary>
    /// Runs the structural checks every drop needs, then asks the configured rules in order; the first rule that
    /// resolves the drop wins. Side-effect free, so previews and real drops share exactly this path.
    /// </summary>
    internal sealed class DropResolver
    {
        private readonly IDropRule[] rules;

        public DropResolver(IReadOnlyList<IDropRule> rules)
        {
            this.rules = new IDropRule[rules.Count];
            for (var i = 0; i < rules.Count; i++)
                this.rules[i] = rules[i];
        }

        public DropOutcome Resolve(IReadOnlyBoard board, ItemCatalog catalog, GridPosition from, GridPosition to)
        {
            if (!board.IsInside(from) || !board.IsInside(to))
                return DropOutcome.Reject(from, to, DropRejectReason.OutOfBounds);
            if (from == to)
                return DropOutcome.Reject(from, to, DropRejectReason.SameCell);
            if (!board.TryGetItem(from, out var sourceItem))
                return DropOutcome.Reject(from, to, DropRejectReason.SourceEmpty);

            board.TryGetItem(to, out var targetItem);
            var context = new DropContext(board, catalog, from, to, sourceItem, targetItem);

            foreach (var rule in rules)
            {
                if (rule.TryResolve(context, out var outcome))
                    return outcome;
            }

            throw new InvalidOperationException(
                $"No drop rule resolved the drop from {from} to {to}. The rule list must handle every drop, e.g. end with {nameof(MergeRule)}.");
        }
    }
}
