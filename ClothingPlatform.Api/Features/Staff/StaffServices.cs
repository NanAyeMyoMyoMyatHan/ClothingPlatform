using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Api.Models.Order;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingPlatform.Api.Features.Staff
{
    public class StaffServices : IStaffService
    {
        private readonly AppDbContext _db;

        public StaffServices(AppDbContext db)
        {
            _db = db;
        }

        public async Task<StaffDashboardDataDto> GetDashboardDataAsync(int staffId, DateTime reportDate)
        {
            var operationalStaffId = await ResolveOperationalStaffIdAsync(staffId);
            var data = new StaffDashboardDataDto();

            data.AllOrders = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.Payments)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderId)
                .AsNoTracking()
                .ToListAsync();

            data.AllGuestOrders = await _db.GuestOrders
                .OrderByDescending(g => g.GuestOrderId)
                .AsNoTracking()
                .ToListAsync();

            data.InventoryVariants = await _db.ProductVariants
                .Include(v => v.Product)
                .AsNoTracking()
                .ToListAsync();

            // Explicitly break reference cycles for JSON serialization
            foreach (var order in data.AllOrders)
            {
                if (order.User != null)
                {
                    order.User.Orders = null;
                }
                if (order.Payments != null)
                {
                    foreach (var payment in order.Payments)
                    {
                        payment.Order = null;
                    }
                }
                if (order.OrderItems != null)
                {
                    foreach (var item in order.OrderItems)
                    {
                        item.Order = null;
                        if (item.Variant != null)
                        {
                            item.Variant.OrderItems = null;
                            if (item.Variant.Product != null)
                                item.Variant.Product.ProductVariants = null;
                        }
                    }
                }
            }

            foreach (var variant in data.InventoryVariants)
            {
                variant.OrderItems = null;
                if (variant.Product != null)
                {
                    variant.Product.ProductVariants = null;
                }
            }

            // Report logic for regular orders
            var orderIds = await _db.StaffFulfillmentLogs
                .Where(l => l.StaffId == operationalStaffId && l.ActionAt != null && l.ActionAt.Value.Date == reportDate.Date)
                .Select(l => l.OrderId)
                .Distinct()
                .ToListAsync();

            data.ReportOrders = data.AllOrders.Where(o => orderIds.Contains(o.OrderId)).ToList();

            // Report logic for guest orders
            var guestOrderIds = await _db.StaffActivityLogs
                .Where(l => l.StaffId == operationalStaffId && l.TargetTable == "guest_orders" && l.ActionType == "create" && l.CreatedAt != null && l.CreatedAt.Value.Date == reportDate.Date)
                .Select(l => l.TargetId)
                .Distinct()
                .ToListAsync();

            data.ReportGuestOrders = data.AllGuestOrders.Where(g => guestOrderIds.Contains(g.GuestOrderId)).ToList();

            return data;
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, int staffId, string newStatus)
        {
            var operationalStaffId = await ResolveOperationalStaffIdAsync(staffId);
            var dbOrder = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (dbOrder == null) return false;

            var normalizedStatus = OrderWorkflow.Normalize(newStatus);
            if (!OrderWorkflow.CanMoveTo(dbOrder.OrderStatus, normalizedStatus)) return false;

            dbOrder.OrderStatus = normalizedStatus;
            _db.StaffFulfillmentLogs.Add(new StaffFulfillmentLog
            {
                OrderId = orderId,
                StaffId = operationalStaffId,
                ActionTaken = normalizedStatus,
                ActionAt = DateTime.Now,
                Notes = "Status changed by operational staff member."
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateGuestOrderStatusAsync(int guestOrderId, int staffId, string newStatus)
        {
            var operationalStaffId = await ResolveOperationalStaffIdAsync(staffId);
            var dbGo = await _db.GuestOrders.FirstOrDefaultAsync(g => g.GuestOrderId == guestOrderId);
            if (dbGo == null) return false;

            var normalizedStatus = OrderWorkflow.Normalize(newStatus);
            if (!OrderWorkflow.CanMoveTo(dbGo.OrderStatus, normalizedStatus)) return false;

            dbGo.OrderStatus = normalizedStatus;

            // FIX: "update_status" was not an allowed value for the action_type
            // check constraint in the database, which caused SaveChangesAsync to
            // throw a SqlException whenever staff changed a guest order's status.
            // All other activity logs in this codebase use "create" / "update",
            // so we follow that same convention here.
            _db.StaffActivityLogs.Add(new StaffActivityLog
            {
                StaffId = operationalStaffId,
                TargetTable = "guest_orders",
                TargetId = guestOrderId,
                ActionType = "update",
                Description = $"Guest order status updated to {normalizedStatus}",
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AdjustStockAsync(int variantId, int adjustment, int staffId)
        {
            var operationalStaffId = await ResolveOperationalStaffIdAsync(staffId);
            var dbVariant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.VariantId == variantId);
            if (dbVariant == null) return false;

            int oldQty = dbVariant.StockQuantity;
            dbVariant.StockQuantity = Math.Max(0, dbVariant.StockQuantity + adjustment);

            _db.StaffActivityLogs.Add(new StaffActivityLog
            {
                StaffId = operationalStaffId,
                TargetTable = "product_variants",
                TargetId = variantId,
                ActionType = "update",
                Description = $"Adjusted SKU {dbVariant.Sku} stock from {oldQty} to {dbVariant.StockQuantity}.",
                CreatedAt = DateTime.Now
            });

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SubmitPhoneOrderAsync(GuestOrderRequestDto request, int staffId)
        {
            var operationalStaffId = await ResolveOperationalStaffIdAsync(staffId);
            var inventoryVariants = await _db.ProductVariants.Include(v => v.Product).ToListAsync();

            var demandByVariant = request.OrderLines
                .GroupBy(l => l.VariantId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            foreach (var kv in demandByVariant)
            {
                var dbVariant = inventoryVariants.FirstOrDefault(v => v.VariantId == kv.Key);
                if (dbVariant == null || kv.Value > dbVariant.StockQuantity) return false;
            }

            decimal orderTotal = 0;
            int totalQuantity = 0;
            foreach (var line in request.OrderLines)
            {
                var variant = inventoryVariants.FirstOrDefault(v => v.VariantId == line.VariantId);
                if (variant == null) continue;
                var unit = variant.Product.BasePrice + (variant.PriceModifier ?? 0);
                orderTotal += unit * line.Quantity;
                totalQuantity += line.Quantity;
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var guestOrder = new GuestOrder
                {
                    CustomerName = request.CustomerName.Trim(),
                    PhoneNumber = request.PhoneNumber.Trim(),
                    ShippingAddress = request.ShippingAddress.Trim(),
                    TotalQuantity = totalQuantity,
                    TotalAmount = orderTotal,
                    OrderStatus = OrderWorkflow.Pending,
                    CreatedAt = DateTime.Now,
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = request.PaymentStatus
                };

                _db.GuestOrders.Add(guestOrder);
                await _db.SaveChangesAsync();

                // Log the creation so it shows up in Sales Report
                _db.StaffActivityLogs.Add(new StaffActivityLog
                {
                    StaffId = operationalStaffId,
                    TargetTable = "guest_orders",
                    TargetId = guestOrder.GuestOrderId,
                    ActionType = "create",
                    Description = $"Guest order created by staff.",
                    CreatedAt = DateTime.Now
                });

                foreach (var kv in demandByVariant)
                {
                    var dbVariant = _db.ProductVariants.FirstOrDefault(v => v.VariantId == kv.Key);
                    if (dbVariant == null) continue;
                    int oldQty = dbVariant.StockQuantity;
                    dbVariant.StockQuantity = Math.Max(0, dbVariant.StockQuantity - kv.Value);

                    _db.StaffActivityLogs.Add(new StaffActivityLog
                    {
                        StaffId = operationalStaffId,
                        TargetTable = "product_variants",
                        TargetId = dbVariant.VariantId,
                        ActionType = "update",
                        Description = $"Stock reduced for SKU {dbVariant.Sku}: {oldQty} -> {dbVariant.StockQuantity} (guest order #GORD-{guestOrder.GuestOrderId:D4}).",
                        CreatedAt = DateTime.Now
                    });
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> UpdateProfileAsync(int staffId, string firstName, string lastName, string email)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == staffId &&
                    (u.Role.RoleName == "staff" || u.Role.RoleName == "admin"));
            if (user == null) return false;

            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;

            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<int> ResolveOperationalStaffIdAsync(int portalStaffId)
        {
            if (await _db.Users.AnyAsync(u => u.UserId == portalStaffId &&
                (u.Role.RoleName == "staff" || u.Role.RoleName == "admin")))
            {
                return portalStaffId;
            }

            var fallback = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Role.RoleName == "staff" || u.Role.RoleName == "admin");
            return fallback?.UserId ?? portalStaffId;
        }
    }
}
