using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Order;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatformProject.Features.Order
{
    public class OrderServices : IOrderService
    {
        private readonly AppDbContext _db;

        public OrderServices(AppDbContext db)
        {
            _db = db;
        }

        public List<OrderHistoryDto> GetUserOrderHistory(int userId)
        {
            return _db.Orders.AsNoTracking()
                .Where(o => o.UserId == userId)
                .Select(o => new OrderHistoryDto
                {
                    OrderId = o.OrderId,
                    TotalPrice = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    ShippingAddress = o.ShippingAddress,
                    CreatedAt = (DateTime)o.CreatedAt
                }).ToList();
        }

        public OrderResultDto PlaceOrderTransaction(CheckoutRequest model)
        {
            // ၁။ User ရဲ့ Address ကို DB မှ ဆွဲထုတ်ယူပြီး စာသား (string) အဖြစ် ပြောင်းလဲခြင်း
            string fullAddress = !string.IsNullOrEmpty(model.ShippingAddress)
                ? model.ShippingAddress
                : "Default Store Pickup Address";

            // ၂။ Order Header ကို သိမ်းဆည်းခြင်း
            var order = new ClothingPlatform.DB.AppDbModels.Order
            {
                UserId = model.UserId,
                TotalAmount = model.TotalPrice,
                OrderStatus = "processing",
                ShippingAddress = fullAddress
            };
            _db.Orders.Add(order);
            _db.SaveChanges(); // OrderId သတ်မှတ်ချက် အရင်ရယူရန်

            // ၃။ ပစ္စည်းတစ်ခုချင်းစီအား OrderItems ထဲသို့ ထည့်သွင်းပြီး Stock မှ နှုတ်ခြင်း
            foreach (var item in model.Items)
            {
                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    PriceAtPurchase = item.Price
                });

                // Stock Update ပြုလုပ်ခြင်း
                var variant = _db.ProductVariants.FirstOrDefault(v => v.VariantId == item.VariantId);
                if (variant != null)
                {
                    variant.StockQuantity -= item.Quantity;
                }
            }

            // ၄။ ငွေပေးချေမှုမှတ်တမ်း (Payments) ထဲသို့ မှတ်တမ်းသွင်းခြင်း
            _db.Payments.Add(new Payment
            {
                OrderId = order.OrderId,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = "completed",
                Amount = model.TotalPrice
            });

            _db.SaveChanges();

            return new OrderResultDto
            {
                OrderId = order.OrderId,
                IsSuccess = true,
                Message = "Order and Payment checkout process completed."
            };
        }
    }
}
