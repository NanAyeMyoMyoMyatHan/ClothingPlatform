using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Report;
using Microsoft.EntityFrameworkCore;
using ClothingPlatformProject.Models;

namespace ClothingPlatformProject.Features.Report
{
    public class ReportServices : IReportService
    {
        private readonly AppDbContext _db;
        public ReportServices(AppDbContext db)
        {
            _db = db;
        }

        // ၁။ နေ့စဉ် အရောင်းမှတ်တမ်းအသေးစိတ် (StaffSalesLogs) မှ ဒေတာများကို ယူ၍ Summary ဇယားများထဲသို့ Aggregation ပြုလုပ်သိမ်းဆည်းခြင်း Logic
        public void GenerateDailyAggregatedSummaries(DateTime date)
        {
            var targetDate =DateOnly.FromDateTime( date);
            var nextDate = targetDate.AddDays(1);

            // ထိုနေ့ရက်အတွက် ရှိသော Sales Logs များကို ဆွဲထုတ်ခြင်း
            var dailyLogs = _db.StaffSalesLogs
                .Where(s => s.SoldAt >= targetDate && s.SoldAt < nextDate)
                .ToList();

            if (!dailyLogs.Any()) return;

            // --- က။ ဝန်ထမ်းတစ်ဦးချင်းစီအလိုက် Group ဖွဲ့၍ staff_sales_daily ထဲသို့ သိမ်းခြင်း ---
            var staffGroups = dailyLogs.GroupBy(l => l.StaffId);
            foreach (var group in staffGroups)
            {
                var existingStaffReport = _db.StaffSalesDailies
                    .FirstOrDefault(r => r.StaffId == group.Key && r.ReportDate == targetDate);

                int totalQty = group.Sum(g => g.QuantitySold);
                decimal totalAmount = group.Sum(g => g.SaleAmount);

                if (existingStaffReport != null)
                {
                    existingStaffReport.TotalProductsSold = totalQty;
                    existingStaffReport.TotalSalesValue = totalAmount;
                }
                else
                {
                    _db.StaffSalesDailies.Add(new StaffSalesDaily
                    {
                        StaffId = group.Key,
                        ReportDate = targetDate,
                        TotalProductsSold = totalQty,
                        TotalSalesValue = totalAmount
                    });
                }

                // --- ခ။ staff_sales_monthly (လစဉ်ဇယား) အတွက်ပါ တစ်ခါတည်း Update လုပ်ခြင်း ---
                var existingStaffMonthly = _db.StaffSalesMonthlies
                    .FirstOrDefault(m => m.StaffId == group.Key && m.ReportMonth == targetDate.Month && m.ReportYear == targetDate.Year);

                if (existingStaffMonthly != null)
                {
                    // ယခင်လစဉ် စုစုပေါင်းထဲကို ယနေ့ရောင်းရငွေ ထပ်ပေါင်းထည့်ခြင်း (Idempotent ဖြစ်စေရန် နေ့စဉ်အဟောင်းကို နှုတ်ပြီးမှ ပေါင်းခြင်း သို့မဟုတ် Recalculate လုပ်ခြင်းက ပိုကောင်းသော်လည်း ဤနေရာတွင် ရိုးရှင်းစွာ ရေးသားထားပါသည်)
                    existingStaffMonthly.TotalProductsSold += totalQty;
                    existingStaffMonthly.TotalSalesValue += totalAmount;
                }
                else
                {
                    _db.StaffSalesMonthlies.Add(new StaffSalesMonthly
                    {
                        StaffId = group.Key,
                        ReportMonth = targetDate.Month,
                        ReportYear = targetDate.Year,
                        TotalProductsSold = totalQty,
                        TotalSalesValue = totalAmount
                    });
                }
            }

            // --- ဂ။ ဆိုင်တစ်ခုလုံးအတွက် store_sales_daily ထဲသို့ သိမ်းခြင်း ---
            var existingStoreReport = _db.StoreSalesDailies.FirstOrDefault(s => s.ReportDate == targetDate);
            int activeStaff = staffGroups.Count();
            int storeTotalQty = dailyLogs.Sum(l => l.QuantitySold);
            decimal storeTotalRevenue = dailyLogs.Sum(l => l.SaleAmount);

            if (existingStoreReport != null)
            {
                existingStoreReport.ActiveStaffCount = activeStaff;
                existingStoreReport.TotalProductsSold = storeTotalQty;
                existingStoreReport.TotalRevenue = storeTotalRevenue;
            }
            else
            {
                _db.StoreSalesDailies.Add(new StoreSalesDaily
                {
                    ReportDate = targetDate,
                    ActiveStaffCount = activeStaff,
                    TotalProductsSold = storeTotalQty,
                    TotalRevenue = storeTotalRevenue
                });
            }

            // --- ဃ။ ဆိုင်တစ်ခုလုံးအတွက် store_sales_monthly ထဲသို့ သိမ်းခြင်း ---
            var existingStoreMonthly = _db.StoreSalesMonthlies
                .FirstOrDefault(m => m.ReportMonth == targetDate.Month && m.ReportYear == targetDate.Year);

            if (existingStoreMonthly != null)
            {
                existingStoreMonthly.TotalProductsSold += storeTotalQty;
                existingStoreMonthly.TotalRevenue += storeTotalRevenue;
            }
            else
            {
                _db.StoreSalesMonthlies.Add(new StoreSalesMonthly
                {
                    ReportMonth = targetDate.Month,
                    ReportYear = targetDate.Year,
                    TotalProductsSold = storeTotalQty,
                    TotalRevenue = storeTotalRevenue
                });
            }

            _db.SaveChanges();
        }

