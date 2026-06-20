namespace ClothingPlatform.Api.Models.Order
{
    public static class OrderWorkflow
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Confirm = "Confirm";

        public static readonly string[] Statuses = { Pending, Processing, Confirm };

        public static string Normalize(string? status)
        {
            return (status ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "processing" => Processing,
                "confirm" or "confirmed" or "completed" or "delivered" => Confirm,
                _ => Pending
            };
        }

        public static bool CanMoveTo(string? currentStatus, string? requestedStatus)
        {
            var current = Normalize(currentStatus);
            var requested = Normalize(requestedStatus);
            return Array.IndexOf(Statuses, requested) >= Array.IndexOf(Statuses, current);
        }

        public static bool IsFinal(string? status)
        {
            return string.Equals(Normalize(status), Confirm, StringComparison.Ordinal);
        }
    }
}
