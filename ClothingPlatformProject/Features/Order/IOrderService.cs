using ClothingPlatformProject.Models.Order;

namespace ClothingPlatformProject.Features.Order
{
    public interface IOrderService
    {
        List<OrderHistoryDto> GetUserOrderHistory(int userId);
        OrderResultDto PlaceOrderTransaction(CheckoutRequest model);

        Task<List<OrderDashboardDto>> GetAllOrder();
    }
}
