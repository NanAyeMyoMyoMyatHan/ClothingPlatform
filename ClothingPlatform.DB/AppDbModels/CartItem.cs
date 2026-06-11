using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class CartItem
{
    public int CartId { get; set; }

    public int UserId { get; set; }

    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ProductVariant Variant { get; set; } = null!;
}
