using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class StaffSalesDaily
{
    public int ReportId { get; set; }

    public int StaffId { get; set; }

    public DateOnly ReportDate { get; set; }

    public int TotalProductsSold { get; set; }

    public decimal TotalSalesValue { get; set; }

    public virtual User Staff { get; set; } = null!;
}
