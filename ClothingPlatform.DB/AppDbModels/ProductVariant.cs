using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class ProductVariant
{
    public int VariantId { get; set; }

    public int ProductId { get; set; }

    public string Size { get; set; } = null!;

    public string Color { get; set; } = null!;

    public string Sku { get; set; } = null!;

    public int StockQuantity { get; set; }

    public decimal? PriceModifier { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<GuestOrderItem> GuestOrderItems { get; set; } = new List<GuestOrderItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<StaffSalesLog> StaffSalesLogs { get; set; } = new List<StaffSalesLog>();
}
