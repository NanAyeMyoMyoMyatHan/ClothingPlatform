using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class StaffSalesLog
{
    public int LogId { get; set; }

    public int StaffId { get; set; }

    public int OrderId { get; set; }

    public int VariantId { get; set; }

    public int QuantitySold { get; set; }

    public decimal SaleAmount { get; set; }

    public DateOnly SoldAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;

    public virtual ProductVariant Variant { get; set; } = null!;
}
