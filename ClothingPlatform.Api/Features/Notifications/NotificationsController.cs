using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatform.Api.Features.Notifications
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ICustomerNotificationService _notificationService;

        public NotificationsController(ICustomerNotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId)
        {
            return Ok(await _notificationService.GetUserNotificationsAsync(userId));
        }

        [HttpPost("{notificationId}/read")]
        public async Task<IActionResult> MarkRead(int notificationId)
        {
            var success = await _notificationService.MarkReadAsync(notificationId);
            if (!success) return NotFound();
            return Ok();
        }

        [HttpPost("send-cancelled")]
        public async Task<IActionResult> SendCancelledNotification([FromQuery] int userId, [FromQuery] int orderId)
        {
            var notification = await _notificationService.CreateNotificationAsync(
                userId,
                orderId,
                "Order Cancellation Update",
                $"Dear valued customer, we sincerely apologize, but your order ORD-{orderId:D4} has been cancelled due to an unexpected stock issue. We thank you so much for your interest in Chic Boutique and apologize for the inconvenience. To show how much we value you, please use the discount code CHIC10 for 10% off your next purchase!");
            return Ok(notification);
        }
    }
}
