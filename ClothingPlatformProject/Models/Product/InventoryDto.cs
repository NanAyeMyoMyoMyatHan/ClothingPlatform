namespace ClothingPlatformProject.Models.Product
{
    public class InventoryDto
    {
        public int VariantId { get; set; }
        public string ProductName { get; set; } = "";
        public string Sku { get; set; } = "";
        public string Size { get; set; } = "";
        public string Color { get; set; } = "";
        public int StockQuantity { get; set; }
    }
}
