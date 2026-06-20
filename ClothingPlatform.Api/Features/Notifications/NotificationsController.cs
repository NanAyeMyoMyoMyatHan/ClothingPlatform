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
    }
}
