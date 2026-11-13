using System;
using MergeGame.Core.Grid;
using MergeGame.Core.Items;

namespace MergeGame.Core.Rules
{
    public sealed class DropOutcome
    {
        private readonly ItemId resultItemId;

        private DropOutcome(DropOutcomeKind kind, GridPosition from, GridPosition to, ItemId resultItemId, DropRejectReason rejectReason)
        {
            Kind = kind;
            From = from;
            To = to;
            this.resultItemId = resultItemId;
            RejectReason = rejectReason;
        }

        public DropOutcomeKind Kind { get; }
        public GridPosition From { get; }
        public GridPosition To { get; }

        /// <summary><see cref="DropRejectReason.None"/> unless <see cref="Kind"/> is <see cref="DropOutcomeKind.Reject"/>.</summary>
        public DropRejectReason RejectReason { get; }

        public bool IsAccepted => Kind != DropOutcomeKind.Reject;

        /// <exception cref="InvalidOperationException">The outcome is not a merge.</exception>
        public ItemId ResultItemId =>
            Kind == DropOutcomeKind.Merge
                ? resultItemId
                : throw new InvalidOperationException($"A {Kind} outcome has no result item.");

        public static DropOutcome Move(GridPosition from, GridPosition to) =>
            new DropOutcome(DropOutcomeKind.Move, from, to, default, DropRejectReason.None);

        public static DropOutcome Merge(GridPosition from, GridPosition to, ItemId resultItemId) =>
            new DropOutcome(DropOutcomeKind.Merge, from, to, resultItemId, DropRejectReason.None);

        public static DropOutcome Reject(GridPosition from, GridPosition to, DropRejectReason reason)
        {
            if (reason == DropRejectReason.None)
                throw new ArgumentException("A rejected drop needs a reason.", nameof(reason));
            return new DropOutcome(DropOutcomeKind.Reject, from, to, default, reason);
        }

        public override string ToString() =>
            Kind switch
            {
                DropOutcomeKind.Merge => $"Merge {From} -> {To} into {resultItemId}",
                DropOutcomeKind.Reject => $"Reject {From} -> {To} ({RejectReason})",
                _ => $"Move {From} -> {To}"
            };
    }
}
