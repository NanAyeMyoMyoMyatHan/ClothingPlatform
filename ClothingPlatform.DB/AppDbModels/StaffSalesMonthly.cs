using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class StaffSalesMonthly
{
    public int MonthlyReportId { get; set; }

    public int StaffId { get; set; }

    public int ReportMonth { get; set; }

    public int ReportYear { get; set; }

    public int TotalProductsSold { get; set; }

    public decimal TotalSalesValue { get; set; }

    public virtual User Staff { get; set; } = null!;
}
