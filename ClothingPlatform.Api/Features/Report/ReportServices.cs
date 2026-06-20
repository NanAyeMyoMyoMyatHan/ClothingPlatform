using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Api.Models.Order;
using ClothingPlatform.Api.Models.Report;
using Microsoft.EntityFrameworkCore;
using ClothingPlatform.Api.Models;

namespace ClothingPlatform.Api.Features.Report
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
            // 💡 အဓိကသော့ချက်- Database ထဲက DateTime? နှင့် နှိုင်းယှဉ်ရန်အတွက် DateTime Type ဖြင့်သာ Boundary သတ်မှတ်ပါမည်။
            DateTime targetDateStart = date.Date; // ဥပမာ - 2026-06-13 00:00:00.000
            DateTime targetDateEnd = targetDateStart.AddDays(1); // ဥပမာ - 2026-06-14 00:00:00.000

            // UI/Report ဇယားများအတွက် DateOnly ပြောင်းလဲခြင်း
            var reportDateOnly = DateOnly.FromDateTime(targetDateStart);

            // 🟢 အဆင့်မြှင့်ချက်- GroupBy ကို Database ဘက်မှာတင် တစ်ခါတည်း တွက်ခိုင်းလိုက်ခြင်းဖြင့် သာလွန်ကောင်းမွန်သော Performance ရရှိစေပါသည်။
            var dailySummaryFromDb = _db.StaffSalesLogs
                .Where(s => s.SoldAt >= targetDateStart && s.SoldAt < targetDateEnd)
                .GroupBy(l => l.StaffId)
                .Select(group => new
                {
                    StaffId = group.Key,
                    TotalQty = group.Sum(g => g.QuantitySold),
                    TotalAmount = group.Sum(g => g.SaleAmount)
                })
                .ToList();

            if (!dailySummaryFromDb.Any()) return;

            // ဆိုင်တစ်ခုလုံးအတွက် စုစုပေါင်း တွက်ချက်ရန် Variable များ
            int activeStaff = dailySummaryFromDb.Count;
            int storeTotalQty = dailySummaryFromDb.Sum(x => x.TotalQty);
            decimal storeTotalRevenue = dailySummaryFromDb.Sum(x => x.TotalAmount);

            // --- က။ ဝန်ထမ်းတစ်ဦးချင်းစီအလိုက် ပတ်၍ နေ့စဉ်/လစဉ် ဇယားများသိမ်းခြင်း ---
            foreach (var staffData in dailySummaryFromDb)
            {
                // ၁။ staff_sales_daily Update သို့မဟုတ် Add လုပ်ခြင်း
                var existingStaffReport = _db.StaffSalesDailies
                    .FirstOrDefault(r => r.StaffId == staffData.StaffId && r.ReportDate == reportDateOnly);

                if (existingStaffReport != null)
                {
                    existingStaffReport.TotalProductsSold = staffData.TotalQty;
                    existingStaffReport.TotalSalesValue = staffData.TotalAmount;
                }
                else
                {
                    _db.StaffSalesDailies.Add(new StaffSalesDaily
                    {
                        StaffId = staffData.StaffId,
                        ReportDate = reportDateOnly,
                        TotalProductsSold = staffData.TotalQty,
                        TotalSalesValue = staffData.TotalAmount
                    });
                }

                // ၂။ staff_sales_monthly (လစဉ်ဇယား) အတွက်ပါ တစ်ခါတည်း Update လုပ်ခြင်း
                var existingStaffMonthly = _db.StaffSalesMonthlies
                    .FirstOrDefault(m => m.StaffId == staffData.StaffId && m.ReportMonth == reportDateOnly.Month && m.ReportYear == reportDateOnly.Year);

                if (existingStaffMonthly != null)
                {
                    // 💡 မှတ်ချက်- နေ့စဉ်အဟောင်းကို နှုတ်ပြီးမှ ပေါင်းခြင်း Logic ကို သုံးနိုင်ရန် ဤနေရာတွင် ရိုးရှင်းစွာ ဆက်လက်ထားရှိပါသည်
                    existingStaffMonthly.TotalProductsSold += staffData.TotalQty;
                    existingStaffMonthly.TotalSalesValue += staffData.TotalAmount;
                }
                else
                {
                    _db.StaffSalesMonthlies.Add(new StaffSalesMonthly
                    {
                        StaffId = staffData.StaffId,
                        ReportMonth = reportDateOnly.Month,
                        ReportYear = reportDateOnly.Year,
                        TotalProductsSold = staffData.TotalQty,
                        TotalSalesValue = staffData.TotalAmount
                    });
                }
            }

            // --- ခ။ ဆိုင်တစ်ခုလုံးအတွက် store_sales_daily ထဲသို့ သိမ်းခြင်း ---
            var existingStoreReport = _db.StoreSalesDailies.FirstOrDefault(s => s.ReportDate == reportDateOnly);

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
                    ReportDate = reportDateOnly,
                    ActiveStaffCount = activeStaff,
                    TotalProductsSold = storeTotalQty,
                    TotalRevenue = storeTotalRevenue
                });
            }

            // --- ဂ။ ဆိုင်တစ်ခုလုံးအတွက် store_sales_monthly ထဲသို့ သိမ်းခြင်း ---
            var existingStoreMonthly = _db.StoreSalesMonthlies
                .FirstOrDefault(m => m.ReportMonth == reportDateOnly.Month && m.ReportYear == reportDateOnly.Year);

            if (existingStoreMonthly != null)
            {
                existingStoreMonthly.TotalProductsSold += storeTotalQty;
                existingStoreMonthly.TotalRevenue += storeTotalRevenue;
            }
            else
            {
                _db.StoreSalesMonthlies.Add(new StoreSalesMonthly
                {
                    ReportMonth = reportDateOnly.Month,
                    ReportYear = reportDateOnly.Year,
                    TotalProductsSold = storeTotalQty,
                    TotalRevenue = storeTotalRevenue
                });
            }

            // အားလုံးပြီးမှ Database ဆီသို့ Commit တစ်ခါတည်း လုပ်ခြင်း
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
            // 💡 အဓိကဖြေရှင်းနည်း- နှိုင်းယှဉ်မှု မှန်ကန်စေရန် DateTime Boundary ကိုသာ အသုံးပြုပါမည်
            DateTime targetDateStart = date.Date; // ဥပမာ - 2026-06-13 00:00:00.000
            DateTime targetDateEnd = targetDateStart.AddDays(1); // ဥပမာ - 2026-06-14 00:00:00.000

            return _db.StaffActivityLogs
                .AsNoTracking() // Read-only ဖြစ်လို့ Performance တက်အောင် သုံးထားတာ အရမ်းမှန်ပါတယ်ဗျာ
                .Where(l => l.CreatedAt >= targetDateStart && l.CreatedAt < targetDateEnd)
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => new StaffActivityLogDto
                {
                    LogId = l.LogId,
                    StaffId = l.StaffId,
                    // 🟢 Null Conditional စစ်ပြီး ဝန်ထမ်းနာမည်ကို တစ်ခါတည်း ပေါင်းထုတ်ခြင်း
                    StaffName = l.Staff != null ? l.Staff.FirstName + " " + l.Staff.LastName : "Unknown Staff",
                    TargetTable = l.TargetTable,
                    TargetId = l.TargetId,
                    ActionType = l.ActionType,
                    Description = l.Description,
                    CreatedAt = l.CreatedAt
                })
                .ToList();
        }

        public AdminReportSummaryDto GetAdminReport(DateTime from, DateTime to)
        {
            var start = from.Date;
            var end = to.Date.AddDays(1);

            var orders = _db.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.Payments)
                .Where(o => o.CreatedAt >= start && o.CreatedAt < end)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            var rows = orders.Select(o => new AdminReportOrderDto
            {
                OrderId = o.OrderId,
                OrderDate = o.CreatedAt,
                CustomerName = o.User != null ? $"{o.User.FirstName} {o.User.LastName}".Trim() : "Customer",
                OrderStatus = OrderWorkflow.Normalize(o.OrderStatus),
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.Payments.FirstOrDefault()?.PaymentMethod ?? "cod",
                TotalAmount = o.TotalAmount
            }).ToList();

            return new AdminReportSummaryDto
            {
                From = start,
                To = to.Date,
                TotalOrders = rows.Count,
                PendingOrders = rows.Count(o => o.OrderStatus == OrderWorkflow.Pending),
                ProcessingOrders = rows.Count(o => o.OrderStatus == OrderWorkflow.Processing),
                ConfirmOrders = rows.Count(o => o.OrderStatus == OrderWorkflow.Confirm),
                TotalRevenue = rows.Sum(o => o.TotalAmount),
                PaidRevenue = rows.Where(o =>
                        string.Equals(o.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(o.PaymentStatus, "completed", StringComparison.OrdinalIgnoreCase))
                    .Sum(o => o.TotalAmount),
                Orders = rows
            };
        }
    }
}
