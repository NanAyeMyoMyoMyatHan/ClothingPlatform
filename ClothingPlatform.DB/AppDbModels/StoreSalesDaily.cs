using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class StoreSalesDaily
{
    public int DailySummaryId { get; set; }

    public DateOnly ReportDate { get; set; }

    public int ActiveStaffCount { get; set; }

    public int TotalProductsSold { get; set; }

    public decimal TotalRevenue { get; set; }
}
