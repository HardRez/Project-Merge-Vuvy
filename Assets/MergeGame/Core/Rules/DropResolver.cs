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
                    return EnsureValid(rule, outcome, context);
            }

            throw new InvalidOperationException(
                $"No drop rule resolved the drop from {from} to {to}. The rule list must handle every drop, e.g. end with {nameof(MergeRule)}.");
        }

        /// <summary>Rules are extension points, so their answer is checked before the session acts on it.</summary>
        private static DropOutcome EnsureValid(IDropRule rule, DropOutcome outcome, DropContext context)
        {
            var ruleName = rule.GetType().Name;
            if (outcome == null)
                throw new InvalidOperationException(
                    $"Drop rule {ruleName} claimed the drop from {context.From} to {context.To} but returned no outcome.");
            if (outcome.From != context.From || outcome.To != context.To)
                throw new InvalidOperationException(
                    $"Drop rule {ruleName} returned an outcome for {outcome.From} -> {outcome.To} while resolving {context.From} -> {context.To}.");
            if (outcome.Kind == DropOutcomeKind.Move && !context.IsTargetEmpty)
                throw new InvalidOperationException($"Drop rule {ruleName} moved an item onto the occupied cell {context.To}.");
            if (outcome.Kind == DropOutcomeKind.Merge && (context.IsTargetEmpty || !context.Catalog.Contains(outcome.ResultItemId)))
                throw new InvalidOperationException(
                    $"Drop rule {ruleName} merged into {outcome.ResultItemId} on {context.To}; merges need an occupied target and a catalog item.");
            return outcome;
        }
    }
}
