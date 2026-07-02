namespace ClothingPlatform.Api.Models.Order
{
    public static class OrderWorkflow
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Confirm = "Confirm";
        public const string Cancelled = "Cancelled";
        public const string CancelledByCustomer = "CancelledByCustomer";
        public const string CancelledByStaff = "CancelledByStaff";

        public static readonly string[] Statuses = { Pending, Processing, Confirm, Cancelled, CancelledByCustomer, CancelledByStaff };
        private static readonly string[] FulfillmentStatuses = { Pending, Processing, Confirm };

        public static string Normalize(string? status)
        {
            return (status ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "processing" => Processing,
                "confirm" or "confirmed" or "completed" or "delivered" => Confirm,
                "cancelledbycustomer" or "cancelled_by_customer" => CancelledByCustomer,
                "cancelledbystaff" or "cancelled_by_staff" or "cancelledbyadmin" or "cancelled_by_admin" => CancelledByStaff,
                "cancelled" or "canceled" => Cancelled,
                _ => Pending
            };
        }

        public static bool IsCancelled(string? status)
        {
            var normalized = Normalize(status);
            return normalized == Cancelled
                || normalized == CancelledByCustomer
                || normalized == CancelledByStaff;
        }

        public static bool CanMoveTo(string? currentStatus, string? requestedStatus)
        {
            var current = Normalize(currentStatus);
            var requested = Normalize(requestedStatus);

            if (IsCancelled(requested))
            {
                return current == Pending || current == Processing;
            }

            if (IsCancelled(current))
            {
                return false;
            }

            return Array.IndexOf(FulfillmentStatuses, requested) >= Array.IndexOf(FulfillmentStatuses, current);
        }

        public static bool IsFinal(string? status)
        {
            var normalized = Normalize(status);
            return string.Equals(normalized, Confirm, StringComparison.Ordinal)
                || IsCancelled(normalized);
        }
    }
}
