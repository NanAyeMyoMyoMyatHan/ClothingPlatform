using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class GuestOrderItem
{
    public int GuestOrderItemId { get; set; }

    public int GuestOrderId { get; set; }

    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public decimal PriceAtPurchase { get; set; }

    public virtual GuestOrder GuestOrder { get; set; } = null!;

    public virtual ProductVariant Variant { get; set; } = null!;
}
