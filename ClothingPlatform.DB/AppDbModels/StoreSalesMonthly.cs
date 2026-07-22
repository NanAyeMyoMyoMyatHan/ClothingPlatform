using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class StoreSalesMonthly
{
    public int MonthlySummaryId { get; set; }

    public int ReportMonth { get; set; }

    public int ReportYear { get; set; }

    public int TotalProductsSold { get; set; }

    public decimal TotalRevenue { get; set; }
}
