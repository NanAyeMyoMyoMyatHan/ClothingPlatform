using ClothingPlatformProject.Models.Cart;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClothingPlatformProject.Features.Cart
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetCart(int userId)
        {
            var result = await _cartService.GetUserCartAsync(userId);
            return Ok(result);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(AddToCartRequest model)
        {
            var result = await _cartService.AddItemToCartAsync(model);
            return Ok(result);
        }

        [HttpPut("item/{cartItemId}")]
        public async Task<IActionResult> UpdateItem(int cartItemId, UpdateCartItemRequest model)
        {
            var result = await _cartService.UpdateItemQuantityAsync(cartItemId, model.Quantity);
            if (result == null) return NotFound("Cart item not found.");
            return Ok(result);
        }

        [HttpDelete("item/{cartItemId}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            await _cartService.RemoveItemFromCartAsync(cartItemId);
            return Ok("Item removed successfully");
        }

        [HttpDelete("user/{userId}/clear")]
        public async Task<IActionResult> ClearCart(int userId)
        {
            await _cartService.ClearUserCartAsync(userId);
            return Ok("Cart cleared successfully");
        }
    }
}
