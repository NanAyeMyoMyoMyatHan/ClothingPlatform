using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatformProject.Features.Report
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        // နေ့စဉ်အစီရင်ခံစာများ တွက်ချက်စုစည်းရန် Trigger ပေးသည့် API (ဥပမာ- ညဉ့်နက်ပိုင်းတွင် Run ရန်)
        [HttpPost("aggregate-daily")]
        public IActionResult TriggerDailyAggregation([FromQuery] DateTime date)
        {
            _reportService.GenerateDailyAggregatedSummaries(date);
            return Ok($"Daily aggregation completed for date: {date.ToShortDateString()}");
        }

        // ဝန်ထမ်းအလိုက် နေ့စဉ်အရောင်းမှတ်တမ်း ကြည့်ရန် API
        [HttpGet("staff-daily")]
        public IActionResult GetStaffDaily([FromQuery] DateTime date)
        {
            return Ok(_reportService.GetStaffDailySales(date));
        }

        // ဝန်ထမ်းအလိုက် လစဉ်အရောင်းမှတ်တမ်း ကြည့်ရန် API
        [HttpGet("staff-monthly")]
        public IActionResult GetStaffMonthly([FromQuery] int year, [FromQuery] int month)
        {
            return Ok(_reportService.GetStaffMonthlySales(year, month));
        }

        // ဆိုင်တစ်ခုလုံး၏ နေ့စဉ်အရောင်းအနှစ်ချုပ် ကြည့်ရန် API
        [HttpGet("store-daily")]
        public IActionResult GetStoreDaily([FromQuery] DateTime date)
        {
            var result = _reportService.GetStoreDailySummary(date);
            if (result == null) return NotFound("No summary found for the specified date.");
            return Ok(result);
        }

        // ဆိုင်တစ်ခုလုံး၏ လစဉ်အရောင်းအနှစ်ချုပ် ကြည့်ရန် API
        [HttpGet("store-monthly")]
        public IActionResult GetStoreMonthly([FromQuery] int year, [FromQuery] int month)
        {
            var result = _reportService.GetStoreMonthlySummary(year, month);
            if (result == null) return NotFound("No summary found for the specified month.");
            return Ok(result);
        }

        // ဝန်ထမ်းများ၏ စနစ်တွင်း လုပ်ဆောင်ချက် မှတ်တမ်းများကို စစ်ဆေးရန် API
        [HttpGet("staff-activities")]
        public IActionResult GetStaffActivities([FromQuery] DateTime date)
        {
            return Ok(_reportService.GetStaffActivityLogs(date));
        }
    }
}
