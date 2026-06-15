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
        public async Task<List<OrderDashboardDto>> GetAllOrder()
        {
            return await _db.Orders
                    .AsNoTracking() // 💡 Read-only ဖြစ်၍ Performance ပိုမြန်စေရန်
                    .Include(o => o.User)
                    .Include(o => o.Payments)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Variant)
                            .ThenInclude(v => v.Product)
                    .OrderByDescending(o => o.OrderId)
                    // 🟢 အမှန်ပြင်ဆင်ချက်: Entity (Order) မှ DTO (OrderDashboardDto) သို့ ဒေတာများ ပြောင်းလဲပေးခြင်း
                    .Select(o => new OrderDashboardDto
                    {
                        OrderId = o.OrderId,
                        UserId = o.UserId,
                        // 🟢 အမှန်ပြင်ဆင်ချက်: FirstName နှင့် LastName ကို တစ်ခါတည်း တွဲပေးလိုက်ခြင်း
                        UserName = o.User != null ? $"{o.User.FirstName} {o.User.LastName}".Trim() : "Guest",
                        UserEmail = o.User != null ? o.User.Email : string.Empty,
                        OrderDate = o.CreatedAt,
                        TotalAmount = o.TotalAmount,
                        OrderStatus = o.OrderStatus,
                        PaymentStatus = o.PaymentStatus,
                        ShippingAddress = o.ShippingAddress,
                        PhoneNumber = o.User.PhoneNumber,
                        

                        // Payments များကို Mapping လုပ်ခြင်း
                        Payments = o.Payments.Select(p => new OrderPaymentDto
                        {
                            PaymentId = p.PaymentId,
                            PaymentMethod = p.PaymentMethod,
                            TransactionNumber = p.TransactionId,
                            AmountPaid = p.Amount,
                            PaymentDate = p.CreatedAt,
                            Status = p.PaymentStatus
                        }).ToList(),

                        // OrderItems များကို Mapping လုပ်ခြင်း
                        OrderItems = o.OrderItems.Select(oi => new OrderItemDashboardDto
                        {
                            OrderItemId = oi.OrderItemId,
                            VariantId = oi.VariantId,
                            ProductId = oi.Variant != null ? oi.Variant.ProductId : 0,
                            ProductName = oi.Variant != null && oi.Variant.Product != null ? oi.Variant.Product.Name : "Unknown Product",
                            Size = oi.Variant != null ? oi.Variant.Size : string.Empty,
                            Color = oi.Variant != null ? oi.Variant.Color : string.Empty,
                            Sku = oi.Variant != null ? oi.Variant.Sku : string.Empty,
                            PricePerUnit = oi.PriceAtPurchase,
                            Quantity = oi.Quantity
                        }).ToList()
                    })
                    .ToListAsync();
        }

    }
}
