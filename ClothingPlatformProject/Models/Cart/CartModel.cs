namespace ClothingPlatformProject.Models.Cart
{
    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    // GET User Cart အတွက် DTO
    public class CartDto
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
    }

    // POST Request (ခြင်းတောင်းထဲ ပစ္စည်းထည့်ရန်) Model
    public class AddToCartRequest
    {
        public int UserId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        public int Quantity { get; set; }
    }
}
