namespace ClothingPlatform.Api.Models.Report
{
    public class StaffDailySalesDto
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public DateOnly ReportDate { get; set; }
        public int TotalProductsSold { get; set; }
        public decimal TotalSalesValue { get; set; }
    }

    // ဝန်ထမ်းတစ်ဦးချင်းစီ၏ လစဉ်အရောင်း DTO
    public class StaffMonthlySalesDto
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public int ReportMonth { get; set; }
        public int ReportYear { get; set; }
        public int TotalProductsSold { get; set; }
        public decimal TotalSalesValue { get; set; }
    }

    // ဆိုင်တစ်ခုလုံး၏ နေ့စဉ်အရောင်း Summary DTO
    public class StoreDailySummaryDto
    {
        public DateOnly ReportDate { get; set; }
        public int ActiveStaffCount { get; set; }
        public int TotalProductsSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // ဆိုင်တစ်ခုလုံး၏ လစဉ်အရောင်း Summary DTO
    public class StoreMonthlySummaryDto
    {
        public int ReportMonth { get; set; }
        public int ReportYear { get; set; }
        public int TotalProductsSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // ဝန်ထမ်းများ၏ လုပ်ဆောင်ချက် (Activity Logs) ကို ပြသရန် DTO
    public class StaffActivityLogDto
    {
        public int LogId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string TargetTable { get; set; } = string.Empty;
        public int TargetId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AdminReportSummaryDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ConfirmOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal PaidRevenue { get; set; }
        public List<AdminReportOrderDto> Orders { get; set; } = new();
    }

    public class AdminReportOrderDto
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}
