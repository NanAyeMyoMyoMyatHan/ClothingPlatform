namespace ClothingPlatform.Api.Models.Order
{
    public static class OrderWorkflow
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Confirm = "Confirm";
        public const string Cancelled = "Cancelled";

        public static readonly string[] Statuses = { Pending, Processing, Confirm, Cancelled };
        private static readonly string[] FulfillmentStatuses = { Pending, Processing, Confirm };

        public static string Normalize(string? status)
        {
            return (status ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "processing" => Processing,
                "confirm" or "confirmed" or "completed" or "delivered" => Confirm,
                "cancelled" or "canceled" => Cancelled,
                _ => Pending
            };
        }

        public static bool CanMoveTo(string? currentStatus, string? requestedStatus)
        {
            var current = Normalize(currentStatus);
            var requested = Normalize(requestedStatus);

            if (requested == Cancelled)
            {
                return current == Pending || current == Processing;
            }

            if (current == Cancelled)
            {
                return false;
            }

            return Array.IndexOf(FulfillmentStatuses, requested) >= Array.IndexOf(FulfillmentStatuses, current);
        }

        public static bool IsFinal(string? status)
        {
            var normalized = Normalize(status);
            return string.Equals(normalized, Confirm, StringComparison.Ordinal)
                || string.Equals(normalized, Cancelled, StringComparison.Ordinal);
        }
    }
}
