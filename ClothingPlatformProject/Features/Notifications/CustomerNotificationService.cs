using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatformProject.Features.Notifications
{
    public class CustomerNotificationService : ICustomerNotificationService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<CustomerNotificationHub> _hubContext;

        public CustomerNotificationService(AppDbContext db, IHubContext<CustomerNotificationHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        public async Task<CustomerNotificationDto> CreateOrderDeletedNotificationAsync(int userId, int orderId)
        {
            var notification = new CustomerNotification
            {
                UserId = userId,
                OrderId = orderId,
                Title = "Order deleted",
                Message = $"Your order ORD-{orderId:D4} was deleted by the admin team.",
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _db.CustomerNotifications.Add(notification);
            await _db.SaveChangesAsync();

            var dto = ToDto(notification);
            await _hubContext.Clients
                .Group(CustomerNotificationHub.CustomerGroup(userId))
                .SendAsync("CustomerNotification", dto);

            return dto;
        }

        public async Task<List<CustomerNotificationDto>> GetUserNotificationsAsync(int userId)
        {
            var notifications = await _db.CustomerNotifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return notifications.Select(ToDto).ToList();
        }

        public async Task<bool> MarkReadAsync(int notificationId)
        {
            var notification = await _db.CustomerNotifications.FirstOrDefaultAsync(n => n.NotificationId == notificationId);
            if (notification == null) return false;

            notification.IsRead = true;
            await _db.SaveChangesAsync();
            return true;
        }

        private static CustomerNotificationDto ToDto(CustomerNotification notification)
        {
            return new CustomerNotificationDto
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                OrderId = notification.OrderId,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };
        }
    }
}
