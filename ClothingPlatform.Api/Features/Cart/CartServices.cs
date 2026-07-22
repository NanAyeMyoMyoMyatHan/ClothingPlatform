using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Api.Models.Cart;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatform.Api.Features.Cart
{
    public class CartServices : ICartService
    {
        private readonly AppDbContext _db;

        public CartServices(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CartDto> GetUserCartAsync(int userId)
        {
            return new CartDto
            {
                CartId = 0,
                UserId = userId,
                Items = await BuildCartItemsQuery(userId).ToListAsync()
            };
        }

        public async Task<CartDto> AddItemToCartAsync(AddToCartRequest model)
        {
            if (model.Quantity <= 0)
            {
                model.Quantity = 1;
            }

            var variant = await _db.ProductVariants.AsNoTracking()
                .FirstOrDefaultAsync(v => v.VariantId == model.VariantId);
            if (variant == null)
            {
                throw new InvalidOperationException("Selected product variant does not exist.");
            }

            var existingItem = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.UserId == model.UserId && ci.VariantId == model.VariantId);

            if (existingItem != null)
            {
                existingItem.Quantity = Math.Min(variant.StockQuantity, existingItem.Quantity + model.Quantity);
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserId = model.UserId,
                    VariantId = model.VariantId,
                    Quantity = Math.Min(variant.StockQuantity, model.Quantity)
                });
            }
            await _db.SaveChangesAsync();

            return await GetUserCartAsync(model.UserId);
        }

        public async Task<CartDto?> UpdateItemQuantityAsync(int cartItemId, int quantity)
        {
            var item = await _db.CartItems.FirstOrDefaultAsync(ci => ci.CartId == cartItemId);
            if (item == null) return null;

            if (quantity <= 0)
            {
                _db.CartItems.Remove(item);
            }
            else
            {
                var stock = await _db.ProductVariants
                    .Where(v => v.VariantId == item.VariantId)
                    .Select(v => v.StockQuantity)
                    .FirstOrDefaultAsync();

                item.Quantity = Math.Min(stock, quantity);
            }

            await _db.SaveChangesAsync();
            return await GetUserCartAsync(item.UserId);
        }

        public async Task RemoveItemFromCartAsync(int cartItemId)
        {
            var item = await _db.CartItems.FirstOrDefaultAsync(ci => ci.CartId == cartItemId);
            if (item == null) return;

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }

        public async Task ClearUserCartAsync(int userId)
        {
            var items = await _db.CartItems.Where(ci => ci.UserId == userId).ToListAsync();
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }

        private IQueryable<CartItemDto> BuildCartItemsQuery(int userId)
        {
            return _db.CartItems
                .AsNoTracking()
                .Where(ci => ci.UserId == userId)
                .OrderBy(ci => ci.CartId)
                .Select(ci => new CartItemDto
                {
                    CartItemId = ci.CartId,
                    VariantId = ci.VariantId,
                    Quantity = ci.Quantity,
                    ProductName = ci.Variant.Product.Name,
                    Size = ci.Variant.Size,
                    Color = ci.Variant.Color,
                    UnitPrice = ci.Variant.SalePrice ?? 0,
                    ImageUrl = ci.Variant.Product.ProductImages
                        .Where(i => i.IsPrimary == true)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault() ?? string.Empty
                });
        }
    }
}
