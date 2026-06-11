using ClothingPlatformProject.Models.Cart;

namespace ClothingPlatformProject.Features.Cart
{
    public interface ICartService
    {
        CartDto? GetUserCart(int userId);
        void AddItemToCart(AddToCartRequest model);
        void RemoveItemFromCart(int cartItemId);
    }
}
