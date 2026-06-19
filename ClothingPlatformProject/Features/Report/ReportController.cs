using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ClothingPlatformProject.Features.Report
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Reports.Generate")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost("aggregate-daily")]
        public IActionResult TriggerDailyAggregation([FromQuery] DateTime date)
        {
            _reportService.GenerateDailyAggregatedSummaries(date);
            return Ok($"Daily aggregation completed for date: {date.ToShortDateString()}");
        }

        [HttpGet("staff-daily")]
        public IActionResult GetStaffDaily([FromQuery] DateTime date)
        {
            return Ok(_reportService.GetStaffDailySales(date));
        }

        [HttpGet("staff-monthly")]
        public IActionResult GetStaffMonthly([FromQuery] int year, [FromQuery] int month)
        {
            return Ok(_reportService.GetStaffMonthlySales(year, month));
        }

        [HttpGet("store-daily")]
        public IActionResult GetStoreDaily([FromQuery] DateTime date)
        {
            var result = _reportService.GetStoreDailySummary(date);
            if (result == null) return NotFound("No summary found for the specified date.");
            return Ok(result);
        }

        [HttpGet("store-monthly")]
        public IActionResult GetStoreMonthly([FromQuery] int year, [FromQuery] int month)
        {
            var result = _reportService.GetStoreMonthlySummary(year, month);
            if (result == null) return NotFound("No summary found for the specified month.");
            return Ok(result);
        }

        [HttpGet("staff-activities")]
        public IActionResult GetStaffActivities([FromQuery] DateTime date)
        {
            return Ok(_reportService.GetStaffActivityLogs(date));
        }

        [HttpGet("admin")]
        public IActionResult GetAdminReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var end = to ?? DateTime.Today;
            var start = from ?? end.AddDays(-30);
            return Ok(_reportService.GetAdminReport(start, end));
        }

        [HttpGet("admin.csv")]
        public IActionResult GetAdminReportCsv([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var end = to ?? DateTime.Today;
            var start = from ?? end.AddDays(-30);
            var report = _reportService.GetAdminReport(start, end);

            var csv = new StringBuilder();
            csv.AppendLine("OrderId,OrderDate,CustomerName,OrderStatus,PaymentStatus,PaymentMethod,TotalAmount");
            foreach (var row in report.Orders)
            {
                csv.AppendLine(string.Join(",",
                    row.OrderId,
                    Csv(row.OrderDate?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty),
                    Csv(row.CustomerName),
                    Csv(row.OrderStatus),
                    Csv(row.PaymentStatus),
                    Csv(row.PaymentMethod),
                    row.TotalAmount.ToString("0.00")));
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"admin-report-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.csv");
        }

        private static string Csv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