        // ၂။ ဝန်ထမ်းအားလုံး၏ နေ့စဉ်အရောင်းမှတ်တမ်းများ ဆွဲထုတ်ခြင်း
        public List<StaffDailySalesDto> GetStaffDailySales(DateTime date)
        {
            var dateTime = DateOnly.FromDateTime(date);
            return _db.StaffSalesDailies
                .Include(s => s.Staff)
                .AsNoTracking()
                .Where(s => s.ReportDate == dateTime)
                .Select(s => new StaffDailySalesDto
                {
                    StaffId = s.StaffId,
                    StaffName = s.Staff != null ? $"{s.Staff.FirstName} {s.Staff.LastName}" : "Unknown Staff",
                    ReportDate = s.ReportDate,
                    TotalProductsSold = s.TotalProductsSold,
                    TotalSalesValue = s.TotalSalesValue
                }).ToList();
        }

        // ၃။ ဝန်ထမ်းအားလုံး၏ လစဉ်အရောင်းမှတ်တမ်းများ ဆွဲထုတ်ခြင်း
        public List<StaffMonthlySalesDto> GetStaffMonthlySales(int year, int month)
        {
            return _db.StaffSalesMonthlies
                .Include(s => s.Staff)
                .AsNoTracking()
                .Where(s => s.ReportYear == year && s.ReportMonth == month)
                .Select(s => new StaffMonthlySalesDto
                {
                    StaffId = s.StaffId,
                    StaffName = s.Staff != null ? $"{s.Staff.FirstName} {s.Staff.LastName}" : "Unknown Staff",
                    ReportMonth = s.ReportMonth,
                    ReportYear = s.ReportYear,
                    TotalProductsSold = s.TotalProductsSold,
                    TotalSalesValue = s.TotalSalesValue
                }).ToList();
        }

        // ၄။ ဆိုင်တစ်ခုလုံး၏ နေ့စဉ်အရောင်းအနှစ်ချုပ် ဆွဲထုတ်ခြင်း
        public StoreDailySummaryDto? GetStoreDailySummary(DateTime date)
        {
            var dateTime = DateOnly.FromDateTime(date);
            var summary = _db.StoreSalesDailies.AsNoTracking().FirstOrDefault(s => s.ReportDate == dateTime);
            if (summary == null) return null;

            return new StoreDailySummaryDto
            {
                ReportDate = summary.ReportDate,
                ActiveStaffCount = summary.ActiveStaffCount,
                TotalProductsSold = summary.TotalProductsSold,
                TotalRevenue = summary.TotalRevenue
            };
        }

        // ၅။ ဆိုင်တစ်ခုလုံး၏ လစဉ်အရောင်းအနှစ်ချုပ် ဆွဲထုတ်ခြင်း
        public StoreMonthlySummaryDto? GetStoreMonthlySummary(int year, int month)
        {
            var summary = _db.StoreSalesMonthlies.AsNoTracking().FirstOrDefault(s => s.ReportYear == year && s.ReportMonth == month);
            if (summary == null) return null;

            return new StoreMonthlySummaryDto
            {
                ReportMonth = summary.ReportMonth,
                ReportYear = summary.ReportYear,
                TotalProductsSold = summary.TotalProductsSold,
                TotalRevenue = summary.TotalRevenue
            };
        }

        // ၆။ ဝန်ထမ်းများ၏ Activity Logs ပြန်ကြည့်ခြင်း Logic
        public List<StaffActivityLogDto> GetStaffActivityLogs(DateTime date)
        {
            DateOnly targetDate = DateOnly.FromDateTime(date);
            var nextDate = targetDate.AddDays(1);

            return _db.StaffActivityLogs
                .Include(l => l.Staff)
                .AsNoTracking()
                .Where(l => l.CreatedAt >= targetDate && l.CreatedAt < nextDate)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new StaffActivityLogDto
                {
                    LogId = l.LogId,
                    StaffId = l.StaffId,
                    StaffName = l.Staff != null ? $"{l.Staff.FirstName} {l.Staff.LastName}" : "Unknown Staff",
                    TargetTable = l.TargetTable,
                    TargetId = l.TargetId,
                    ActionType = l.ActionType,
                    Description = l.Description,
                    CreatedAt = l.CreatedAt
                }).ToList();
        }
    }
}