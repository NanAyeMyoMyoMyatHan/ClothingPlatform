using ClothingPlatform.Api.Models.Product;
using ClothingPlatform.Api.Models.Order;

namespace ClothingPlatform.Api.Models.Staff;

public class StaffDashboardDto
{
    public List<OrderDto> Orders { get; set; } = new();
    public List<InventoryDto> Inventory { get; set; } = new();  // ★ InventoryDto
}