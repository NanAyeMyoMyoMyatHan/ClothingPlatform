using ClothingPlatform.Api.Models.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatform.Api.Features.Order
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("history/{userId}")]
        public IActionResult GetHistory(int userId)
        {
            return Ok(_orderService.GetUserOrderHistory(userId));
        }

        [HttpPost("checkout")]
        public IActionResult Checkout(CheckoutRequest model)
        {
            var result = _orderService.PlaceOrderTransaction(model);
            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        [Authorize(Policy = "AdminOrStaff")]
        [HttpGet("getAllOrder")]
        public async Task<ActionResult<List<OrderDashboardDto>>> GetAllOrdersAsync()
        {
            return await _orderService.GetAllOrder();
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{orderId}")]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            var success = await _orderService.DeleteOrderAsync(orderId);
            if (!success) return NotFound("Order not found.");
            return Ok(true);
        }
    }
}
