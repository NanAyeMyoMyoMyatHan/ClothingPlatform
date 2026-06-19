using ClothingPlatformProject.Models.Notifications;

namespace ClothingPlatformProject.Features.Notifications
{
    public interface ICustomerNotificationService
    {
        Task<CustomerNotificationDto> CreateOrderDeletedNotificationAsync(int userId, int orderId);
        Task<List<CustomerNotificationDto>> GetUserNotificationsAsync(int userId);
        Task<bool> MarkReadAsync(int notificationId);
    }
}
