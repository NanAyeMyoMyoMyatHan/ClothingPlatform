namespace ClothingPlatformProject.Models.Order
{
    public class CheckoutItemRequest
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    // POST Request (Checkout/Place Order) လုပ်ရန် Model
    public class CheckoutRequest
    {
        public int UserId { get; set; }
        public decimal TotalPrice { get; set; }
        public string ShippingAddress { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // ဥပမာ - KPay, CBPay, Cash
        public List<CheckoutItemRequest> Items { get; set; } = new();
    }

    // Order Checkout ပြီးဆုံးပါက ပြန်လည်ထုတ်ပေးမည့် Response DTO
    public class OrderResultDto
    {
        public int OrderId { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // GET Order History အတွက် သုံးမည့် DTO
    public class OrderHistoryDto
    {
        public int OrderId { get; set; }
        public decimal TotalPrice { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
