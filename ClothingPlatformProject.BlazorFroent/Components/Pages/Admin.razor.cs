using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.BlazorFroent.Services;
using ClothingPlatformProject.Models.Order;
using ClothingPlatformProject.Models.User;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace ClothingPlatformProject.BlazorFroent.Components.Pages
{
    public partial class Admin
    {
        [Inject]
        public AppDbContext _db { get; set; }

        [Inject]
        public NavigationManager Nav { get; set; }

        [Inject]
        public SessionState Session { get; set; }

       

        // State variables
        private string activeView = "dashboard";
        private string errorMessage = "";
        private string successMessage = "";

        // Lists
        private List<Order> allOrders = new();
        public List<OrderDashboardDto> orders = new();
        private List<OrderDashboardDto> filteredOrders = new();
        private List<Product> allProducts = new();
        private List<Category> allCategories = new();
        private List<User> customers = new();
        private List<StaffActivityLog> activityLogs = new();
        private List<VariantDto> variants = new();
        private ProductModel model = new ProductModel();
        private bool showDeleteAlert = false;
        // Dashboard Stats
        private decimal TotalRevenue;
        private int TotalOrders;
        private int PendingOrders;
        private int TotalCustomers;
        private string imageUrl = string.Empty;
        // Order Filter
        private string orderFilter = "All";
        private int quantity = 0;
        // Product Form (Create/Edit)
        private Product? editingProduct;
        private string newProductName = "";
        private string newProductDesc = "";
        private decimal newProductBasePrice;
        private int newProductCategoryId;
        private string newProductImgUrl = "";
        private string selectedSizesString = "";
        private string selectedColorsString = "";
        private int selectedquantity;

        // Reports stats
        private decimal storeDailyRevenue;
        private int storeDailySalesCount;
        private List<DailySummaryModel> dailyReportList = new();

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

        // စာမျက်နှာ နံပါတ်နှိပ်လိုက်ရင် ပြောင်းပေးမည့် Methods
        protected override async Task OnInitializedAsync()
        {
            // First run seeder to populate sample data if DB is empty
            DbSeeder.Seed(_db);
            Console.WriteLine("Component");
            if(model.ImageDto == null)
            {
                model.ImageDto = new ProductImageModel();
            }
            await LoadData();
        }
    
        private async Task LoadData()
        {
            try
            {
                // Load categories
                allCategories = await _db.Categories.AsNoTracking().ToListAsync();

                // Load products with images and variants
                allProducts = await _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
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
                productTotalCount = await _db.Products.CountAsync();

               

                // အကယ်၍ Page နံပါတ်က ရှိသမျှ စာမျက်နှာထက် ကျော်နေရင် နောက်ဆုံးစာမျက်နှာကို ပြန်ညွှန်းပါ
                if (productPage > ProductTotalPages && ProductTotalPages > 0)
                {
                    productPage = ProductTotalPages;
                }

                // 3. လက်ရှိ Page အတွက်ပဲ ဒေတာဆွဲထုတ်ပါ
                PagedProducts = await _db.Products
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
                PageStaff = staff.Items;
                PageCustomer = result.Items;

                staffTotalCount = staff.TotalCount;
                staffTotalPage = (int)Math.Ceiling((double)staffTotalCount / staffPageSize);

                customerTotalCount = result.TotalCount;
                 
                customerTotalPage =(int)Math.Ceiling((double)customerTotalCount / customerPageSize);

        IQueryable<Order> orderQuery = _db.Orders;

                if (orderFilter != "All")
                {
                    orderQuery = orderQuery.Where(o => o.OrderStatus.ToLower() == orderFilter.ToLower());
                }

                // စုစုပေါင်း အရေအတွက်ကို ယူမယ်
                orderTotalCount = await orderQuery.CountAsync();

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
                Pagedorders = await orderQuery
                    .Include(o => o.User)
                    .Include(o => o.Payments)
                    .OrderByDescending(o => o.OrderId)
                    .Skip((orderPage - 1) * orderPageSize)
                    .Take(orderPageSize)
                    .AsNoTracking()
                    .ToListAsync();

                await InvokeAsync(StateHasChanged);

                // Load customers
                customers = await _db.Users
                    .Where(u => u.Role == "customer")
                    .OrderByDescending(u => u.UserId)
                    .ToListAsync();

                // Load staff logs
                activityLogs = await _db.StaffActivityLogs
                    .Include(l => l.Staff)
                    .OrderByDescending(l => l.LogId)
                    .Take(50)
                    .ToListAsync();

                // Compute dashboard KPI stats
                TotalRevenue = orders.Where(o => o.OrderStatus.ToLower() == "delivered").Sum(o => o.TotalAmount);
                TotalOrders = orders.Count;
                PendingOrders = orders.Count(o => o.OrderStatus.ToLower() == "pending");
                TotalCustomers = customers.Count;

                // Load report lists
                dailyReportList = allOrders
                    .GroupBy(o => o.CreatedAt.HasValue ? o.CreatedAt.Value.Date : DateTime.Today)
                    .Select(g => new DailySummaryModel
                    {
                        Date = g.Key,
                        OrdersCount = g.Count(),
                        Revenue = g.Where(o => o.OrderStatus.ToLower() == "completed" || o.OrderStatus.ToLower() == "processing" || o.OrderStatus.ToLower() == "delivered").Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(r => r.Date)
                    .ToList();
            }
            catch (Exception ex)
            {
                errorMessage = "Error loading data: " + ex.Message;
                Console.WriteLine(ex);
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
                    .Where(o => o.OrderStatus?.Trim()
                        .Equals(orderFilter.Trim(), StringComparison.OrdinalIgnoreCase) == true)
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
                var dbOrder = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == order.OrderId);
                if (dbOrder != null)
                {
                    dbOrder.OrderStatus = newStatus;
                    
                    await _db.SaveChangesAsync();
                    successMessage = $"Order #{order.OrderId} status updated to {newStatus}.";
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Error updating order status: " + ex.Message;
            }
        }

        // Product methods
        private void ResetProductForm()
        {
            editingProduct = null;
            model = new();
            newProductCategoryId = allCategories.FirstOrDefault()?.CategoryId ?? 0;
            newProductImgUrl = "";
            selectedSizesString = "S, M, L";
            selectedColorsString = "Cream Beige, Midnight Black, Blush Pink";
            
        }

        private async Task HandleCreateProduct()
        {
            errorMessage = "";
            successMessage = "";

            // 💡 ခံစစ်ကုဒ် - Category မရွေးထားရင် ဆက်သွားခွင့်မပြုခြင်း
            if (newProductCategoryId == 0)
            {
                errorMessage = "Please select a valid category.";
                return;
            }

            if (string.IsNullOrWhiteSpace(model.Name) || model.BasePrice <= 0)
            {
                errorMessage = "Product name and positive base price are required.";
                return;
            }

            try
            {
                // ၁။ ဒေတာအသစ် ဆောက်မည့် မော်ဒယ်ကို အောက်ခြေက List တွေပါ တစ်ခါတည်း Instance ဆောက်ခဲ့ခြင်း
                var product = new ProductModel
                {
                    Name = model.Name,
                    Description = model.Description,
                    BasePrice = model.BasePrice,
                    CategoryId = newProductCategoryId,
                    IsFeatured = true,
                    CreatedAt = DateTime.Now,
                    VariantsDto = new List<VariantDto>() // Null Error မတက်အောင် တစ်ခါတည်း ဆောက်ပေးခြင်း
                };

                // ၂။ Image သတ်မှတ်ခြင်း (မပါလာလျှင် default link ပေးခြင်း)
                product.ImageDto = new ProductImageModel
                {
                    ImageUrl = !string.IsNullOrWhiteSpace(imageUrl)
                        ? imageUrl.Trim()
                        : "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=600&q=80"
                };

                // ၃။ Comma-separated စာသားများကို ခွဲထုတ်ပြီး ဒေတာထဲ ထည့်သွင်းခြင်း
                var sizes = selectedSizesString.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                var colors = selectedColorsString.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c)).ToList();

                

                // ၄။ Size နဲ့ Color ကို Loop ပတ်ပြီး API ကို မပို့ခင် Variants list ထဲ တစ်ခါတည်း စုထည့်ခြင်း
                foreach (var size in sizes)
                {
                    foreach (var color in colors)
                    {
                        product.VariantsDto.Add(new VariantDto
                        {
                            Size = size,
                            Color = color,
                            StockQuantity = selectedquantity,
                            Sku = "" // Backend ရောက်မှ Auto-Gen လုပ်မှာမို့လို့ ဤနေရာတွင် အလွတ်ပေးခဲ့နိုင်သည် (Required ပြဿနာရှင်းပြီး)
                        });
                    }
                }

                // ၅။ ပြီးပြည့်စုံသွားတဲ့ product ပါဆယ်ထုပ်ကြီးကို Backend API ဆီသို့ တိုက်ရိုက် ပို့လိုက်ခြင်း
                var result = await HttpClientServices.ExecuteAsync<ProductModel>(
                    "api/product",
                    product,
                    EnumHttpMethod.Post
                );

                if (result != null)
                {
                    successMessage = $"Product '{product.Name}' created successfully with its variants!";

                    // UI Form ကို မူလအတိုင်း ပြန်ရှင်းခြင်း
                    newProductCategoryId = 0;
                    imageUrl = "";
                    selectedSizesString = "";
                    selectedColorsString = "";
                    quantity = 0;

                    ResetProductForm();
                    await LoadData(); // ဇယားအသစ်ကို ပြန်ဆွဲတင်ခြင်း
                    StateHasChanged();
                }
                else
                {
                    errorMessage = "Failed to save product. Please check API server logs.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Error creating product: " + ex.Message;
            }
        }

        private async Task HandleFileSelected(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file == null)
                {
                    errorMessage = "No file selected";
                    return;
                }

                errorMessage = "";
                successMessage = $"File selected: {file.Name}"; // UI မှာ တွေ့ရမယ်
                StateHasChanged();

                using var content = new MultipartFormDataContent();
                using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(streamContent, "file", file.Name);

                var response = await Http.PostAsync("api/product/upload-image", content);
                var raw = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // raw ဘာပြတာလဲ UI မှာ ပြမယ်
                    successMessage = $"Upload OK: {raw}";
                    var result = System.Text.Json.JsonSerializer.Deserialize<UploadResult>(
                        raw, new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    imageUrl = result?.FileName ?? "";
                    StateHasChanged();
                }
                else
                {
                    errorMessage = $"Upload failed {response.StatusCode}: {raw}";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Exception: {ex.Message}";
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

            // 💡 ခံစစ်ကုဒ် - Category မရွေးထားရင် ဆက်သွားခွင့်မပြုခြင်း
            if (newProductCategoryId == 0)
            {
                errorMessage = "Please select a valid category.";
                return;
            }

            if (string.IsNullOrWhiteSpace(model.Name) || model.BasePrice <= 0)
            {
                errorMessage = "Product name and positive base price are required.";
                return;
            }

            try
            {
                // ၁။ ဒေတာအသစ် ဆောက်မည့် မော်ဒယ်ကို အောက်ခြေက List တွေပါ တစ်ခါတည်း Instance ဆောက်ခဲ့ခြင်း
                var product = new ProductModel
                {
                    Name = model.Name,
                    Description = model.Description,
                    BasePrice = model.BasePrice,
                    CategoryId = newProductCategoryId,
                    IsFeatured = true,
                    CreatedAt = DateTime.Now,
                    VariantsDto = new List<VariantDto>() // Null Error မတက်အောင် တစ်ခါတည်း ဆောက်ပေးခြင်း
                };

                product.ImageDto = new ProductImageModel
                {
                    ImageUrl = !string.IsNullOrWhiteSpace(imageUrl)
                        ? imageUrl.Trim()
                        : "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=600&q=80"
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
                    "api/product",
                    product,
                    EnumHttpMethod.Post
                );

                if (result != null)
                {
                    successMessage = $"Product '{product.Name}' created successfully with its variants!";

                    // UI Form ကို မူလအတိုင်း ပြန်ရှင်းခြင်း
                    newProductCategoryId = 0;
                    imageUrl = "";
                    selectedSizesString = "";
                    selectedColorsString = "";
                    selectedquantity = 0;

                    ResetProductForm();
                    await LoadData(); // ဇယားအသစ်ကို ပြန်ဆွဲတင်ခြင်း
                    StateHasChanged();
                }
                else
                {
                    errorMessage = "Failed to save product. Please check API server logs.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Error creating product: " + ex.Message;
            }

        }
        private async Task DeleteProduct(int productId)
        {
            string message = "Your product will be permanently deleted. Are you sure you want to delete it?";

            var isConfirm = await JSRuntime.InvokeAsync<bool>("confirm", message);

            if (isConfirm)
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
                        errorMessage = "Delete failed.";
                    }
                }
                catch (Exception ex)
                {
                    errorMessage = $"Error deleting product: {ex.Message}";
                }
            }
            else
            {
                return;
            }
        }
        private async Task TriggerDailyReport()
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var todaySummary = await _db.StoreSalesDailies
                                            .FirstOrDefaultAsync(s => s.ReportDate == today);

                // ✅ Bug 1 fixed: || operators ထည့်ပြီး
                var todaysOrders = allOrders.Where(o =>
                    o.CreatedAt.HasValue &&
                    DateOnly.FromDateTime(o.CreatedAt.Value) == today &&
                    (o.OrderStatus.ToLower() == "completed" ||
                     o.OrderStatus.ToLower() == "processing" ||
                     o.OrderStatus.ToLower() == "delivered")).ToList();

                decimal todayRev = todaysOrders.Sum(o => o.TotalAmount);
                int todaySold = todaysOrders.Count;

                if (todaySummary == null)
                {
                    _db.StoreSalesDailies.Add(new StoreSalesDaily
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

                await _db.SaveChangesAsync();
                successMessage = "Daily sales figures aggregated successfully.";
                await LoadData();   // ← LoadData ထဲမှာ mapping မှန်ဖို့လည်း သေချာစစ်ပါ
            }
            catch (Exception ex)
            {
                errorMessage = "Error calculating summaries: " + ex.Message;
            }
        }
        private void Logout()
        {
            Session.Logout();
            Nav.NavigateTo("/login");
        }

        // Inner model for reports aggregation table
        public class DailySummaryModel
        {
            public DateTime Date { get; set; }
            public int OrdersCount { get; set; }
            public decimal Revenue { get; set; }
        }
    }
}
