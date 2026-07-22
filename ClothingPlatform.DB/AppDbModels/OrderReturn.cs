using System;

namespace ClothingPlatform.DB.AppDbModels;

public partial class OrderReturn
{
    public int OrderReturnId { get; set; }
    public int OrderId { get; set; }
    public int VariantId { get; set; }
    public int Quantity { get; set; }
    public string ReasonCheckbox { get; set; } = "";
    public string? ReasonText { get; set; }
    public string? ReceiptImageUrl { get; set; }
    public string ReturnOption { get; set; } = ""; // "Refund" or "Exchange"
    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"
    public DateTime CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual ProductVariant Variant { get; set; } = null!;
}
