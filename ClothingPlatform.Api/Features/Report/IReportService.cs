using ClothingPlatform.Api.Models.Report;

namespace ClothingPlatform.Api.Features.Report
{
    public interface IReportService
    {
        void GenerateDailyAggregatedSummaries(DateTime date);

        // ဝန်ထမ်းအားလုံး၏ နေ့စဉ်အရောင်းမှတ်တမ်းများ ကြည့်ရန်
        List<StaffDailySalesDto> GetStaffDailySales(DateTime date);

        // ဝန်ထမ်းအားလုံး၏ လစဉ်အရောင်းမှတ်တမ်းများ ကြည့်ရန်
        List<StaffMonthlySalesDto> GetStaffMonthlySales(int year, int month);

        // ဆိုင်တစ်ခုလုံး၏ နေ့စဉ်အရောင်းအနှစ်ချုပ် ကြည့်ရန်
        StoreDailySummaryDto? GetStoreDailySummary(DateTime date);

        // ဆိုင်တစ်ခုလုံး၏ လစဉ်အရောင်းအနှစ်ချုပ် ကြည့်ရန်
        StoreMonthlySummaryDto? GetStoreMonthlySummary(int year, int month);

        // ဝန်ထမ်းများ၏ စနစ်တွင်းအသုံးပြုမှု Activity Logs များအား စစ်ဆေးရန်
        List<StaffActivityLogDto> GetStaffActivityLogs(DateTime date);

        AdminReportSummaryDto GetAdminReport(DateTime from, DateTime to);

        // Stock report: how many units were added vs sold per product variant
        StockReportSummaryDto GetStockReport(string? categoryFilter = null, string? searchTerm = null, DateTime? from = null, DateTime? to = null);
    }
}
