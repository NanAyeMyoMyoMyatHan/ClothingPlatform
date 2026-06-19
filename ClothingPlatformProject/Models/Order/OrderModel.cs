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
        public string PaymentMethod { get; set; } = string.Empty; // cod, kpay, wave_money
        public string? TransactionId { get; set; }
        public string? SlipImageUrl { get; set; }
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

    public class OrderDashboardDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        public DateTime? OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; }  // Pending, Processing, Confirm
        public string PaymentStatus { get; set; } = "unpaid";  // unpaid, paid, refunded, completed

        // Shipping Details
        public string ShippingAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Notes { get; set; }

        // Child Collections (ဆက်စပ်နေသော ဇယားတွဲများ)
        public List<OrderPaymentDto> Payments { get; set; } = new();
        public List<OrderItemDashboardDto> OrderItems { get; set; } = new();
    }

    // ၂။ Order Items DTO (အော်ဒါထဲက ပစ္စည်းတစ်ခုချင်းစီ)
    public class OrderItemDashboardDto
    {
        public int OrderItemId { get; set; }
        public int VariantId { get; set; }
        public int ProductId { get; set; }

        // Product & Variant Details (UI တွင် တန်းပြရန် ဆွဲထုတ်လာမည့်အချက်အလက်များ)
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? PrimaryImageUrl { get; set; } // ပစ္စည်းဓာတ်ပုံပြရန်

        public int Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal TotalPrice => Quantity * PricePerUnit; // Auto Calculation
    }

    // ၃။ Payments DTO (ငွေပေးချေမှုမှတ်တမ်း)
    public class OrderPaymentDto
    {
        public int PaymentId { get; set; }
        public string PaymentMethod { get; set; } = string.Empty; // cod, kpay, wave_money
        public string TransactionNumber { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
    public class OrderDto
    {

        public int OrderId { get; set; }

        public string CustomerName { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; }

        public string PaymentMethod { get; set; }

        public int ItemCount { get; set; }

        public DateTime? CreatedAt { get; set; }

    }

}
