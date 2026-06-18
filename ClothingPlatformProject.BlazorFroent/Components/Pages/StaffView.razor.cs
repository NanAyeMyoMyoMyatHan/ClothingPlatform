using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.BlazorFroent.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingPlatformProject.BlazorFroent.Components.Pages
{
    public partial class StaffView : ComponentBase
    {
        [Inject]
        public HttpClientServices HttpServices { get; set; } = default!;

        [Inject]
        public SessionState Session { get; set; } = default!;

        [Inject]
        public NavigationManager Nav { get; set; } = default!;

        private string activeView = "dashboard";
        private string currentFilter = "";
        private string currentDateString = "";
        private bool profileSaved = false;
        private string ordersTab = "regular";

        private string staffFirstName = "Thiri";
        private string staffLastName = "San";
        private string staffEmail = "thiri@boutique.com";
        private string staffPhone = "09222333444";
        private string staffRole = "Senior Staff";
        private string staffName = "Staff User";
        private string staffInitials => (staffFirstName.Length > 0 ? staffFirstName[0].ToString() : "")
                                       + (staffLastName.Length > 0 ? staffLastName[0].ToString() : "");

        private int totalOrdersCount = 0;
        private decimal totalRevenue = 0;
        private int totalSkusCount = 0;
        private int lowStockCount = 0;
        private int pendingCount = 0;
        private int processingCount = 0;
        private int deliveredCount = 0;

        private List<Order> allOrders = new();
        private List<Order> filteredOrders = new();
        private List<Order> recentOrders = new();
        private List<GuestOrder> allGuestOrders = new();
        private List<ProductVariant> inventoryVariants = new();

        private DateTime selectedReportDate = DateTime.Today;
        private decimal reportRevenue = 0;
        private int reportOrderCount = 0;
        private List<Order> reportOrders = new();
        // FIX: this list was never populated before, so phone/guest orders
        // were silently missing from the Daily Sales Report table even
        // though they were already counted in reportOrderCount/reportRevenue.
        private List<GuestOrder> reportGuestOrders = new();

        private string guestCustomerName = "";
        private string guestPhoneNumber = "";
        private string newOrderShippingAddress = "";
        private string guestPaymentMethod = "COD";
        private string guestPaymentStatus = "unpaid";
        private string createOrderError = "";

        private class OrderLineDraft
        {
            public int VariantId { get; set; } = 0;
            public int Quantity { get; set; } = 1;
        }
        private List<OrderLineDraft> orderLines = new() { new OrderLineDraft() };

        private class ToastItem { public string Message { get; set; } = ""; public bool IsError { get; set; } }
        private List<ToastItem> activeToasts = new();

        protected override async Task OnInitializedAsync()
        {
            if (!Session.IsLoggedIn)
            {
                Nav.NavigateTo("/login");
                return;
            }

            var user = Session.CurrentUser!;
            staffFirstName = user.FirstName;
            staffLastName = user.LastName;
            staffEmail = user.Email;
            staffName = $"{user.FirstName} {user.LastName}";
            staffRole = user.Role == "admin" ? "Boutique Owner" : "Senior Staff";
            currentDateString = DateTime.Now.ToString("ddd, d MMM yyyy");

            await LoadStaffDashboardDataAsync();
        }

        private async Task LoadStaffDashboardDataAsync()
        {
            try
            {
                var staffId = Session.CurrentUser!.UserId;
                var dateStr = selectedReportDate.ToString("yyyy-MM-dd");
                var data = await HttpServices.ExecuteAsync<ClothingPlatformProject.Features.Staff.StaffDashboardDataDto>(
                    $"api/Staff/dashboard/{staffId}?reportDate={dateStr}");

                if (data != null)
                {
                    allOrders = data.AllOrders ?? new List<Order>();
                    allGuestOrders = data.AllGuestOrders ?? new List<GuestOrder>();
                    inventoryVariants = data.InventoryVariants ?? new List<ProductVariant>();
                    reportOrders = data.ReportOrders ?? new List<Order>();
                    reportGuestOrders = data.ReportGuestOrders ?? new List<GuestOrder>();

                    reportOrderCount = reportOrders.Count + reportGuestOrders.Count;
                    reportRevenue = reportOrders.Sum(o => o.TotalAmount) + reportGuestOrders.Sum(g => g.TotalAmount);

                    recentOrders = allOrders.Take(5).ToList();
                    totalOrdersCount = allOrders.Count;
                    totalRevenue = allOrders.Sum(o => o.TotalAmount);
                    totalSkusCount = inventoryVariants.Count;
                    lowStockCount = inventoryVariants.Count(v => v.StockQuantity < 5);
                    pendingCount = allOrders.Count(o => o.OrderStatus == "pending");
                    processingCount = allOrders.Count(o => o.OrderStatus == "processing");
                    deliveredCount = allOrders.Count(o => o.OrderStatus == "delivered");
                }
            }
            catch (Exception ex)
            {
                TriggerToast($"Failed to load dashboard data. {ex.Message}", true);
                Console.WriteLine(ex.ToString());
            }

            ApplyFilter();
            StateHasChanged();
        }

        private void LoadSalesReport() { }

        private async Task OnReportDateChanged(ChangeEventArgs e)
        {
            if (DateTime.TryParseExact(e.Value?.ToString(), "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var parsed))
            {
                selectedReportDate = parsed.Date;
                await LoadStaffDashboardDataAsync();
            }
        }

        private async Task ResetReportToToday()
        {
            selectedReportDate = DateTime.Today;
            await LoadStaffDashboardDataAsync();
        }

        private async Task NavigateTo(string view)
        {
            activeView = view;
            profileSaved = false;
            await LoadStaffDashboardDataAsync();
        }

        private void FilterOrders(string status) { currentFilter = status; ApplyFilter(); }
        private void ApplyFilter()
        {
            filteredOrders = string.IsNullOrEmpty(currentFilter)
                ? allOrders
                : allOrders.Where(o => (o.OrderStatus ?? "").ToLower() == currentFilter).ToList();
        }

        private async Task UpdateOrderStatus(Order order, string? newStatus)
        {
            if (string.IsNullOrEmpty(newStatus)) return;
            var staffId = Session.CurrentUser!.UserId;
            try
            {
                await HttpServices.ExecuteAsync<object>($"api/Staff/order/{order.OrderId}/status?staffId={staffId}&newStatus={newStatus}", null, ClothingPlatformProject.BlazorFroent.Services.EnumHttpMethod.Post);
                await LoadStaffDashboardDataAsync();
                TriggerToast($"Order #ORD-{order.OrderId:D4} updated to <strong>{newStatus.ToUpper()}</strong>");
            }
            catch (Exception ex)
            {
                TriggerToast($"Failed to update status. {ex.Message}", true);
            }
        }

        private async Task UpdateGuestOrderStatus(GuestOrder guestOrder, string? newStatus)
        {
            if (string.IsNullOrEmpty(newStatus)) return;
            var staffId = Session.CurrentUser!.UserId;
            try
            {
                await HttpServices.ExecuteAsync<object>($"api/Staff/guestorder/{guestOrder.GuestOrderId}/status?staffId={staffId}&newStatus={newStatus}", null, ClothingPlatformProject.BlazorFroent.Services.EnumHttpMethod.Post);
                await LoadStaffDashboardDataAsync();
                TriggerToast($"Guest order #GORD-{guestOrder.GuestOrderId:D4} updated to <strong>{newStatus.ToUpper()}</strong>");
            }
            catch (Exception ex)
            {
                TriggerToast($"Failed to update status. {ex.Message}", true);
            }
        }

        private async Task AdjustStock(ProductVariant variant, int adjustment)
        {
            var staffId = Session.CurrentUser!.UserId;
            try
            {
                await HttpServices.ExecuteAsync<object>($"api/Staff/stock/adjust?variantId={variant.VariantId}&adjustment={adjustment}&staffId={staffId}", null, ClothingPlatformProject.BlazorFroent.Services.EnumHttpMethod.Post);
                await LoadStaffDashboardDataAsync();
                TriggerToast($"Stock adjusted for SKU <strong>{variant.Sku}</strong>");
            }
            catch (Exception ex)
            {
                TriggerToast($"Failed to adjust stock. {ex.Message}", true);
            }
        }

        // FIX: previously `v.Product?.BasePrice ?? 0 + (v.PriceModifier ?? 0)`.
        // Because ?? has lower precedence than +, this evaluated as
        // `v.Product?.BasePrice ?? (0 + (v.PriceModifier ?? 0))`, which meant
        // PriceModifier was completely ignored whenever Product was not null.
        private decimal GetVariantUnitPrice(int variantId)
        {
            var v = inventoryVariants.FirstOrDefault(x => x.VariantId == variantId);
            if (v == null) return 0;
            return (v.Product?.BasePrice ?? 0) + (v.PriceModifier ?? 0);
        }
        private decimal GetLineTotal(OrderLineDraft line) => GetVariantUnitPrice(line.VariantId) * line.Quantity;
        private decimal OrderLinesTotal => orderLines.Sum(l => GetLineTotal(l));

        private void OnLineVariantChanged(OrderLineDraft line, string? val) { if (int.TryParse(val, out var id)) line.VariantId = id; }
        private void OnLineQtyChanged(OrderLineDraft line, string? val) { if (int.TryParse(val, out var q) && q > 0) line.Quantity = q; }
        private void AddOrderLine() => orderLines.Add(new OrderLineDraft());
        private void RemoveOrderLine(OrderLineDraft line)
        {
            if (orderLines.Count > 1) orderLines.Remove(line);
            else { line.VariantId = 0; line.Quantity = 1; }
        }

        private void ResetCreateOrderForm()
        {
            guestCustomerName = "";
            guestPhoneNumber = "";
            newOrderShippingAddress = "";
            guestPaymentMethod = "COD";
            guestPaymentStatus = "unpaid";
            createOrderError = "";
            orderLines = new() { new OrderLineDraft() };
        }

        private async Task SubmitPhoneOrder()
        {
            createOrderError = "";

            if (string.IsNullOrWhiteSpace(guestCustomerName) || string.IsNullOrWhiteSpace(guestPhoneNumber))
            {
                createOrderError = "Please enter the guest customer's name and phone number.";
                return;
            }
            if (string.IsNullOrWhiteSpace(newOrderShippingAddress))
            {
                createOrderError = "Please enter a delivery address for this order.";
                return;
            }

            var validLines = orderLines.Where(l => l.VariantId > 0 && l.Quantity > 0).ToList();
            if (!validLines.Any())
            {
                createOrderError = "Please add at least one item to the order.";
                return;
            }

            var requestDto = new ClothingPlatformProject.Features.Staff.GuestOrderRequestDto
            {
                CustomerName = guestCustomerName,
                PhoneNumber = guestPhoneNumber,
                ShippingAddress = newOrderShippingAddress,
                PaymentMethod = guestPaymentMethod,
                PaymentStatus = guestPaymentStatus,
                OrderLines = validLines.Select(l => new ClothingPlatformProject.Features.Staff.OrderLineDraftDto { VariantId = l.VariantId, Quantity = l.Quantity }).ToList()
            };

            var staffId = Session.CurrentUser!.UserId;
            try
            {
                await HttpServices.ExecuteAsync<object>($"api/Staff/phoneorder?staffId={staffId}", requestDto, ClothingPlatformProject.BlazorFroent.Services.EnumHttpMethod.Post);

                TriggerToast($"Guest order created for <strong>{guestCustomerName}</strong>.");
                ResetCreateOrderForm();

                activeView = "orders";
                ordersTab = "guest";

                await LoadStaffDashboardDataAsync();
            }
            catch (Exception ex)
            {
                createOrderError = "Could not save this order. Ensure items are in stock.";
                TriggerToast($"Order creation failed. {ex.Message}", true);
            }
        }

        private async Task SaveProfile()
        {
            var staffId = Session.CurrentUser!.UserId;
            try
            {
                await HttpServices.ExecuteAsync<object>($"api/Staff/profile/{staffId}?firstName={staffFirstName}&lastName={staffLastName}&email={staffEmail}", null, ClothingPlatformProject.BlazorFroent.Services.EnumHttpMethod.Post);
                var user = Session.CurrentUser!;
                user.FirstName = staffFirstName;
                user.LastName = staffLastName;
                user.Email = staffEmail;
                Session.Login(user);
                profileSaved = true;
                TriggerToast("Staff profile saved successfully.");
            }
            catch (Exception ex)
            {
                TriggerToast($"Failed to save profile. {ex.Message}", true);
            }
        }

        private void Logout() { Session.Logout(); Nav.NavigateTo("/login"); }

        private void TriggerToast(string msg, bool isError = false)
        {
            var toast = new ToastItem { Message = msg, IsError = isError };
            activeToasts.Add(toast);
            StateHasChanged();
            Task.Delay(3500).ContinueWith(_ => { activeToasts.Remove(toast); InvokeAsync(StateHasChanged); });
        }

        private MarkupString HtmlRaw(string value) => new MarkupString(value);

        private MarkupString StatusBadge(string status) => status.ToLower() switch
        {
            "delivered" => new MarkupString("<span class=\"status-badge badge-delivered\"><i class=\"bi bi-check-circle\"></i> Delivered</span>"),
            "processing" => new MarkupString("<span class=\"status-badge badge-processing\"><i class=\"bi bi-arrow-repeat\"></i> Processing</span>"),
            _ => new MarkupString("<span class=\"status-badge badge-pending\"><i class=\"bi bi-hourglass-split\"></i> Pending</span>")
        };
    }
}

