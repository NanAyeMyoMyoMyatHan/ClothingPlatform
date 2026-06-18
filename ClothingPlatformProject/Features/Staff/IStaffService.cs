using ClothingPlatform.DB.AppDbModels;

namespace ClothingPlatformProject.Features.Staff
{
    public interface IStaffService
    {
        Task<StaffDashboardDataDto> GetDashboardDataAsync(int staffId, DateTime reportDate);
        Task<bool> UpdateOrderStatusAsync(int orderId, int staffId, string newStatus);
        Task<bool> UpdateGuestOrderStatusAsync(int guestOrderId, int staffId, string newStatus);
        Task<bool> AdjustStockAsync(int variantId, int adjustment, int staffId);
        Task<bool> SubmitPhoneOrderAsync(GuestOrderRequestDto request, int staffId);
        Task<bool> UpdateProfileAsync(int staffId, string firstName, string lastName, string email);
    }

    public class StaffDashboardDataDto
    {
        public List<ClothingPlatform.DB.AppDbModels.Order> AllOrders { get; set; } = new();
        public List<GuestOrder> AllGuestOrders { get; set; } = new();
        public List<ProductVariant> InventoryVariants { get; set; } = new();
        public List<ClothingPlatform.DB.AppDbModels.Order> ReportOrders { get; set; } = new();
        public List<GuestOrder> ReportGuestOrders { get; set; } = new();
    }

    public class GuestOrderRequestDto
    {
        public string CustomerName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string ShippingAddress { get; set; } = "";
        public string PaymentMethod { get; set; } = "COD";
        public string PaymentStatus { get; set; } = "unpaid";
        public List<OrderLineDraftDto> OrderLines { get; set; } = new();
    }

    public class OrderLineDraftDto
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
