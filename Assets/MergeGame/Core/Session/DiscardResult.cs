using System;
using MergeGame.Core.Items;

namespace MergeGame.Core.Session
{
    public sealed class DiscardResult
    {
        private readonly ItemId discardedItemId;

        private DiscardResult(bool succeeded, ItemId discardedItemId, DiscardFailureReason failureReason)
        {
            Succeeded = succeeded;
            this.discardedItemId = discardedItemId;
            FailureReason = failureReason;
        }

        public bool Succeeded { get; }
        public DiscardFailureReason FailureReason { get; }

        /// <exception cref="InvalidOperationException">The discard failed.</exception>
        public ItemId DiscardedItemId =>
            Succeeded ? discardedItemId : throw new InvalidOperationException($"A failed discard ({FailureReason}) removed no item.");

        public static DiscardResult Success(ItemId discardedItemId) => new DiscardResult(true, discardedItemId, DiscardFailureReason.None);

        public static DiscardResult Failure(DiscardFailureReason reason)
        {
            if (reason == DiscardFailureReason.None)
                throw new ArgumentException("A failed discard needs a reason.", nameof(reason));
            return new DiscardResult(false, default, reason);
        }

        public override string ToString() => Succeeded ? $"Discarded {discardedItemId}" : $"Discard failed ({FailureReason})";
    }
}
