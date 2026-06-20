using ClothingPlatform.Api.Models.Order;
using ClothingPlatform.DB.AppDbModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ClothingPlatform.Web.Services
{
    public sealed class StaffPortalState
    {
        private readonly HttpClientServices _httpServices;
        private readonly SessionState _session;
        private readonly NavigationManager _nav;
        private readonly IPortalSessionBootstrapper _portalSessionBootstrapper;
        private readonly IJSRuntime _jsRuntime;

        private readonly List<ToastItem> _activeToasts = new();
        private DateTime _selectedReportDate = DateTime.Today;
        private List<Order> _reportOrders = new();
        private List<GuestOrder> _reportGuestOrders = new();
        private string _regularOrderSearch = "";
        private string _guestOrderSearch = "";
        private string _guestPaymentFilter = "";
        private string _productSearch = "";

        public StaffPortalState(
            HttpClientServices httpServices,
            SessionState session,
            NavigationManager nav,
            IPortalSessionBootstrapper portalSessionBootstrapper,
            IJSRuntime jsRuntime)
        {
            _httpServices = httpServices;
            _session = session;
            _nav = nav;
            _portalSessionBootstrapper = portalSessionBootstrapper;
            _jsRuntime = jsRuntime;
        }

        public event Action? StateChanged;

        public bool IsPortalReady { get; private set; }
        public bool ShowLogoutConfirm { get; private set; }
        public string CurrentFilter { get; private set; } = "";
        public string CurrentDateString { get; private set; } = "";
        public bool ProfileSaved { get; set; }
        public string OrdersTab { get; set; } = "regular";
        public string RegularOrderSearch
        {
            get => _regularOrderSearch;
            set
            {
                var next = value ?? "";
                if (_regularOrderSearch == next)
                {
                    return;
                }

                _regularOrderSearch = next;
                OrderPage = 1;
                ApplyFilter();
                ApplyAllPaging();
                NotifyStateChanged();
            }
        }

        public string GuestOrderSearch
        {
            get => _guestOrderSearch;
            set
            {
                var next = value ?? "";
                if (_guestOrderSearch == next)
                {
                    return;
                }

                _guestOrderSearch = next;
                GuestOrderPage = 1;
                ApplyGuestFilter();
                ApplyAllPaging();
                NotifyStateChanged();
            }
        }

        public string GuestPaymentFilter
        {
            get => _guestPaymentFilter;
            set
            {
                var next = (value ?? "").Trim().ToLowerInvariant();
                if (_guestPaymentFilter == next)
                {
                    return;
                }

                _guestPaymentFilter = next;
                GuestOrderPage = 1;
                ApplyGuestFilter();
                ApplyAllPaging();
                NotifyStateChanged();
            }
        }

        public string ProductSearch
        {
            get => _productSearch;
            set
            {
                var next = value ?? "";
                if (_productSearch == next)
                {
                    return;
                }

                _productSearch = next;
                NotifyStateChanged();
            }
        }

        public string StaffFirstName { get; set; } = "Thiri";
        public string StaffLastName { get; set; } = "San";
        public string StaffEmail { get; set; } = "thiri@boutique.com";
        public string StaffPhone { get; set; } = "09222333444";
        public string StaffRole { get; private set; } = "Senior Staff";
        public string StaffName { get; private set; } = "Staff User";
        public string StaffInitials =>
            (StaffFirstName.Length > 0 ? StaffFirstName[0].ToString() : "")
            + (StaffLastName.Length > 0 ? StaffLastName[0].ToString() : "");

        public int TotalOrdersCount { get; private set; }
        public decimal TotalRevenue { get; private set; }
        public int TotalSkusCount { get; private set; }
        public int LowStockCount { get; private set; }
        public int PendingCount { get; private set; }
        public int ProcessingCount { get; private set; }
        public int ConfirmCount { get; private set; }
        public decimal ReportRevenue { get; private set; }
        public int ReportOrderCount { get; private set; }

        public List<Order> AllOrders { get; private set; } = new();
        public List<Order> FilteredOrders { get; private set; } = new();
        public List<Order> RecentOrders { get; private set; } = new();
        public List<GuestOrder> AllGuestOrders { get; private set; } = new();
        public List<GuestOrder> FilteredGuestOrders { get; private set; } = new();
        public List<ProductVariant> InventoryVariants { get; private set; } = new();

        public string GuestCustomerName { get; set; } = "";
        public string GuestPhoneNumber { get; set; } = "";
        public string NewOrderShippingAddress { get; set; } = "";
        public string GuestPaymentMethod { get; set; } = "cod";
        public string GuestPaymentStatus { get; set; } = "unpaid";
        public string CreateOrderError { get; private set; } = "";

        public int OrderPage { get; private set; } = 1;
        public int OrderPageSize { get; private set; } = 10;
        public int OrderTotalCount { get; private set; }
        public int OrderTotalPages => (int)Math.Ceiling((double)OrderTotalCount / OrderPageSize);
        public List<Order> PagedOrders { get; private set; } = new();

        public int GuestOrderPage { get; private set; } = 1;
        public int GuestOrderPageSize { get; private set; } = 10;
        public int GuestOrderTotalCount { get; private set; }
        public int GuestOrderTotalPages => (int)Math.Ceiling((double)GuestOrderTotalCount / GuestOrderPageSize);
        public List<GuestOrder> PagedGuestOrders { get; private set; } = new();

        public int InventoryPage { get; private set; } = 1;
        public int InventoryPageSize { get; private set; } = 10;
        public int InventoryTotalCount { get; private set; }
        public int InventoryTotalPages => (int)Math.Ceiling((double)InventoryTotalCount / InventoryPageSize);
        public List<ProductVariant> PagedInventoryVariants { get; private set; } = new();

        public int RecentOrderPage { get; private set; } = 1;
        public int RecentOrderPageSize { get; private set; } = 5;
        public int RecentOrderTotalCount { get; private set; }
        public int RecentOrderTotalPages => (int)Math.Ceiling((double)RecentOrderTotalCount / RecentOrderPageSize);
        public List<Order> PagedRecentOrders { get; private set; } = new();

        public List<OrderLineDraft> OrderLines { get; private set; } = new() { new OrderLineDraft() };
        public IReadOnlyList<ToastItem> ActiveToasts => _activeToasts;

        public decimal OrderLinesTotal => OrderLines.Sum(l => GetLineTotal(l));
        public int SelectedLineCount => OrderLines.Count(l => l.VariantId > 0);
        public int SelectedItemCount => OrderLines.Where(l => l.VariantId > 0).Sum(l => l.Quantity);
        public bool CanSubmitPhoneOrder =>
            !string.IsNullOrWhiteSpace(GuestCustomerName)
            && !string.IsNullOrWhiteSpace(GuestPhoneNumber)
            && !string.IsNullOrWhiteSpace(NewOrderShippingAddress)
            && OrderLines.Any(l => l.VariantId > 0 && l.Quantity > 0);

        public async Task InitializeFromCurrentSessionAsync()
        {
            if (IsPortalReady)
            {
                return;
            }

            if (_session.IsLoggedIn && (_session.IsStaff || _session.IsAdmin))
            {
                await InitializePortalAsync();
            }
        }

        public async Task RestoreOrRedirectAsync()
        {
            if (IsPortalReady)
            {
                return;
            }

            var restored = await _portalSessionBootstrapper.RestorePortalSessionAsync();
            if (!restored || (!_session.IsStaff && !_session.IsAdmin))
            {
                _nav.NavigateTo("/portal-login", replace: true);
                return;
            }

            await InitializePortalAsync();
            NotifyStateChanged();
        }

        private async Task InitializePortalAsync()
        {
            if (IsPortalReady)
            {
                return;
            }

            try
            {
                var user = _session.CurrentUser;
                if (user == null)
                {
                    _nav.NavigateTo("/portal-login", replace: true);
                    return;
                }

                StaffFirstName = user.FirstName;
                StaffLastName = user.LastName;
                StaffEmail = user.Email;
                StaffName = $"{user.FirstName} {user.LastName}";
                StaffRole = user.Role.RoleName == "admin" ? "Boutique Owner" : "Senior Staff";
                CurrentDateString = DateTime.Now.ToString("ddd, d MMM yyyy");

                await LoadStaffDashboardDataAsync();
            }
            catch (Exception ex)
            {
                TriggerToast(UiMessages.StaffPortal.PrepareFailed(ex.Message), true);
            }
            finally
            {
                IsPortalReady = true;
                NotifyStateChanged();
            }
        }

        public async Task LoadStaffDashboardDataAsync()
        {
            try
            {
                var staffId = _session.CurrentUser!.UserId;
                var dateStr = _selectedReportDate.ToString("yyyy-MM-dd");
                var data = await _httpServices.ExecuteAsync<ClothingPlatform.Api.Features.Staff.StaffDashboardDataDto>(
                    $"api/Staff/dashboard/{staffId}?reportDate={dateStr}");

                if (data != null)
                {
                    AllOrders = data.AllOrders ?? new List<Order>();
                    AllGuestOrders = data.AllGuestOrders ?? new List<GuestOrder>();
                    InventoryVariants = data.InventoryVariants ?? new List<ProductVariant>();
                    _reportOrders = data.ReportOrders ?? new List<Order>();
                    _reportGuestOrders = data.ReportGuestOrders ?? new List<GuestOrder>();

                    ReportOrderCount = _reportOrders.Count + _reportGuestOrders.Count;
                    ReportRevenue = _reportOrders.Sum(o => o.TotalAmount) + _reportGuestOrders.Sum(g => g.TotalAmount);

                    RecentOrders = AllOrders.Take(5).ToList();
                    TotalOrdersCount = AllOrders.Count;
                    TotalRevenue = AllOrders.Sum(o => o.TotalAmount);
                    TotalSkusCount = InventoryVariants.Count;
                    LowStockCount = InventoryVariants.Count(v => v.StockQuantity < 5);

                    foreach (var order in AllOrders)
                    {
                        order.OrderStatus = OrderWorkflow.Normalize(order.OrderStatus);
                    }

                    foreach (var guestOrder in AllGuestOrders)
                    {
                        guestOrder.OrderStatus = OrderWorkflow.Normalize(guestOrder.OrderStatus);
                    }

                    PendingCount = AllOrders.Count(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Pending);
                    ProcessingCount = AllOrders.Count(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Processing);
                    ConfirmCount = AllOrders.Count(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Confirm);
                }
            }
            catch (Exception ex)
            {
                TriggerToast(UiMessages.StaffPortal.DashboardLoadFailed(ex.Message), true);
                Console.WriteLine(ex.ToString());
            }

            ApplyFilter();
            ApplyGuestFilter();
            ApplyAllPaging();
            NotifyStateChanged();
        }

        private void ApplyAllPaging()
        {
            OrderTotalCount = FilteredOrders.Count;
            if (OrderPage > OrderTotalPages && OrderTotalPages > 0)
            {
                OrderPage = OrderTotalPages;
            }

            if (OrderPage < 1)
            {
                OrderPage = 1;
            }

            PagedOrders = FilteredOrders
                .Skip((OrderPage - 1) * OrderPageSize)
                .Take(OrderPageSize)
                .ToList();

            GuestOrderTotalCount = FilteredGuestOrders.Count;
            if (GuestOrderPage > GuestOrderTotalPages && GuestOrderTotalPages > 0)
            {
                GuestOrderPage = GuestOrderTotalPages;
            }

            if (GuestOrderPage < 1)
            {
                GuestOrderPage = 1;
            }

            PagedGuestOrders = FilteredGuestOrders
                .Skip((GuestOrderPage - 1) * GuestOrderPageSize)
                .Take(GuestOrderPageSize)
                .ToList();

            InventoryTotalCount = InventoryVariants.Count;
            if (InventoryPage > InventoryTotalPages && InventoryTotalPages > 0)
            {
                InventoryPage = InventoryTotalPages;
            }

            if (InventoryPage < 1)
            {
                InventoryPage = 1;
            }

            PagedInventoryVariants = InventoryVariants
                .Skip((InventoryPage - 1) * InventoryPageSize)
                .Take(InventoryPageSize)
                .ToList();

            RecentOrderTotalCount = AllOrders.Count;
            if (RecentOrderPage > RecentOrderTotalPages && RecentOrderTotalPages > 0)
            {
                RecentOrderPage = RecentOrderTotalPages;
            }

            if (RecentOrderPage < 1)
            {
                RecentOrderPage = 1;
            }

            PagedRecentOrders = AllOrders
                .Skip((RecentOrderPage - 1) * RecentOrderPageSize)
                .Take(RecentOrderPageSize)
                .ToList();
        }

        public Task ChangeOrderPage(int newPage)
        {
            OrderPage = newPage;
            ApplyAllPaging();
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task ChangeGuestOrderPage(int newPage)
        {
            GuestOrderPage = newPage;
            ApplyAllPaging();
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task ChangeInventoryPage(int newPage)
        {
            InventoryPage = newPage;
            ApplyAllPaging();
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public Task ChangeRecentOrderPage(int newPage)
        {
            RecentOrderPage = newPage;
            ApplyAllPaging();
            NotifyStateChanged();
            return Task.CompletedTask;
        }

        public void FilterOrders(string status)
        {
            CurrentFilter = status;
            OrderPage = 1;
            ApplyFilter();
            ApplyAllPaging();
            NotifyStateChanged();
        }

        private void ApplyFilter()
        {
            var query = AllOrders.AsEnumerable();

            if (!string.IsNullOrEmpty(CurrentFilter))
            {
                query = query.Where(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Normalize(CurrentFilter));
            }

            if (!string.IsNullOrWhiteSpace(RegularOrderSearch))
            {
                var search = RegularOrderSearch.Trim();
                query = query.Where(o => OrderMatchesSearch(o, search));
            }

            FilteredOrders = query.ToList();
        }

        private void ApplyGuestFilter()
        {
            var query = AllGuestOrders.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(GuestPaymentFilter))
            {
                query = query.Where(g => string.Equals(g.PaymentStatus ?? "unpaid", GuestPaymentFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(GuestOrderSearch))
            {
                var search = GuestOrderSearch.Trim();
                query = query.Where(g => GuestOrderMatchesSearch(g, search));
            }

            FilteredGuestOrders = query.ToList();
        }

        public async Task UpdateOrderStatus(Order order, string? newStatus)
        {
            if (string.IsNullOrEmpty(newStatus))
            {
                return;
            }

            var normalizedStatus = OrderWorkflow.Normalize(newStatus);
            if (!OrderWorkflow.CanMoveTo(order.OrderStatus, normalizedStatus))
            {
                TriggerToast(UiMessages.StaffPortal.RegularOrderForwardOnly, true);
                return;
            }

            var staffId = _session.CurrentUser!.UserId;
            try
            {
                await _httpServices.ExecuteAsync<object>(
                    $"api/Staff/order/{order.OrderId}/status?staffId={staffId}&newStatus={normalizedStatus}",
                    null,
                    EnumHttpMethod.Post);
                await LoadStaffDashboardDataAsync();
                TriggerToast(UiMessages.StaffPortal.RegularOrderUpdated(order.OrderId, normalizedStatus));
            }
            catch (Exception ex)
            {
                TriggerToast(UiMessages.StaffPortal.RegularOrderUpdateFailed(ex.Message), true);
            }
        }

        public async Task UpdateGuestOrderStatus(GuestOrder guestOrder, string? newStatus)
        {
            if (string.IsNullOrEmpty(newStatus))
            {
                return;
            }

            var normalizedStatus = OrderWorkflow.Normalize(newStatus);
            if (!OrderWorkflow.CanMoveTo(guestOrder.OrderStatus, normalizedStatus))
            {
                TriggerToast(UiMessages.StaffPortal.GuestOrderForwardOnly, true);
                return;
            }

            var staffId = _session.CurrentUser!.UserId;
            try
            {
                await _httpServices.ExecuteAsync<object>(
                    $"api/Staff/guestorder/{guestOrder.GuestOrderId}/status?staffId={staffId}&newStatus={normalizedStatus}",
                    null,
                    EnumHttpMethod.Post);
                await LoadStaffDashboardDataAsync();
                TriggerToast(UiMessages.StaffPortal.GuestOrderUpdated(guestOrder.GuestOrderId, normalizedStatus));
            }
            catch (Exception ex)
            {
                TriggerToast(UiMessages.StaffPortal.GuestOrderUpdateFailed(ex.Message), true);
            }
        }

        public async Task AdjustStock(ProductVariant variant, int adjustment)
        {
            var staffId = _session.CurrentUser!.UserId;
            try
            {
                await _httpServices.ExecuteAsync<object>(
                    $"api/Staff/stock/adjust?variantId={variant.VariantId}&adjustment={adjustment}&staffId={staffId}",
                    null,
                    EnumHttpMethod.Post);
                await LoadStaffDashboardDataAsync();
                TriggerToast(UiMessages.StaffPortal.StockAdjusted(variant.Sku));
            }
            catch (Exception ex)
            {
                TriggerToast(UiMessages.StaffPortal.StockAdjustFailed(ex.Message), true);
            }
        }

        public decimal GetVariantUnitPrice(int variantId)
        {
            var variant = InventoryVariants.FirstOrDefault(x => x.VariantId == variantId);
            if (variant == null)
            {
                return 0;
            }

            return (variant.Product?.BasePrice ?? 0) + (variant.PriceModifier ?? 0);
        }

        public decimal GetLineTotal(OrderLineDraft line) => GetVariantUnitPrice(line.VariantId) * line.Quantity;

        public IEnumerable<ProductVariant> GetSelectableVariants(OrderLineDraft line)
        {
            var query = InventoryVariants.Where(x => x.StockQuantity > 0 || x.VariantId == line.VariantId);

            if (!string.IsNullOrWhiteSpace(ProductSearch))
            {
                var search = ProductSearch.Trim();
                query = query.Where(v =>
                    v.VariantId == line.VariantId
                    || ContainsText(v.Sku, search)
                    || ContainsText(v.Product?.Name, search)
                    || ContainsText(v.Size, search)
                    || ContainsText(v.Color, search));
            }

            return query
                .OrderBy(v => v.Product?.Name)
                .ThenBy(v => v.Size)
                .ThenBy(v => v.Color)
                .ToList();
        }

        public void OnLineVariantChanged(OrderLineDraft line, string? value)
        {
            if (int.TryParse(value, out var id))
            {
                line.VariantId = id;
                NotifyStateChanged();
            }
        }

        public void OnLineQtyChanged(OrderLineDraft line, string? value)
        {
            if (int.TryParse(value, out var quantity) && quantity > 0)
            {
                line.Quantity = quantity;
                NotifyStateChanged();
            }
        }

        public void AdjustLineQuantity(OrderLineDraft line, int adjustment)
        {
            line.Quantity = Math.Max(1, line.Quantity + adjustment);
            NotifyStateChanged();
        }

        public void AddOrderLine()
        {
            OrderLines.Add(new OrderLineDraft());
            NotifyStateChanged();
        }

        public void RemoveOrderLine(OrderLineDraft line)
        {
            if (OrderLines.Count > 1)
            {
                OrderLines.Remove(line);
            }
            else
            {
                line.VariantId = 0;
                line.Quantity = 1;
            }

            NotifyStateChanged();
        }

        public void ResetCreateOrderForm()
        {
            GuestCustomerName = "";
            GuestPhoneNumber = "";
            NewOrderShippingAddress = "";
            GuestPaymentMethod = "cod";
            GuestPaymentStatus = "unpaid";
            CreateOrderError = "";
            ProductSearch = "";
            OrderLines = new() { new OrderLineDraft() };
            NotifyStateChanged();
        }

        public async Task SubmitPhoneOrder()
        {
            CreateOrderError = "";

            if (string.IsNullOrWhiteSpace(GuestCustomerName) || string.IsNullOrWhiteSpace(GuestPhoneNumber))
            {
                CreateOrderError = UiMessages.StaffPortal.PhoneOrderCustomerRequired;
                NotifyStateChanged();
                return;
            }

            if (string.IsNullOrWhiteSpace(NewOrderShippingAddress))
            {
                CreateOrderError = UiMessages.StaffPortal.PhoneOrderAddressRequired;
                NotifyStateChanged();
                return;
            }

            var validLines = OrderLines.Where(l => l.VariantId > 0 && l.Quantity > 0).ToList();
            if (!validLines.Any())
            {
                CreateOrderError = UiMessages.StaffPortal.PhoneOrderItemRequired;
                NotifyStateChanged();
                return;
            }

            var requestDto = new ClothingPlatform.Api.Features.Staff.GuestOrderRequestDto
            {
                CustomerName = GuestCustomerName,
                PhoneNumber = GuestPhoneNumber,
                ShippingAddress = NewOrderShippingAddress,
                PaymentMethod = GuestPaymentMethod,
                PaymentStatus = GuestPaymentStatus,
                OrderLines = validLines
                    .Select(l => new ClothingPlatform.Api.Features.Staff.OrderLineDraftDto
                    {
                        VariantId = l.VariantId,
                        Quantity = l.Quantity
                    })
                    .ToList()
            };

            var staffId = _session.CurrentUser!.UserId;
            try
            {
                await _httpServices.ExecuteAsync<object>(
                    $"api/Staff/phoneorder?staffId={staffId}",
                    requestDto,
                    EnumHttpMethod.Post);

                TriggerToast(UiMessages.StaffPortal.PhoneOrderCreated(GuestCustomerName));
                ResetCreateOrderForm();

                OrdersTab = "guest";
                GuestOrderPage = 1;

                await LoadStaffDashboardDataAsync();
                _nav.NavigateTo("/staff/order-management");
            }
            catch (Exception ex)
            {
                CreateOrderError = UiMessages.StaffPortal.PhoneOrderSaveFailed;
                TriggerToast(UiMessages.StaffPortal.PhoneOrderSubmitFailed(ex.Message), true);
                NotifyStateChanged();
            }
        }

        public async Task SaveProfile()
        {
            var staffId = _session.CurrentUser!.UserId;
            try
            {
                await _httpServices.ExecuteAsync<object>(
                    $"api/Staff/profile/{staffId}?firstName={StaffFirstName}&lastName={StaffLastName}&email={StaffEmail}",
                    null,
                    EnumHttpMethod.Post);

                var user = _session.CurrentUser!;
                user.FirstName = StaffFirstName;
                user.LastName = StaffLastName;
                user.Email = StaffEmail;
                _session.Login(user);
                ProfileSaved = true;
                TriggerToast(UiMessages.StaffPortal.ProfileSavedToast);
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                TriggerToast(UiMessages.StaffPortal.ProfileSaveFailed(ex.Message), true);
            }
        }

        public void RequestLogout()
        {
            ShowLogoutConfirm = true;
            NotifyStateChanged();
        }

        public void CancelLogout()
        {
            ShowLogoutConfirm = false;
            NotifyStateChanged();
        }

        public async Task ConfirmLogout()
        {
            ShowLogoutConfirm = false;
            await Logout();
            NotifyStateChanged();
        }

        public async Task Logout()
        {
            _session.Logout();
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            _nav.NavigateTo("/portal-login", replace: true);
        }

        public void TriggerToast(string message, bool isError = false)
        {
            var toast = new ToastItem { Message = message, IsError = isError };
            _activeToasts.Add(toast);
            NotifyStateChanged();
            _ = RemoveToastAfterDelayAsync(toast);
        }

        private async Task RemoveToastAfterDelayAsync(ToastItem toast)
        {
            await Task.Delay(3500);
            _activeToasts.Remove(toast);
            NotifyStateChanged();
        }

        private static bool OrderMatchesSearch(Order order, string search)
        {
            var paymentMethod = order.Payments.FirstOrDefault()?.PaymentMethod;
            return ContainsText($"ORD-{order.OrderId:D4}", search)
                || ContainsText(order.OrderId.ToString(), search)
                || ContainsText(order.User?.FirstName, search)
                || ContainsText(order.User?.LastName, search)
                || ContainsText($"{order.User?.FirstName} {order.User?.LastName}", search)
                || ContainsText(order.ShippingAddress, search)
                || ContainsText(paymentMethod, search)
                || ContainsText(order.OrderStatus, search)
                || ContainsText(order.PaymentStatus, search);
        }

        private static bool GuestOrderMatchesSearch(GuestOrder order, string search)
        {
            return ContainsText($"GORD-{order.GuestOrderId:D4}", search)
                || ContainsText(order.GuestOrderId.ToString(), search)
                || ContainsText(order.CustomerName, search)
                || ContainsText(order.PhoneNumber, search)
                || ContainsText(order.ShippingAddress, search)
                || ContainsText(order.PaymentMethod, search)
                || ContainsText(order.PaymentStatus, search)
                || ContainsText(order.OrderStatus, search);
        }

        private static bool ContainsText(string? value, string search)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        public MarkupString HtmlRaw(string value) => new(value);

        public static string NormalizeStatus(string? status) => OrderWorkflow.Normalize(status);

        public static bool IsFinalStatus(string? status) => OrderWorkflow.IsFinal(status);

        public MarkupString StatusBadge(string status) => OrderWorkflow.Normalize(status) switch
        {
            OrderWorkflow.Confirm => new MarkupString("<span class=\"status-badge badge-confirm\"><i class=\"bi bi-check-circle\"></i> Confirm</span>"),
            OrderWorkflow.Processing => new MarkupString("<span class=\"status-badge badge-processing\"><i class=\"bi bi-arrow-repeat\"></i> Processing</span>"),
            _ => new MarkupString("<span class=\"status-badge badge-pending\"><i class=\"bi bi-hourglass-split\"></i> Pending</span>")
        };

        private void NotifyStateChanged() => StateChanged?.Invoke();

        public sealed class OrderLineDraft
        {
            public int VariantId { get; set; }
            public int Quantity { get; set; } = 1;
        }

        public sealed class ToastItem
        {
            public string Message { get; set; } = "";
            public bool IsError { get; set; }
        }
    }
}
