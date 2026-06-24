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
        private string selectedSizesString = "";
        private string selectedColorsString = "";
        private int selectedquantity;

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
            if (Session.IsLoggedIn && Session.IsAdmin)
            {
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
            if (!restored || !Session.IsAdmin)
            {
                Nav.NavigateTo("/portal-login", replace: true);
                return;
            }

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
                
                var result = await HttpClientServices.ExecuteAsync<PagedResult<UserModel>>(
                $"api/user/customers?page={customerPage}&pageSize={customerPageSize}",
                null,
                EnumHttpMethod.Get);

                var staff = await HttpClientServices.ExecuteAsync<PagedResult<UserModel>>(
                $"api/user/staffs?staffpage={staffPage}&staffpageSize={staffPageSize}",
                null,
                EnumHttpMethod.Get);
                PageStaff = staff?.Items ?? new();
                PageCustomer = result?.Items ?? new();
                recentStaff = PageStaff.Take(5).ToList();

                staffTotalCount = staff?.TotalCount ?? 0;
                staffTotalPage = (int)Math.Ceiling((double)staffTotalCount / staffPageSize);

                customerTotalCount = result?.TotalCount ?? 0;
                 
                customerTotalPage =(int)Math.Ceiling((double)customerTotalCount / customerPageSize);

                allOrders = await db.Orders
                    .Include(o => o.User)
                    .Include(o => o.Payments)
                    .OrderByDescending(o => o.OrderId)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var order in allOrders)
                {
                    order.OrderStatus = OrderWorkflow.Normalize(order.OrderStatus);
                }

                var filteredOrderRows = orderFilter == "All"
                    ? allOrders
                    : allOrders
                        .Where(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Normalize(orderFilter))
                        .ToList();

                if (orderFilter != "All")
                {
                    filteredOrderRows = filteredOrderRows
                        .Where(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Normalize(orderFilter))
                        .ToList();
                }

                // စုစုပေါင်း အရေအတွက်ကို ယူမယ်
                orderTotalCount = filteredOrderRows.Count;

                // Safety Checks
                if (orderPage > OrderTotalPages && OrderTotalPages > 0)
                {
                    orderPage = OrderTotalPages;
                }
                if (orderPage < 1)
                {
                    orderPage = 1;
                }

                // လက်ရှိ Page စာပဲ ဆွဲထုတ်ပြီး Pagedorders ထဲ ထည့်မယ်
                Pagedorders = filteredOrderRows
                    .Skip((orderPage - 1) * orderPageSize)
                    .Take(orderPageSize)
                    .ToList();

                // Load customers
                customers = await db.Users
                    .Include(u => u.Role)
                    .Where(u => u.Role.RoleName == "customer")
                    .AsNoTracking()
                    .OrderByDescending(u => u.UserId)
                    .ToListAsync();

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
        private void SetView(string viewName)
        {
            activeView = viewName;
            errorMessage = "";
            successMessage = "";
            editingProduct = null;
            ResetProductForm();

            if (viewName == "reports")
            {
                _ = LoadAdminReport();
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
            selectedSizesString = "";
            selectedColorsString = "";
            selectedquantity = 0;
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
                    VariantsDto = new List<VariantDto>(),
                    ImageBase64 = imageBase64,
                    ImageFileName = imageFileName,
                    ImageDto = new ProductImageModel
                    {
                        ImageUrl =""
                    }
                };

                

                var sizes = selectedSizesString.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var colors = selectedColorsString.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();

                foreach (var size in sizes)
                {
                    foreach (var color in colors)
                    {
                        product.VariantsDto.Add(new VariantDto
                        {
                            Size = size,
                            Color = color,
                            StockQuantity = selectedquantity,
                            Sku = ""
                        });
                    }
                }

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
                    selectedSizesString = "";
                    selectedColorsString = "";
                    selectedquantity = 0;
                    ResetProductForm();
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

        private async Task EditProduct(Product prod)
        {
            editingProduct = prod;

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

            // =========================
            // ✅ VARIANTS → INPUT FIELDS
            // =========================

            selectedColorsString = string.Join(", ",
                product.VariantsDto?
                    .Select(v => v.Color)
                    .Distinct() ?? new List<string>());

            selectedSizesString = string.Join(", ",
                product.VariantsDto?
                    .Select(v => v.Size)
                    .Distinct() ?? new List<string>());

            selectedquantity = product.VariantsDto?.FirstOrDefault()?.StockQuantity ?? 0;
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
                    VariantsDto = new List<VariantDto>(),
                    ImageBase64 = imageBase64,
                    ImageFileName = imageFileName,
                    ImageDto = new ProductImageModel
                    {
                        ImageUrl = ""
                    }
                };



                var sizes = selectedSizesString.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var colors = selectedColorsString.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();

                foreach (var size in sizes)
                {
                    foreach (var color in colors)
                    {
                        updateModel.VariantsDto.Add(new VariantDto
                        {
                            Size = size,
                            Color = color,
                            StockQuantity = selectedquantity,
                            Sku = ""
                        });
                    }
                }

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
                    selectedSizesString = "";
                    selectedColorsString = "";
                    selectedquantity = 0;
                    ResetProductForm();
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
            
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "customerId");
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

    }
}
