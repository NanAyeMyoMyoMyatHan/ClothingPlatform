namespace ClothingPlatformProject.Models.Notifications
{
    public class CustomerNotificationDto
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public int? OrderId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
