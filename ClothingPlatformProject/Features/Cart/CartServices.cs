using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Cart;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatformProject.Features.Cart
{
    public class CartServices : ICartService
    {
        private readonly AppDbContext _db;

        public CartServices(AppDbContext db)
        {
            _db = db;
        }

        public CartDto? GetUserCart(int userId)
        {
            var cart = _db.CartItems.AsNoTracking().FirstOrDefault(c => c.UserId == userId);
            if (cart == null) return null;

            return new CartDto
            {
                CartId = cart.CartId,
                UserId = cart.UserId,
                Items = _db.CartItems.AsNoTracking()
                    .Where(ci => ci.CartId == cart.CartId)
                    .Select(ci => new CartItemDto
                    {
                        CartItemId = ci.CartId,
                        VariantId = ci.VariantId,
                        Quantity = ci.Quantity
                    }).ToList()
            };
        }

        public void AddItemToCart(AddToCartRequest model)
        {
            // ခြင်းတောင်း (Cart) ရှိမရှိ အရင်စစ်ဆေးပြီး မရှိလျှင် အသစ်ဆောက်သည်
            var cart = _db.CartItems.FirstOrDefault(c => c.UserId == model.UserId);
            if (cart == null)
            {
                cart = new ClothingPlatform.DB.AppDbModels.CartItem { UserId = model.UserId };
                _db.CartItems.Add(cart);
                _db.SaveChanges();
            }

            // Cart ထဲတွင် ထိုပစ္စည်း (Variant) ရှိပြီးသားဆိုလျှင် အရေအတွက်ပေါင်းထည့်သည်၊ မရှိလျှင် အသစ်ထည့်သည်
            var existingItem = _db.CartItems.FirstOrDefault(ci => ci.CartId == cart.CartId && ci.VariantId == model.VariantId);
            if (existingItem != null)
            {
                existingItem.Quantity += model.Quantity;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    CartId = cart.CartId,
                    VariantId = model.VariantId,
                    Quantity = model.Quantity
                });
            }
            _db.SaveChanges();
        }

        public void RemoveItemFromCart(int cartItemId)
        {
            var item = _db.CartItems.FirstOrDefault(ci => ci.CartId == cartItemId);
            if (item == null) return;

            _db.CartItems.Remove(item);
            _db.SaveChanges();
        }
    }
}
