using ClothingPlatform.Api.Models.Order;

namespace ClothingPlatform.Api.Features.Order
{
    public interface IOrderService
    {
        List<OrderHistoryDto> GetUserOrderHistory(int userId);
        OrderResultDto PlaceOrderTransaction(CheckoutRequest model);

        Task<List<OrderDashboardDto>> GetAllOrder();

        Task<bool> DeleteOrderAsync(int orderId);
    }
}
