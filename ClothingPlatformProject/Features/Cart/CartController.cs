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
        public IActionResult GetCart(int userId)
        {
            var result = _cartService.GetUserCart(userId);
            if (result == null) return NotFound("Cart is empty");
            return Ok(result);
        }

        [HttpPost("add")]
        public IActionResult AddToCart(AddToCartRequest model)
        {
            _cartService.AddItemToCart(model);
            return Ok("Added to Cart successfully");
        }

        [HttpDelete("item/{cartItemId}")]
        public IActionResult RemoveItem(int cartItemId)
        {
            _cartService.RemoveItemFromCart(cartItemId);
            return Ok("Item removed successfully");
        }
    }
}
