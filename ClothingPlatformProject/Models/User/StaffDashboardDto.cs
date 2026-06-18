using ClothingPlatformProject.Models.Product;
using ClothingPlatformProject.Models.Order;

namespace ClothingPlatformProject.Models.Staff;

public class StaffDashboardDto
{
    public List<OrderDto> Orders { get; set; } = new();
    public List<InventoryDto> Inventory { get; set; } = new();  // ★ InventoryDto
}