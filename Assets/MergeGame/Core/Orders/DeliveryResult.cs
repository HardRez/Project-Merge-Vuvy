using System;

namespace MergeGame.Core.Orders
{
    public sealed class DeliveryResult
    {
        private readonly Order deliveredOrder;

        private DeliveryResult(bool succeeded, Order deliveredOrder, DeliveryFailureReason failureReason)
        {
            Succeeded = succeeded;
            this.deliveredOrder = deliveredOrder;
            FailureReason = failureReason;
        }

        public bool Succeeded { get; }
        public DeliveryFailureReason FailureReason { get; }

        /// <exception cref="InvalidOperationException">The delivery failed.</exception>
        public Order DeliveredOrder =>
            Succeeded ? deliveredOrder : throw new InvalidOperationException($"A failed delivery ({FailureReason}) has no delivered order.");

        public static DeliveryResult Success(Order deliveredOrder) =>
            new DeliveryResult(true, deliveredOrder ?? throw new ArgumentNullException(nameof(deliveredOrder)), DeliveryFailureReason.None);

        public static DeliveryResult Failure(DeliveryFailureReason reason)
        {
            if (reason == DeliveryFailureReason.None)
                throw new ArgumentException("A failed delivery needs a reason.", nameof(reason));
            return new DeliveryResult(false, null, reason);
        }

        public override string ToString() => Succeeded ? $"Delivered {deliveredOrder}" : $"Delivery failed ({FailureReason})";
    }
}
