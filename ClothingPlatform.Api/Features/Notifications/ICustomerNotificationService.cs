using ClothingPlatform.Api.Models.Notifications;

namespace ClothingPlatform.Api.Features.Notifications
{
    public interface ICustomerNotificationService
    {
        Task<CustomerNotificationDto> CreateOrderDeletedNotificationAsync(int userId, int orderId);
        Task<CustomerNotificationDto> CreateNotificationAsync(int userId, int? orderId, string title, string message);
        Task<List<CustomerNotificationDto>> GetUserNotificationsAsync(int userId);
        Task<bool> MarkReadAsync(int notificationId);
    }
}
