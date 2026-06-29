using ClothingPlatform.Api.Features.Permission;
using ClothingPlatform.Api.Models.Order;
using ClothingPlatform.Api.Models.Report;
using ClothingPlatform.Api.Models.User;
using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Web.Components.Partial;
using ClothingPlatform.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace ClothingPlatform.Web.Components.Pages
{
    public partial class Admin
    {
        [Inject]
        public IDbContextFactory<AppDbContext> DbFactory { get; set; }

        [Inject]
        public NavigationManager Nav { get; set; }

        [Inject]
        public SessionState Session { get; set; }

        [Inject]
        public IPortalSessionBootstrapper PortalSessionBootstrapper { get; set; }

       

        // State variables
        private bool isPortalReady = false;
        private readonly SemaphoreSlim portalInitializationLock = new(1, 1);
        private readonly SemaphoreSlim dataLoadLock = new(1, 1);
        private string activeView = "dashboard";
        private string errorMessage = "";
        private string successMessage = "";
        private readonly HashSet<string> adminLoadingActions = new();
        private string profileFirstName = "";
        private string profileLastName = "";
        private string profileEmail = "";
        private string profilePhone = "";
        private string profileRoleLabel = "";
        private bool isSavingProfile = false;
        private bool profileSaved = false;

        private bool IsPortalOperator => Session.IsAdmin || Session.IsStaff;
        private bool CanViewCustomers => Session.HasPermission("Customers.View");
        private bool CanManageStaff => Session.HasPermission("Staff.Manage");
        private bool CanManageProducts => Session.HasPermission("Products.Manage");
        private bool CanViewReports => Session.HasPermission("Reports.Generate");
        private bool CanAccessAdminInsights => Session.IsAdmin || CanViewReports;
        private string PortalHeadline => Session.IsAdmin ? "Atelier Admin Panel" : "Atelier Operations Portal";
        private string PortalShellLabel => Session.IsAdmin ? "Admin Control" : "Shared Operations";
        private string ProductsNavLabel => CanManageProducts ? "Products" : "Inventory";
        private bool IsProductsView => activeView == "products" || activeView == "products-new";

        // Lists
        private List<Order> allOrders = new();
        public List<OrderDashboardDto> orders = new();
        private List<OrderDashboardDto> filteredOrders = new();
        private List<Product> allProducts = new();
        private List<Category> allCategories = new();
        private List<User> customers = new();
        private List<StaffActivityLog> activityLogs = new();
        private List<VariantDto> variants = new();
        public ProductModel model = new ProductModel();
        private bool showDeleteAlert = false;
        // Dashboard Stats
        private decimal TotalRevenue;
        private int TotalOrders;
        private int PendingOrders;
        private int TotalCustomers;
        public string imageUrl = string.Empty;
        // Order Filter
        private string orderFilter = "All";
        private int quantity = 0;
        // Product Form (Create/Edit)
        private Product? editingProduct;
        private string newProductName = "";
        private string newProductDesc = "";
        private decimal newProductBasePrice;
        private int newProductCategoryId;
        public string newProductImgUrl = "";
        private static readonly string[] ProductSizeOptions = ["XS", "S", "M", "L", "XL"];
        private List<ProductVariantDraft> variantDrafts = new();

        // Reports stats
        private decimal storeDailyRevenue;
        private int storeDailySalesCount;
        private List<DailySummaryModel> dailyReportList = new();
        private DateTime reportFrom = DateTime.Today.AddDays(-30);
        private DateTime reportTo = DateTime.Today;
        private AdminReportSummaryDto? adminReport;


        // ─── Staff Creation ───────────────────────────────────────────────────────
        private StaffFormModel staffForm = new();
        private bool isCreatingStaff = false;
        private List<UserModel> recentStaff = new();

        // ─── Permission Management ────────────────────────────────────────────────
        private List<RoleDto> permRoles = new();
        private List<PermissionDto> permList = new();
        /// <summary>Local mirror of grant states: (permId, roleId) → granted.</summary>
        private Dictionary<(int permId, int roleId), bool> permGrants = new();
        private bool isLoadingPermissions = false;
        private int? savingPermRoleId = null;
        private string permSuccessMessage = "";
        private string permErrorMessage = "";
        private int activePermRoleId = 0;   // which role tab is active in the permissions view

        //Pagination parameters
        private int staffPage = 1;
        private int staffPageSize = 10;
        private int staffTotalCount;
        private int staffTotalPage;
        private int productPage = 1;
        private int productPageSize = 10;
        private int productTotalCount; // 👈 ဒေတာစုစုပေါင်း အရေအတွက်ကို သိမ်းရန် တိုးလိုက်သည်။

        // 👈 အောက်က တွက်ချက်မှုမှာ allProducts.Count အစား productTotalCount ကို ပြောင်းသုံးပါ။
        private int ProductTotalPages => (int)Math.Ceiling((double)productTotalCount / productPageSize);

        // 👈 standard property ဖြစ်တဲ့ PagedProducts ကို List သာမန် variable အဖြစ် ပြောင်းလိုက်ပါ။
        private List<Product> PagedProducts { get; set; } = new();
        

        //order pagination

        // ── Orders Pagination Variables ──
        private int orderPage = 1;         // လက်ရှိရောက်နေတဲ့ စာမျက်နှာ
        private int orderPageSize = 10;
        private int orderTotalCount;
        // တစ်မျက်နှာမှာ ပြမည့် Order အရေအတွက်

        private int customerPage = 1;
        private int customerPageSize = 10;
        private int customerTotalCount;
        private int customerTotalPage ;

        // စုစုပေါင်း ရှိမည့် Page အရေအတွက်ကို တွက်ချက်ခြင်း
        private int OrderTotalPages => (int)Math.Ceiling((double)orderTotalCount / orderPageSize);
        
        // လက်ရှိ စာမျက်နှာအတွက်ပဲ ရှာဖွေပြီး ဖြတ်ထုတ်ပေးမည့် Orders စာရင်း
        private List<Order> Pagedorders { get; set; } = new();

        // ── Guest/Phone Orders State ──
        private string ordersTab = "regular";
        private string regularOrderSearch = "";
        private string guestOrderSearch = "";
        private string guestPaymentFilter = "";
        private List<GuestOrder> allGuestOrders = new();
        private List<GuestOrder> filteredGuestOrders = new();
        private List<GuestOrder> PagedGuestOrders { get; set; } = new();
        private int guestOrderPage = 1;
        private int guestOrderPageSize = 10;
        private int guestOrderTotalCount = 0;
        private int GuestOrderTotalPages => (int)Math.Ceiling((double)guestOrderTotalCount / guestOrderPageSize);
        private int? updatingRegularOrderId;
        private int? updatingGuestOrderId;

        // ── Order Receipt Modal State ──
        private Order? selectedRegularOrder;
        private GuestOrder? selectedGuestOrder;
        private bool showReceiptModal = false;
        
        private List<UserModel> PageCustomer { get; set; } = new();
        private List<UserModel> PageStaff { get; set; } = new();
        private List<ProductImageModel> imageModel { get; set; } = new();

        private const string LoadDataAction = "load-data";
        private const string CreateProductAction = "create-product";
        private const string UpdateProductAction = "update-product";
        private const string CreateStaffAction = "create-staff";
        private const string LoadReportAction = "load-report";
        private const string DownloadReportAction = "download-report";
        private const string DailyReportAction = "daily-report";
        private const string OrderFilterAll = "All";
        private const string OrderFilterPending = "Pending";
        private const string OrderFilterProcessing = "Processing";
        private const string OrderFilterConfirm = "Confirm";

        private bool IsAdminActionLoading(string action) => adminLoadingActions.Contains(action);

        private static string EditProductAction(int productId) => $"edit-product-{productId}";

        private static string DeleteProductAction(int productId) => $"delete-product-{productId}";

        private static string DeleteOrderAction(int orderId) => $"delete-order-{orderId}";

        private static string DeleteStaffAction(int userId) => $"delete-staff-{userId}";

        private static string OrderStatusAction(int orderId) => $"order-status-{orderId}";

        private static string FilterOrdersAction(string status) => $"filter-orders-{status}";

        private string AdminOrderFilterButtonClass(string status, bool includeMargin = true)
        {
            var activeClass = orderFilter == status ? "btn-luxury-solid text-white" : "";
            var marginClass = includeMargin ? " me-2" : "";
            return $"btn-luxury {activeClass}{marginClass}".Trim();
        }

        private async Task RunAdminAction(string action, Func<Task> callback)
        {
            if (!adminLoadingActions.Add(action))
            {
                return;
            }

            StateHasChanged();

            try
            {
                await callback();
            }
            finally
            {
                adminLoadingActions.Remove(action);
                StateHasChanged();
            }
        }
        // စာမျက်နှာ နံပါတ်နှိပ်လိုက်ရင် ပြောင်းပေးမည့် Methods
        protected override async Task OnInitializedAsync()
        {
            if (Session.IsLoggedIn && IsPortalOperator)
            {
                ApplyRequestedView();
                await InitializePortalAsync();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || isPortalReady)
            {
                return;
            }

            var restored = await PortalSessionBootstrapper.RestorePortalSessionAsync();
            if (!restored || !IsPortalOperator)
            {
                Nav.NavigateTo("/portal-login", replace: true);
                return;
            }

            ApplyRequestedView();
            await InitializePortalAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async Task InitializePortalAsync()
        {
            await portalInitializationLock.WaitAsync();

            try
            {
                if (isPortalReady)
                {
                    return;
                }

                await using (var db = await DbFactory.CreateDbContextAsync())
                {
                    DbSeeder.Seed(db);
                }

                Console.WriteLine("Component");
                if(model.ImageDto == null)
                {
                    model.ImageDto = new ProductImageModel();
                }

                SyncPortalUserState();
                await LoadData();
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.PreparePortalFailed(ex.Message);
                Console.WriteLine(ex);
            }
            finally
            {
                isPortalReady = true;
                portalInitializationLock.Release();
            }
        }
    
        private async Task LoadData()
        {
            await dataLoadLock.WaitAsync();

            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();

                // Load categories
                allCategories = await db.Categories.AsNoTracking().ToListAsync();

                // Load products with images and variants
                allProducts = await db.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .AsNoTracking()
                    .OrderByDescending(p => p.ProductId)
                    .ToListAsync();
                 


                // Load orders with users and payments
                var response = await HttpClientServices.ExecuteAsync
                    <List< OrderDashboardDto >> ("api/order/getAllOrder", null, EnumHttpMethod.Get);
                if(response != null)
                {
                    orders = response;
                }
                // Filter orders
                ApplyOrderFilter();

                // Load products with images and variants
                productTotalCount = await db.Products.CountAsync();

               

                // အကယ်၍ Page နံပါတ်က ရှိသမျှ စာမျက်နှာထက် ကျော်နေရင် နောက်ဆုံးစာမျက်နှာကို ပြန်ညွှန်းပါ
                if (productPage > ProductTotalPages && ProductTotalPages > 0)
                {
                    productPage = ProductTotalPages;
                }

                // 3. လက်ရှိ Page အတွက်ပဲ ဒေတာဆွဲထုတ်ပါ
                PagedProducts = await db.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .OrderByDescending(p => p.ProductId)
                    .Skip((productPage - 1) * productPageSize) // 👈 အပေါ်က စစ်ပြီးသားမို့ ဘယ်တော့မှ မမှားတော့ပါ
                    .Take(productPageSize)
                    .AsNoTracking()
                    .ToListAsync();
                
                if (CanViewCustomers)
                {
                    var result = await HttpClientServices.ExecuteAsync<PagedResult<UserModel>>(
                        $"api/user/customers?page={customerPage}&pageSize={customerPageSize}",
                        null,
                        EnumHttpMethod.Get);

                    PageCustomer = result?.Items ?? new();
                    customerTotalCount = result?.TotalCount ?? 0;
                    customerTotalPage = (int)Math.Ceiling((double)customerTotalCount / customerPageSize);
                }
                else
                {
                    PageCustomer = new();
                    customerTotalCount = 0;
                    customerTotalPage = 0;
                }

                if (CanManageStaff)
                {
                    var staff = await HttpClientServices.ExecuteAsync<PagedResult<UserModel>>(
                        $"api/user/staffs?staffpage={staffPage}&staffpageSize={staffPageSize}",
                        null,
                        EnumHttpMethod.Get);

                    PageStaff = staff?.Items ?? new();
                    recentStaff = PageStaff.Take(5).ToList();
                    staffTotalCount = staff?.TotalCount ?? 0;
                    staffTotalPage = (int)Math.Ceiling((double)staffTotalCount / staffPageSize);
                }
                else
                {
                    PageStaff = new();
                    recentStaff = new();
                    staffTotalCount = 0;
                    staffTotalPage = 0;
                }

                allOrders = await db.Orders
                    .Include(o => o.User)
                    .Include(o => o.Payments)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Variant)
                            .ThenInclude(v => v.Product)
                    .OrderByDescending(o => o.OrderId)
                    .AsNoTracking()
                    .ToListAsync();

                allGuestOrders = await db.GuestOrders
                    .Include(g => g.GuestOrderItems)
                        .ThenInclude(gi => gi.Variant)
                            .ThenInclude(v => v.Product)
                    .OrderByDescending(g => g.GuestOrderId)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var order in allOrders)
                {
                    order.OrderStatus = OrderWorkflow.Normalize(order.OrderStatus);
                }

                foreach (var go in allGuestOrders)
                {
                    go.OrderStatus = OrderWorkflow.Normalize(go.OrderStatus);
                }

                // Apply regular orders filter & search
                var regularQuery = allOrders.AsEnumerable();
                if (!string.IsNullOrEmpty(orderFilter) && !orderFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    regularQuery = regularQuery.Where(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Normalize(orderFilter));
                }
                if (!string.IsNullOrWhiteSpace(regularOrderSearch))
                {
                    var search = regularOrderSearch.Trim();
                    regularQuery = regularQuery.Where(o => OrderMatchesSearch(o, search));
                }
                var filteredOrderRows = regularQuery.ToList();

                // Apply guest orders filter & search
                var guestQuery = allGuestOrders.AsEnumerable();
                if (!string.IsNullOrWhiteSpace(guestPaymentFilter))
                {
                    guestQuery = guestQuery.Where(g => string.Equals(g.PaymentStatus ?? "unpaid", guestPaymentFilter, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrWhiteSpace(guestOrderSearch))
                {
                    var search = guestOrderSearch.Trim();
                    guestQuery = guestQuery.Where(g => GuestOrderMatchesSearch(g, search));
                }
                filteredGuestOrders = guestQuery.ToList();

                // Regular Order Pagination safety check & subsetting
                orderTotalCount = filteredOrderRows.Count;
                if (orderPage > OrderTotalPages && OrderTotalPages > 0)
                {
                    orderPage = OrderTotalPages;
                }
                if (orderPage < 1)
                {
                    orderPage = 1;
                }
                Pagedorders = filteredOrderRows
                    .Skip((orderPage - 1) * orderPageSize)
                    .Take(orderPageSize)
                    .ToList();

                // Guest Order Pagination safety check & subsetting
                guestOrderTotalCount = filteredGuestOrders.Count;
                if (guestOrderPage > GuestOrderTotalPages && GuestOrderTotalPages > 0)
                {
                    guestOrderPage = GuestOrderTotalPages;
                }
                if (guestOrderPage < 1)
                {
                    guestOrderPage = 1;
                }
                PagedGuestOrders = filteredGuestOrders
                    .Skip((guestOrderPage - 1) * guestOrderPageSize)
                    .Take(guestOrderPageSize)
                    .ToList();

                // Load customers
                customers = CanViewCustomers
                    ? await db.Users
                        .Include(u => u.Role)
                        .Where(u => u.Role.RoleName == "customer")
                        .AsNoTracking()
                        .OrderByDescending(u => u.UserId)
                        .ToListAsync()
                    : new List<User>();

                // Load staff logs
                activityLogs = await db.StaffActivityLogs
                    .Include(l => l.Staff)
                    .AsNoTracking()
                    .OrderByDescending(l => l.LogId)
                    .Take(50)
                    .ToListAsync();

                // Compute dashboard KPI stats
                TotalRevenue = orders.Where(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Confirm).Sum(o => o.TotalAmount);
                TotalOrders = orders.Count;
                PendingOrders = orders.Count(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Pending);
                TotalCustomers = customers.Count;

                // Load report lists
                dailyReportList = allOrders
                    .GroupBy(o => o.CreatedAt.HasValue ? o.CreatedAt.Value.Date : DateTime.Today)
                    .Select(g => new DailySummaryModel
                    {
                        Date = g.Key,
                        OrdersCount = g.Count(),
                        Revenue = g.Where(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Confirm || OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Processing).Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(r => r.Date)
                    .ToList();
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.LoadDataFailed(ex.Message);
                Console.WriteLine(ex);
            }
            finally
            {
                dataLoadLock.Release();
            }
        }
        //pagination methods
        private async Task OnProductPageChanged(int page)
        {
            productPage = page;
            await LoadData();
        }

        private async Task ChangeOrderPage(int newPage)
        {
            orderPage = newPage;
            await LoadData(); // စာမျက်နှာပြောင်းရင် ဒေတာ ပြန်မောင်းတင်မယ်
        }

        private async Task ChangeCustomerPage(int newPage)
        {
            customerPage = newPage;
            await LoadData(); // စာမျက်နှာပြောင်းရင် ဒေတာ ပြန်မောင်းတင်မယ်
        }

        private async Task ChangeStaffPage(int newPage)
        {
            staffPage = newPage;
            await LoadData(); // စာမျက်နှာပြောင်းရင် ဒေတာ ပြန်မောင်းတင်မယ်
        }
        private void ApplyRequestedView()
        {
            var requestedView = GetRequestedViewFromUrl();
            activeView = CanAccessView(requestedView) ? requestedView : "dashboard";
        }

        private string GetRequestedViewFromUrl()
        {
            var uri = Nav.ToAbsoluteUri(Nav.Uri);
            var query = uri.Query.TrimStart('?');
            if (string.IsNullOrWhiteSpace(query))
            {
                return "dashboard";
            }

            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var segments = pair.Split('=', 2);
                if (segments.Length != 2 || !string.Equals(segments[0], "view", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return NormalizePortalView(Uri.UnescapeDataString(segments[1]));
            }

            return "dashboard";
        }

        private bool CanAccessView(string viewName) => viewName switch
        {
            "customers" => CanViewCustomers,
            "staffs" => CanManageStaff,
            "reports" => CanViewReports,
            "permissions" => Session.IsAdmin,
            "dashboard" or "products" or "products-new" or "orders" or "create-order" or "profile" => IsPortalOperator,
            _ => false
        };

        private static string NormalizePortalView(string? viewName) => (viewName ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "products" => "products",
            "products-new" => "products-new",
            "orders" => "orders",
            "create-order" => "create-order",
            "customers" => "customers",
            "staffs" => "staffs",
            "reports" => "reports",
            "profile" => "profile",
            "permissions" => "permissions",
            _ => "dashboard"
        };

        private static string BuildPortalUrl(string viewName) =>
            viewName == "dashboard"
                ? "/dashboard"
                : $"/dashboard?view={Uri.EscapeDataString(viewName)}";

        private void SetView(string viewName)
        {
            var normalizedView = NormalizePortalView(viewName);
            if (!CanAccessView(normalizedView))
            {
                normalizedView = "dashboard";
            }

            activeView = normalizedView;
            errorMessage = "";
            successMessage = "";
            editingProduct = null;
            ResetProductForm();
            profileSaved = false;

            var targetUrl = BuildPortalUrl(normalizedView);
            var currentUrl = Nav.ToBaseRelativePath(Nav.Uri).Trim('/');
            if (!string.Equals(currentUrl, targetUrl.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
            {
                Nav.NavigateTo(targetUrl, replace: true);
            }

            if (normalizedView == "reports")
            {
                _ = LoadAdminReport();
            }

            if (normalizedView == "create-order")
            {
                _ = LoadAdminInventoryVariants();
            }

            if (normalizedView == "permissions")
            {
                _ = LoadPermissionMatrix();
            }
        }

        // Order methods
        private async void ApplyOrderFilter()
        {
            if (orderFilter == "All")
            {
                filteredOrders = orders;
            }
            else
            {
                filteredOrders = orders
                    .Where(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Normalize(orderFilter))
                    .ToList();
            }

            
        }

        private async Task FilterOrders(string status)
        {
            orderFilter = status;
            ApplyOrderFilter();
            await LoadData();
            await Task.CompletedTask;
        }

        private async Task UpdateOrderStatus(Order order, string newStatus)
        {
            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                var dbOrder = await db.Orders.FirstOrDefaultAsync(o => o.OrderId == order.OrderId);
                if (dbOrder != null)
                {
                    var normalizedStatus = OrderWorkflow.Normalize(newStatus);
                    if (!OrderWorkflow.CanMoveTo(dbOrder.OrderStatus, normalizedStatus))
                    {
                        errorMessage = UiMessages.Admin.OrderStatusForwardOnly;
                        return;
                    }

                    dbOrder.OrderStatus = normalizedStatus;
                    
                    await db.SaveChangesAsync();
                    successMessage = UiMessages.Admin.OrderStatusUpdated(order.OrderId, normalizedStatus);
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.OrderStatusUpdateFailed(ex.Message);
            }
        }
     
        private async Task DeleteOrder(int orderId)
{
    var confirmed = await confirmModal.ShowAsync(
        title: "Delete Order",
        message: UiMessages.Admin.DeleteOrderConfirm(orderId),
        confirmText: "Delete");

    if (!confirmed) return;

    try
    {
        var response = await HttpClientServices.ExecuteAsync<bool>(
            $"api/order/{orderId}",
            null,
            EnumHttpMethod.Delete);

        if (response)
        {
            successMessage = UiMessages.Admin.OrderDeleted(orderId);
            await LoadData();
        }
    }
    catch (Exception ex)
    {
        errorMessage = UiMessages.Admin.OrderDeleteFailed(ex.Message);
    }
}

        // Product methods
        private void ResetProductForm()
        {
            editingProduct = null;
            model = new();
            newProductCategoryId = allCategories.FirstOrDefault()?.CategoryId ?? 0;
            imageUrl = "";
            newProductImgUrl = "";
            imageBase64 = "";
            imageFileName = "";
            previewImageUrl = "";
            variantDrafts = ProductSizeOptions
                .Select(size => new ProductVariantDraft { Size = size })
                .ToList();
        }

        private void StartNewProduct()
        {
            SetView("products-new");
        }

        private void BackToProducts()
        {
            SetView("products");
        }

        private void CloseProductForm()
        {
            BackToProducts();
        }

        private int TotalVariantQuantity => variantDrafts
            .Where(v => v.IsSelected)
            .SelectMany(v => v.Entries)
            .Sum(v => Math.Max(0, v.StockQuantity));

        private void LoadVariantDrafts(IEnumerable<VariantDto>? variants = null)
        {
            variantDrafts = ProductSizeOptions
                .Select(size =>
                {
                    var matchingVariants = variants?
                        .Where(v => string.Equals(v.Size?.Trim(), size, StringComparison.OrdinalIgnoreCase))
                        .ToList() ?? new List<VariantDto>();

                    return new ProductVariantDraft
                    {
                        Size = size,
                        IsSelected = matchingVariants.Count > 0,
                        Entries = matchingVariants.Count > 0
                            ? matchingVariants.Select(v => new ProductVariantEntryDraft
                            {
                                VariantId = v.VariantId > 0 ? v.VariantId : null,
                                Color = v.Color ?? "",
                                StockQuantity = v.StockQuantity,
                                PriceModifier = v.PriceModifier ?? 0m
                            }).ToList()
                            : new List<ProductVariantEntryDraft>()
                    };
                })
                .ToList();
        }

        private static List<VariantDto> BuildVariantDtos(IEnumerable<ProductVariantDraft> drafts, string productName)
        {
            var safeProdName = string.IsNullOrWhiteSpace(productName) ? "PROD" : productName.Replace(" ", "");

            return drafts
                .Where(v => v.IsSelected)
                .SelectMany(v => v.Entries.Select(entry =>
                {
                    var safeSize = string.IsNullOrWhiteSpace(v.Size) ? "FREE" : v.Size.Trim();
                    var safeColor = string.IsNullOrWhiteSpace(entry.Color) ? "MIX" : entry.Color.Trim();

                    return new VariantDto
                    {
                        VariantId = entry.VariantId ?? 0,
                        Size = safeSize,
                        Color = safeColor,
                        StockQuantity = Math.Max(0, entry.StockQuantity),
                        PriceModifier = entry.PriceModifier,
                        Sku = $"{safeProdName.ToUpper()}-{safeSize.Replace(" ", "").ToUpper()}-{safeColor.Replace(" ", "").ToUpper()}-{Random.Shared.Next(1000, 9999)}"
                    };
                }))
                .ToList();
        }

        private void ToggleVariantSize(ProductVariantDraft draft, ChangeEventArgs e)
        {
            var isSelected = e.Value is bool boolValue
                ? boolValue
                : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase);

            draft.IsSelected = isSelected;

            if (draft.IsSelected)
            {
                if (!draft.Entries.Any())
                {
                    draft.Entries.Add(new ProductVariantEntryDraft());
                }
            }
            else
            {
                draft.Entries.Clear();
            }
        }

        private void AddVariantEntry(ProductVariantDraft draft)
        {
            draft.IsSelected = true;
            draft.Entries.Add(new ProductVariantEntryDraft());
        }

        private void RemoveVariantEntry(ProductVariantDraft draft, ProductVariantEntryDraft entry)
        {
            draft.Entries.Remove(entry);

            if (!draft.Entries.Any())
            {
                draft.IsSelected = false;
            }
        }

        private async Task HandleCreateProduct()
        {
            errorMessage = "";
            successMessage = "";

            if (newProductCategoryId == 0)
            {
                errorMessage = UiMessages.Admin.CreateProductCategoryRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(model.Name) || model.BasePrice <= 0)
            {
                errorMessage = UiMessages.Admin.CreateProductRequiredFields;
                return;
            }

            if (!variantDrafts.Any(v => v.IsSelected && v.Entries.Any()))
            {
                errorMessage = "Please select at least one size and fill its variant details.";
                return;
            }

            if (variantDrafts
                .Where(v => v.IsSelected)
                .SelectMany(v => v.Entries)
                .Any(v => string.IsNullOrWhiteSpace(v.Color)))
            {
                errorMessage = "Each selected size needs a color.";
                return;
            }

            try
            {
                // Image upload အရင်လုပ်
                if (!string.IsNullOrEmpty(imageBase64))
                {
                    var content = new MultipartFormDataContent();
                    var fileBytes = Convert.FromBase64String(imageBase64);
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    content.Add(fileContent, "file", imageFileName);

                }

                var product = new ProductModel
                {
                    Name = model.Name,
                    Description = model.Description,
                    BasePrice = model.BasePrice,
                    CategoryId = newProductCategoryId,
                    IsFeatured = true,
                    CreatedAt = DateTime.Now,
                    VariantsDto = BuildVariantDtos(variantDrafts, model.Name),
                    ImageBase64 = imageBase64,
                    ImageFileName = imageFileName,
                    ImageDto = new ProductImageModel
                    {
                        ImageUrl =""
                    }
                };

                var result = await HttpClientServices.ExecuteAsync<ProductModel>(
                    "api/product", product, EnumHttpMethod.Post);

                if (result != null)
                {
                    successMessage = UiMessages.Admin.ProductCreated(product.Name);
                    imageUrl = "";
                    imageBase64 = "";
                    imageFileName = "";
                    previewImageUrl = "";
                    newProductCategoryId = 0;
                    CloseProductForm();
                    await LoadData();
                    StateHasChanged();
                }
                else
                {
                    errorMessage = UiMessages.Admin.ProductCreateFailed;
                }
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.ProductCreateError(ex.Message);
            }
        }

        private string imageBase64 = "";
        private string imageFileName = "";
        private string previewImageUrl = "";

        private async Task HandleFileSelected(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file != null)
            {
                imageFileName = file.Name;
                using var ms = new MemoryStream();
                await file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024).CopyToAsync(ms);
                var bytes = ms.ToArray();
                imageBase64 = Convert.ToBase64String(bytes);
                previewImageUrl = $"data:{file.ContentType};base64,{imageBase64}";
                successMessage = UiMessages.Admin.ProductImageSelected(file.Name);
                StateHasChanged();
            }
        }

        public class UploadResult
        {
            public string FileName { get; set; } = "";
        }

        public class ProductVariantDraft
        {
            public string Size { get; set; } = "";
            public bool IsSelected { get; set; }
            public List<ProductVariantEntryDraft> Entries { get; set; } = new();
        }

        public class ProductVariantEntryDraft
        {
            public int? VariantId { get; set; }
            public string Color { get; set; } = "";
            public int StockQuantity { get; set; }
            public decimal PriceModifier { get; set; }
        }

        private async Task EditProduct(Product prod)
        {
            editingProduct = prod;
            imageUrl = "";
            imageBase64 = "";
            imageFileName = "";
            previewImageUrl = "";

            var product = await HttpClientServices.ExecuteAsync<ProductDto>(
                $"api/product/{prod.ProductId}",
                null,
                EnumHttpMethod.Get);

            model = new ProductModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                BasePrice = product.BasePrice,
            };

            newProductCategoryId = prod.CategoryId;

            newProductImgUrl = prod.ProductImages
                .FirstOrDefault(i => (bool)i.IsPrimary)?.ImageUrl ?? "";
            LoadVariantDrafts(product.VariantsDto);

            activeView = "products-new";
            var targetUrl = BuildPortalUrl("products-new");
            var currentUrl = Nav.ToBaseRelativePath(Nav.Uri).Trim('/');
            if (!string.Equals(currentUrl, targetUrl.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
            {
                Nav.NavigateTo(targetUrl, replace: true);
            }
        }
        private async Task HandleUpdateProduct()
        {
            errorMessage = "";
            successMessage = "";
            if (model.Id <= 0)
            {
                // တကယ်လို့ ဒီနေရာမှာ 0 ဖြစ်နေရင် EditProduct ထဲက model = new ProductModel မှာတင်မကဘဲ 
                // လက်ရှိ သုံးနေတဲ့ မော်ဒယ်က ကွဲလွဲနေတာ သေချာပါတယ်။
                errorMessage = UiMessages.Admin.UpdateProductIdMissing;
                return;
            }

            if (newProductCategoryId == 0)
            {
                errorMessage = UiMessages.Admin.UpdateProductCategoryRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(model.Name) || model.BasePrice <= 0)
            {
                errorMessage = UiMessages.Admin.UpdateProductRequiredFields;
                return;
            }

            if (!variantDrafts.Any(v => v.IsSelected && v.Entries.Any()))
            {
                errorMessage = "Please select at least one size and fill its variant details.";
                return;
            }

            if (variantDrafts
                .Where(v => v.IsSelected)
                .SelectMany(v => v.Entries)
                .Any(v => string.IsNullOrWhiteSpace(v.Color)))
            {
                errorMessage = "Each selected size needs a color.";
                return;
            }

            try
            {
                var updateModel = new UpdateProductRequest
                {
                    Id= model.Id,
                    Name = model.Name,
                    Description = model.Description,
                    BasePrice = model.BasePrice,
                    CategoryId = newProductCategoryId,
                    IsFeatured = true,
                    CreatedAt = DateTime.Now,
                    VariantsDto = BuildVariantDtos(variantDrafts, model.Name),
                    ImageBase64 = imageBase64,
                    ImageFileName = imageFileName,
                    ImageDto = new ProductImageModel
                    {
                        ImageUrl = ""
                    }
                };

                var result = await HttpClientServices.ExecuteAsync<ProductModel>(
            "api/product",
            updateModel,
            EnumHttpMethod.Put // 🌟 PUT မက်သတ် သုံးထားပါတယ်
        );

                if (result != null)
                {
                    successMessage = UiMessages.Admin.ProductUpdated(updateModel.Name);
                    imageUrl = "";
                    imageBase64 = "";
                    imageFileName = "";
                    previewImageUrl = "";
                    newProductCategoryId = 0;
                    CloseProductForm();
                    await LoadData();
                    StateHasChanged();
                }
                else
                {
                    errorMessage = UiMessages.Admin.ProductUpdateFailed;
                }
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.ProductUpdateError(ex.Message);
            }
        }
        private ConfirmModal confirmModal = default!;


        private async Task HandleDeleteClick(int productId)
        {
            var isConfirm = await confirmModal.ShowAsync();
            if (!isConfirm)
                return;

            await RunAdminAction(DeleteProductAction(productId), () => DeleteProductCore(productId));
        }

        private async Task DeleteProductCore(int productId)
        {
            try
            {
                var response = await HttpClientServices.ExecuteAsync<bool>(
                    $"api/product/{productId}",
                    null,
                    EnumHttpMethod.Delete);

                if (response)
                {
                    await LoadData();
                    StateHasChanged();
                }
                else
                {
                    errorMessage = UiMessages.Admin.ProductDeleteFailed;
                }
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.ProductDeleteError(ex.Message);
            }
        }

       



        private async Task TriggerDailyReport()
        {
            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                var today = DateOnly.FromDateTime(DateTime.Today);
                var todaySummary = await db.StoreSalesDailies
                                            .FirstOrDefaultAsync(s => s.ReportDate == today);

                // ✅ Bug 1 fixed: || operators ထည့်ပြီး
                var todaysOrders = allOrders.Where(o =>
                    o.CreatedAt.HasValue &&
                    DateOnly.FromDateTime(o.CreatedAt.Value) == today &&
                    (OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Confirm ||
                     OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Processing)).ToList();

                decimal todayRev = todaysOrders.Sum(o => o.TotalAmount);
                int todaySold = todaysOrders.Count;

                if (todaySummary == null)
                {
                    db.StoreSalesDailies.Add(new StoreSalesDaily
                    {
                        ReportDate = today,
                        TotalRevenue = todayRev,
                        TotalProductsSold = todaySold,
                        ActiveStaffCount = 1
                    });
                }
                else
                {
                    todaySummary.TotalRevenue = todayRev;
                    todaySummary.TotalProductsSold = todaySold;
                }

                await db.SaveChangesAsync();
                successMessage = UiMessages.Admin.DailyReportAggregated;
                await LoadData();   // ← LoadData ထဲမှာ mapping မှန်ဖို့လည်း သေချာစစ်ပါ
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.DailyReportFailed(ex.Message);
            }
        }
        private bool showLogoutConfirm = false;
        private User? currentUser;
        private bool isCustomerLoggingOut = false;

        private void SyncPortalUserState()
        {
            currentUser = Session.CurrentUser;
            profileFirstName = currentUser?.FirstName ?? string.Empty;
            profileLastName = currentUser?.LastName ?? string.Empty;
            profileEmail = currentUser?.Email ?? string.Empty;
            profilePhone = currentUser?.PhoneNumber ?? string.Empty;
            profileRoleLabel = Session.IsAdmin ? "Admin" : "Staff";
        }

        private async Task SavePortalProfile()
        {
            errorMessage = string.Empty;
            successMessage = string.Empty;
            profileSaved = false;

            if (string.IsNullOrWhiteSpace(profileFirstName))
            {
                errorMessage = UiMessages.Admin.ProfileFirstNameRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(profileLastName))
            {
                errorMessage = UiMessages.Admin.ProfileLastNameRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(profileEmail) || !profileEmail.Contains('@'))
            {
                errorMessage = UiMessages.Admin.ProfileEmailRequired;
                return;
            }

            if (currentUser == null)
            {
                errorMessage = UiMessages.Admin.ProfileSaveFailed("Current portal user is missing.");
                return;
            }

            isSavingProfile = true;
            StateHasChanged();

            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                var normalizedEmail = profileEmail.Trim().ToLowerInvariant();
                var duplicateExists = await db.Users.AnyAsync(u =>
                    u.UserId != currentUser.UserId &&
                    u.Email.ToLower() == normalizedEmail);

                if (duplicateExists)
                {
                    errorMessage = UiMessages.Admin.ProfileEmailDuplicate;
                    return;
                }

                var dbUser = await db.Users
                    .Include(u => u.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.UserId == currentUser.UserId);

                if (dbUser == null)
                {
                    errorMessage = UiMessages.Admin.ProfileSaveFailed("Account record was not found.");
                    return;
                }

                dbUser.FirstName = profileFirstName.Trim();
                dbUser.LastName = profileLastName.Trim();
                dbUser.Email = normalizedEmail;
                dbUser.PhoneNumber = profilePhone?.Trim() ?? string.Empty;

                await db.SaveChangesAsync();

                Session.Login(dbUser, Session.Permissions.ToList());
                SyncPortalUserState();
                successMessage = UiMessages.Admin.ProfileSaved;
                profileSaved = true;
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.ProfileSaveFailed(ex.Message);
            }
            finally
            {
                isSavingProfile = false;
                StateHasChanged();
            }
        }

        private void RequestLogout() => showLogoutConfirm = true;
        private void CancelLogout() => showLogoutConfirm = false;
        private async Task ConfirmLogout()
        {
            if (isCustomerLoggingOut)
            {
                return;
            }

            isCustomerLoggingOut = true;
            StateHasChanged();

            showLogoutConfirm = false;
            try
            {
                await Logout();
            }
            finally
            {
                isCustomerLoggingOut = false;
                StateHasChanged();
            }
        }
        
        private async Task Logout()
        {
            Session.Logout();
            currentUser = null;
            
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            Nav.NavigateTo("/portal-login");
        }
        private async Task HandleCreateStaff()
        {
            errorMessage = "";
            successMessage = "";

            // ── Validation ───────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(staffForm.FirstName))
            {
                errorMessage = UiMessages.Admin.StaffFirstNameRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(staffForm.LastName))
            {
                errorMessage = UiMessages.Admin.StaffLastNameRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(staffForm.Email))
            {
                errorMessage = UiMessages.Admin.StaffEmailRequired;
                return;
            }

            if (string.IsNullOrWhiteSpace(staffForm.Password) || staffForm.Password.Length < 6)
            {
                errorMessage = UiMessages.Admin.StaffPasswordRequired;
                return;
            }

            // ── Duplicate email check ─────────────────────────────────────────────
            await using var db = await DbFactory.CreateDbContextAsync();
            bool emailExists = await db.Users.AnyAsync(u =>
                u.Email.ToLower() == staffForm.Email.Trim().ToLower());

            if (emailExists)
            {
                errorMessage = UiMessages.Admin.StaffEmailDuplicate;
                return;
            }

            isCreatingStaff = true;
            StateHasChanged();

            try
            {
                var staffRole = await EnsurePortalRoleAsync(db, "staff", "Staff operations access");

                var newStaff = new User
                {
                    FirstName = staffForm.FirstName.Trim(),
                    LastName = staffForm.LastName.Trim(),
                    Email = staffForm.Email.Trim().ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(staffForm.Password),
                    RoleId = staffRole.RoleId,
                    PhoneNumber = staffForm.Phone?.Trim() ?? string.Empty,
                    Address = staffForm.Address?.Trim() ?? string.Empty,
                    CreatedAt = DateTime.Now
                };

                await db.Users.AddAsync(newStaff);

                await db.SaveChangesAsync();

                successMessage = UiMessages.Admin.StaffCreated(newStaff.FirstName, newStaff.LastName);
                ResetStaffForm();
                await LoadData();
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.StaffCreateFailed(ex.Message);
                Console.WriteLine(ex);
            }
            finally
            {
                isCreatingStaff = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Deletes a staff user by their UserId after admin confirmation.
        /// </summary>
        private async Task DeleteStaff(int userId)
        {
            var confirmed = await confirmModal.ShowAsync(title: "Delete Staff", message: UiMessages.Admin.DeleteStaffConfirm, confirmText: "Delete");

            if (!confirmed) return;

           

            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                var user = await db.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.Role.RoleName == "staff");
                if (user == null)
                {
                    errorMessage = UiMessages.Admin.StaffNotFound;
                    return;
                }

                db.Users.Remove(user);
                await db.SaveChangesAsync();

                successMessage = UiMessages.Admin.StaffDeleted(user.FirstName, user.LastName);
                await LoadData();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.StaffDeleteFailed(ex.Message);
            }
        }

        private void ResetStaffForm()
        {
            staffForm = new StaffFormModel();
        }

        private async Task<ClothingPlatform.DB.AppDbModels.Role> EnsurePortalRoleAsync(AppDbContext db, string roleName, string description)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role != null) return role;

            role = new ClothingPlatform.DB.AppDbModels.Role
            {
                RoleName = roleName,
                Description = description,
                CreatedAt = DateTime.Now
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            return role;
        }

        private async Task LoadAdminReport()
        {
            try
            {
                adminReport = await HttpClientServices.ExecuteAsync<AdminReportSummaryDto>(
                    $"api/report/admin?from={reportFrom:yyyy-MM-dd}&to={reportTo:yyyy-MM-dd}",
                    null,
                    EnumHttpMethod.Get);
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.ReportLoadFailed(ex.Message);
            }
        }

        private async Task DownloadAdminReportCsv()
        {
            try
            {
                var token = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/report/admin.csv?from={reportFrom:yyyy-MM-dd}&to={reportTo:yyyy-MM-dd}");

                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await Http.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var csv = await response.Content.ReadAsStringAsync();
                var dataUrl = "data:text/csv;charset=utf-8," + Uri.EscapeDataString(csv);
                var fileName = $"admin-report-{reportFrom:yyyyMMdd}-{reportTo:yyyyMMdd}.csv";
                await JSRuntime.InvokeVoidAsync("eval",
                    $"const a=document.createElement('a');a.href='{dataUrl}';a.download='{fileName}';document.body.appendChild(a);a.click();a.remove();");
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.Admin.ReportDownloadFailed(ex.Message);
            }
        }

        private static string NormalizeStatus(string? status) => OrderWorkflow.Normalize(status);

        private static bool IsFinalStatus(string? status) =>
            OrderWorkflow.IsFinal(OrderWorkflow.Normalize(status));

        private static string? NormalizeSlipImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return null;
            }

            var trimmedUrl = imageUrl.Trim();
            if (trimmedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                trimmedUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedUrl;
            }

            var normalizedPath = trimmedUrl.Replace('\\', '/').TrimStart('/');
            if (normalizedPath.StartsWith("images/payment-slips/", StringComparison.OrdinalIgnoreCase))
            {
                return $"/{normalizedPath}";
            }

            if (trimmedUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return trimmedUrl;
            }

            return $"/images/payment-slips/{normalizedPath}";
        }

        private static string StatusBadgeClass(string? status) => OrderWorkflow.Normalize(status) switch
        {
            OrderWorkflow.Confirm => "badge-confirm text-success",
            OrderWorkflow.Processing => "badge-processing text-primary",
            _ => "badge-pending text-warning"
        };


        // Inner model for reports aggregation table
        public class DailySummaryModel
        {
            public DateTime Date { get; set; }
            public int OrdersCount { get; set; }
            public decimal Revenue { get; set; }
        }

        public class StaffFormModel
        {
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Address { get; set; } = "";
        }

        // ─── Admin Create Phone Order ─────────────────────────────────────────────

        // Order line draft used only by admin create-order view
        public class AdminOrderLineDraft
        {
            public int VariantId { get; set; } = 0;
            public int Quantity { get; set; } = 1;
        }

        // Form fields
        private string adminGuestCustomerName = "";
        private string adminGuestPhoneNumber = "";
        private string adminShippingAddress = "";
        private string adminPaymentMethod = "cod";
        private string adminPaymentStatus = "unpaid";
        private string adminProductSearch = "";
        private string adminCreateOrderError = "";
        private bool adminIsSubmittingPhoneOrder = false;

        // Product variants loaded for the dropdown (reuses allProducts data)
        private List<ProductVariant> adminInventoryVariants = new();

        // Order lines
        private List<AdminOrderLineDraft> adminOrderLines = new() { new AdminOrderLineDraft() };

        // Computed helpers
        private decimal adminOrderLinesTotal => adminOrderLines.Sum(l => AdminGetLineTotal(l));
        private int adminSelectedLineCount => adminOrderLines.Count(l => l.VariantId > 0);
        private int adminSelectedItemCount => adminOrderLines.Where(l => l.VariantId > 0).Sum(l => l.Quantity);
        private bool adminCanSubmitPhoneOrder =>
            !string.IsNullOrWhiteSpace(adminGuestCustomerName)
            && !string.IsNullOrWhiteSpace(adminGuestPhoneNumber)
            && !string.IsNullOrWhiteSpace(adminShippingAddress)
            && adminOrderLines.Any(l => l.VariantId > 0 && l.Quantity > 0);

        private decimal AdminGetVariantUnitPrice(int variantId)
        {
            var variant = adminInventoryVariants.FirstOrDefault(x => x.VariantId == variantId);
            if (variant == null) return 0;
            return (variant.Product?.BasePrice ?? 0) + (variant.PriceModifier ?? 0);
        }

        private decimal AdminGetLineTotal(AdminOrderLineDraft line) =>
            AdminGetVariantUnitPrice(line.VariantId) * line.Quantity;

        private IEnumerable<ProductVariant> AdminGetSelectableVariants(AdminOrderLineDraft line)
        {
            var query = adminInventoryVariants.Where(x => x.StockQuantity > 0 || x.VariantId == line.VariantId);

            if (!string.IsNullOrWhiteSpace(adminProductSearch))
            {
                var search = adminProductSearch.Trim();
                query = query.Where(v =>
                    v.VariantId == line.VariantId
                    || (v.Sku?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (v.Product?.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (v.Size?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (v.Color?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return query
                .OrderBy(v => v.Product?.Name)
                .ThenBy(v => v.Size)
                .ThenBy(v => v.Color)
                .ToList();
        }

        private void AdminOnLineVariantChanged(AdminOrderLineDraft line, string? value)
        {
            if (int.TryParse(value, out var id))
            {
                line.VariantId = id;
                StateHasChanged();
            }
        }

        private void AdminOnLineQtyChanged(AdminOrderLineDraft line, string? value)
        {
            if (int.TryParse(value, out var qty) && qty > 0)
            {
                line.Quantity = qty;
                StateHasChanged();
            }
        }

        private void AdminAdjustLineQty(AdminOrderLineDraft line, int adjustment)
        {
            line.Quantity = Math.Max(1, line.Quantity + adjustment);
            StateHasChanged();
        }

        private void AdminAddOrderLine()
        {
            adminOrderLines.Add(new AdminOrderLineDraft());
            StateHasChanged();
        }

        private void AdminRemoveOrderLine(AdminOrderLineDraft line)
        {
            if (adminOrderLines.Count > 1)
                adminOrderLines.Remove(line);
            else
            {
                line.VariantId = 0;
                line.Quantity = 1;
            }
            StateHasChanged();
        }

        private void AdminResetCreateOrderForm()
        {
            adminGuestCustomerName = "";
            adminGuestPhoneNumber = "";
            adminShippingAddress = "";
            adminPaymentMethod = "cod";
            adminPaymentStatus = "unpaid";
            adminProductSearch = "";
            adminCreateOrderError = "";
            adminOrderLines = new() { new AdminOrderLineDraft() };
            StateHasChanged();
        }

        private static string AdminCheckClass(bool isComplete) =>
            isComplete ? "summary-check complete" : "summary-check";

        /// <summary>
        /// Loads product variants for the admin phone order dropdown.
        /// Called when navigating to the create-order view.
        /// </summary>
        private async Task LoadAdminInventoryVariants()
        {
            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                adminInventoryVariants = await db.ProductVariants
                    .Include(v => v.Product)
                    .Where(v => v.StockQuantity > 0)
                    .OrderBy(v => v.Product.Name)
                    .ThenBy(v => v.Size)
                    .ThenBy(v => v.Color)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to load inventory: {ex.Message}";
            }
        }

        private async Task AdminSubmitPhoneOrder()
        {
            if (adminIsSubmittingPhoneOrder) return;

            adminCreateOrderError = "";

            if (string.IsNullOrWhiteSpace(adminGuestCustomerName) || string.IsNullOrWhiteSpace(adminGuestPhoneNumber))
            {
                adminCreateOrderError = "Please enter the customer name and phone number.";
                return;
            }

            if (string.IsNullOrWhiteSpace(adminShippingAddress))
            {
                adminCreateOrderError = "Please enter a shipping address.";
                return;
            }

            var validLines = adminOrderLines.Where(l => l.VariantId > 0 && l.Quantity > 0).ToList();
            if (!validLines.Any())
            {
                adminCreateOrderError = "Please add at least one product item.";
                return;
            }

            adminIsSubmittingPhoneOrder = true;
            StateHasChanged();

            try
            {
                var requestDto = new ClothingPlatform.Api.Features.Staff.GuestOrderRequestDto
                {
                    CustomerName = adminGuestCustomerName,
                    PhoneNumber = adminGuestPhoneNumber,
                    ShippingAddress = adminShippingAddress,
                    PaymentMethod = adminPaymentMethod,
                    PaymentStatus = adminPaymentStatus,
                    OrderLines = validLines
                        .Select(l => new ClothingPlatform.Api.Features.Staff.OrderLineDraftDto
                        {
                            VariantId = l.VariantId,
                            Quantity = l.Quantity
                        })
                        .ToList()
                };

                var staffId = Session.CurrentUser!.UserId;
                var result = await HttpClientServices.ExecuteAsync<object>(
                    $"api/Staff/phoneorder?staffId={staffId}",
                    requestDto,
                    EnumHttpMethod.Post);

                successMessage = $"Phone order for '{adminGuestCustomerName}' was created successfully.";
                AdminResetCreateOrderForm();
                await LoadData();
                SetView("orders");
            }
            catch (Exception ex)
            {
                adminCreateOrderError = $"Failed to submit order: {ex.Message}";
            }
            finally
            {
                adminIsSubmittingPhoneOrder = false;
                StateHasChanged();
            }
        }

        private async Task ChangeGuestOrderPage(int newPage)
        {
            guestOrderPage = newPage;
            await LoadData();
        }

        private async Task UpdateGuestOrderStatus(GuestOrder guestOrder, string? newStatus)
        {
            if (updatingGuestOrderId == guestOrder.GuestOrderId) return;
            if (string.IsNullOrEmpty(newStatus)) return;

            var normalizedStatus = OrderWorkflow.Normalize(newStatus);
            if (!OrderWorkflow.CanMoveTo(guestOrder.OrderStatus, normalizedStatus))
            {
                errorMessage = UiMessages.StaffPortal.GuestOrderForwardOnly;
                return;
            }

            var staffId = Session.CurrentUser!.UserId;
            updatingGuestOrderId = guestOrder.GuestOrderId;
            StateHasChanged();

            try
            {
                await HttpClientServices.ExecuteAsync<object>(
                    $"api/Staff/guestorder/{guestOrder.GuestOrderId}/status?staffId={staffId}&newStatus={normalizedStatus}",
                    null,
                    EnumHttpMethod.Post);
                await LoadData();
                successMessage = UiMessages.StaffPortal.GuestOrderUpdated(guestOrder.GuestOrderId, normalizedStatus);
            }
            catch (Exception ex)
            {
                errorMessage = UiMessages.StaffPortal.GuestOrderUpdateFailed(ex.Message);
            }
            finally
            {
                updatingGuestOrderId = null;
                StateHasChanged();
            }
        }

        private void OpenRegularReceipt(Order order)
        {
            selectedRegularOrder = order;
            selectedGuestOrder = null;
            showReceiptModal = true;
            StateHasChanged();
        }

        private void OpenGuestReceipt(GuestOrder guestOrder)
        {
            selectedGuestOrder = guestOrder;
            selectedRegularOrder = null;
            showReceiptModal = true;
            StateHasChanged();
        }

        private void CloseReceipt()
        {
            selectedRegularOrder = null;
            selectedGuestOrder = null;
            showReceiptModal = false;
            StateHasChanged();
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

        private bool IsUpdatingRegularOrder(int orderId) => updatingRegularOrderId == orderId;
        private bool IsUpdatingGuestOrder(int guestOrderId) => updatingGuestOrderId == guestOrderId;

        private static MarkupString PaymentBadge(string? status)
        {
            return string.Equals(status ?? "unpaid", "paid", StringComparison.OrdinalIgnoreCase)
                ? new MarkupString("<span class=\"status-badge badge-paid\"><i class=\"bi bi-check-circle\"></i> Paid</span>")
                : new MarkupString("<span class=\"status-badge badge-unpaid\"><i class=\"bi bi-clock-history\"></i> Unpaid</span>");
        }

        private static MarkupString StatusBadge(string status) => OrderWorkflow.Normalize(status) switch
        {
            OrderWorkflow.Confirm => new MarkupString("<span class=\"status-badge badge-confirm\"><i class=\"bi bi-check-circle\"></i> Confirm</span>"),
            OrderWorkflow.Processing => new MarkupString("<span class=\"status-badge badge-processing\"><i class=\"bi bi-arrow-repeat\"></i> Processing</span>"),
            _ => new MarkupString("<span class=\"status-badge badge-pending\"><i class=\"bi bi-hourglass-split\"></i> Pending</span>")
        };

        // ─── Permission Management Methods ────────────────────────────────────────

        private async Task LoadPermissionMatrix()
        {
            if (isLoadingPermissions)
            {
                return;
            }

            isLoadingPermissions = true;
            permErrorMessage = "";
            permSuccessMessage = "";
            StateHasChanged();

            try
            {
                var matrix = await HttpClientServices.ExecuteAsync<PermissionMatrixDto>(
                    "api/permission/matrix",
                    null,
                    EnumHttpMethod.Get);

                if (matrix != null)
                {
                    permRoles = matrix.Roles ?? new();
                    permList = matrix.Permissions ?? new();

                    // Rebuild local grant dictionary
                    permGrants = new Dictionary<(int, int), bool>();
                    foreach (var grant in matrix.Grants ?? new())
                    {
                        permGrants[(grant.PermissionId, grant.RoleId)] = grant.Granted;
                    }

                    // Default to the first manageable role tab
                    if (activePermRoleId == 0 && permRoles.Any())
                    {
                        activePermRoleId = permRoles[0].RoleId;
                    }
                }
            }
            catch (Exception ex)
            {
                permErrorMessage = $"Failed to load permission matrix: {ex.Message}";
            }
            finally
            {
                isLoadingPermissions = false;
                StateHasChanged();
            }
        }

        private void TogglePermission(int roleId, int permId)
        {
            var key = (permId, roleId);
            if (permGrants.TryGetValue(key, out var current))
            {
                permGrants[key] = !current;
            }
            else
            {
                permGrants[key] = true;
            }

            StateHasChanged();
        }

        private bool IsPermGranted(int roleId, int permId)
        {
            return permGrants.TryGetValue((permId, roleId), out var granted) && granted;
        }

        private async Task SaveRolePermissions(int roleId)
        {
            savingPermRoleId = roleId;
            permErrorMessage = "";
            permSuccessMessage = "";
            StateHasChanged();

            try
            {
                // Collect the IDs of permissions currently toggled ON for this role
                var grantedIds = permList
                    .Where(p => IsPermGranted(roleId, p.PermissionId))
                    .Select(p => p.PermissionId)
                    .ToList();

                var request = new UpdateRolePermissionsRequest { PermissionIds = grantedIds };

                await HttpClientServices.ExecuteAsync<object>(
                    $"api/permission/role/{roleId}",
                    request,
                    EnumHttpMethod.Put);

                var roleName = permRoles.FirstOrDefault(r => r.RoleId == roleId)?.RoleName ?? "Role";
                permSuccessMessage = $"✓ Permissions for '{roleName}' saved. Changes apply on next login.";
            }
            catch (Exception ex)
            {
                permErrorMessage = $"Failed to save permissions: {ex.Message}";
            }
            finally
            {
                savingPermRoleId = null;
                StateHasChanged();
            }
        }

        private void SetActivePermTab(int roleId)
        {
            activePermRoleId = roleId;
            permSuccessMessage = "";
            permErrorMessage = "";
            StateHasChanged();
        }
    }
}
