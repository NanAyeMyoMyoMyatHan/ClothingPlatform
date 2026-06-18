using ClothingPlatformProject.Models.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatformProject.Features.Order
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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

        [HttpGet("{getAllOrder}")]
        public async Task<ActionResult<List<OrderDashboardDto>>> GetAllOrdersAsync()
        {
            return await _orderService.GetAllOrder();
        }
    }
}
