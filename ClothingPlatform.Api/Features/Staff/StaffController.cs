using Microsoft.AspNetCore.Mvc;
using ClothingPlatform.Api.Features.Staff;
using Microsoft.AspNetCore.Authorization;
using ClothingPlatform.Api.Filters;
using System;
using System.Threading.Tasks;

namespace ClothingPlatform.Api.Features.Staff
{
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet("dashboard/{staffId}")]
        [Permission("dashboard.view")]
        public async Task<IActionResult> GetDashboardData(int staffId, [FromQuery] DateTime reportDate)
        {
            var data = await _staffService.GetDashboardDataAsync(staffId, reportDate);
            return Ok(data);
        }

        [HttpPost("order/{orderId}/status")]
        [Permission("orders.update")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromQuery] int staffId, [FromQuery] string newStatus)
        {
            var success = await _staffService.UpdateOrderStatusAsync(orderId, staffId, newStatus);
            if (!success) return BadRequest("Order not found or update failed.");
            return Ok();
        }

        [HttpPost("guestorder/{guestOrderId}/status")]
        [Permission("orders.update")]
        public async Task<IActionResult> UpdateGuestOrderStatus(int guestOrderId, [FromQuery] int staffId, [FromQuery] string newStatus)
        {
            var success = await _staffService.UpdateGuestOrderStatusAsync(guestOrderId, staffId, newStatus);
            if (!success) return BadRequest("Guest Order not found or update failed.");
            return Ok();
        }

        [HttpPost("stock/adjust")]
        [Permission("products.manage")]
        public async Task<IActionResult> AdjustStock([FromQuery] int variantId, [FromQuery] int adjustment, [FromQuery] int staffId)
        {
            var success = await _staffService.AdjustStockAsync(variantId, adjustment, staffId);
            if (!success) return BadRequest("Variant not found or update failed.");
            return Ok();
        }

        [HttpPost("stock/restock")]
        [Permission("products.manage")]
        public async Task<IActionResult> RestockVariant([FromQuery] int variantId, [FromQuery] int quantity, [FromQuery] decimal purchasePrice, [FromQuery] string notes, [FromQuery] int staffId)
        {
            var success = await _staffService.RestockVariantAsync(variantId, quantity, purchasePrice, notes, staffId);
            if (!success) return BadRequest("Variant not found or restock failed.");
            return Ok();
        }

        [HttpPost("phoneorder")]
        [Permission("orders.create")]
        public async Task<IActionResult> SubmitPhoneOrder([FromBody] GuestOrderRequestDto request, [FromQuery] int staffId)
        {
            var success = await _staffService.SubmitPhoneOrderAsync(request, staffId);
            if (!success) return BadRequest("Phone order submission failed (e.g. out of stock or invalid items).");
            return Ok();
        }

        [HttpPost("profile/{staffId}")]
        [Permission("dashboard.view")]
        public async Task<IActionResult> UpdateProfile(int staffId, [FromQuery] string firstName, [FromQuery] string lastName, [FromQuery] string email, [FromQuery] string? password = null)
        {
            var success = await _staffService.UpdateProfileAsync(staffId, firstName, lastName, email, password);
            if (!success) return BadRequest("Staff profile update failed.");
            return Ok();
        }
    }
}
