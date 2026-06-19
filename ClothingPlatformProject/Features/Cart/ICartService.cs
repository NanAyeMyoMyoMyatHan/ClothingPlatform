using ClothingPlatformProject.Models.Cart;

namespace ClothingPlatformProject.Features.Cart
{
    public interface ICartService
    {
        Task<CartDto> GetUserCartAsync(int userId);
        Task<CartDto> AddItemToCartAsync(AddToCartRequest model);
        Task<CartDto?> UpdateItemQuantityAsync(int cartItemId, int quantity);
        Task RemoveItemFromCartAsync(int cartItemId);
        Task ClearUserCartAsync(int userId);
    }
}
