using Microsoft.AspNetCore.SignalR;

namespace ClothingPlatformProject.Features.Notifications
{
    public class CustomerNotificationHub : Hub
    {
        public async Task JoinCustomerGroup(int userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, CustomerGroup(userId));
        }

        public static string CustomerGroup(int userId) => $"customer-{userId}";
    }
}
