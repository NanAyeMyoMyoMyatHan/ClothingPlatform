using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.BlazorFroent.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

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
        private List<Order> filteredOrders = new();
        private List<Product> allProducts = new();
        private List<Category> allCategories = new();
        private List<User> customers = new();
        private List<StaffActivityLog> activityLogs = new();
        private ProductModel model = new();
        // Dashboard Stats
        private decimal TotalRevenue;
        private int TotalOrders;
        private int PendingOrders;
        private int TotalCustomers;

        // Order Filter
        private string orderFilter = "All";

        // Product Form (Create/Edit)
        private Product? editingProduct;
        private string newProductName = "";
        private string newProductDesc = "";
        private decimal newProductBasePrice;
        private int newProductCategoryId;
        private string newProductImgUrl = "";
        private string selectedSizesString = "S, M, L";
        private string selectedColorsString = "Cream Beige, Midnight Black, Blush Pink";

        // Reports stats
        private decimal storeDailyRevenue;
        private int storeDailySalesCount;
        private List<DailySummaryModel> dailyReportList = new();

        protected override async Task OnInitializedAsync()
        {
            // First run seeder to populate sample data if DB is empty
            DbSeeder.Seed(_db);

            // Access control
            if (!Session.IsLoggedIn || Session.CurrentUser?.Role != "admin")
            {
                var user = HttpClientServices.ExecuteAsync<List<User>>("admin", EnumHttpMethod.Get);
                // If not logged in as admin, try to login automatically as default admin for convenience
                var defaultAdmin = _db.Users.FirstOrDefault(u => u.Role == "admin");
                if (defaultAdmin != null)
                {
                    Session.Login(defaultAdmin);
                }
                else
                {
                    Nav.NavigateTo("/login");
                    return;
                }
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
                allOrders = await _db.Orders
                    .Include(o => o.User)
                    .Include(o => o.Payments)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Variant)
                            .ThenInclude(v => v.Product)
                    .OrderByDescending(o => o.OrderId)
                    .ToListAsync();

                // Filter orders
                ApplyOrderFilter();

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
                TotalRevenue = allOrders.Where(o => o.OrderStatus.ToLower() == "completed" || o.OrderStatus.ToLower() == "processing" || o.OrderStatus.ToLower() == "delivered").Sum(o => o.TotalAmount);
                TotalOrders = allOrders.Count;
                PendingOrders = allOrders.Count(o => o.OrderStatus.ToLower() == "pending");
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

        private void SetView(string viewName)
        {
            activeView = viewName;
            errorMessage = "";
            successMessage = "";
            editingProduct = null;
            ResetProductForm();
        }

        // Order methods
        private void ApplyOrderFilter()
        {
            if (orderFilter == "All")
            {
                filteredOrders = allOrders;
            }
            else
            {
                filteredOrders = allOrders.Where(o => o.OrderStatus.Equals(orderFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        private async Task FilterOrders(string status)
        {
            orderFilter = status;
            ApplyOrderFilter();
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
                    
                    // Add fulfillment log
                    var log = new StaffFulfillmentLog
                    {
                        OrderId = order.OrderId,
                        StaffId = Session.CurrentUser!.UserId,
                        ActionTaken = $"Status updated to {newStatus}",
                        Notes = "Status updated from Admin portal."
                    };
                    _db.StaffFulfillmentLogs.Add(log);

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
            newProductName = "";
            newProductDesc = "";
            newProductBasePrice = 0;
            newProductCategoryId = allCategories.FirstOrDefault()?.CategoryId ?? 0;
            newProductImgUrl = "";
            selectedSizesString = "S, M, L";
            selectedColorsString = "Cream Beige, Midnight Black, Blush Pink";
            editingProduct = null;
        }

        private async Task HandleCreateProduct()
        {
            errorMessage = "";
            successMessage = "";

            if (string.IsNullOrWhiteSpace(newProductName) || newProductBasePrice <= 0)
            {
                errorMessage = "Product name and positive base price are required.";
                return;
            }

            try
            {
                
                var product = new Product
                {
                    Name = model.Name.Trim(),
                    Description = model.Description.Trim(),
                    BasePrice = model.BasePrice,
                    CategoryId = newProductCategoryId,
                    IsFeatured = true,
                    CreatedAt = DateTime.Now
                };
                var result = await HttpClientServices.ExecuteAsync <ProductModel>(
                    "api/product",model,
                     EnumHttpMethod.Post);

                 

                // Add image if provided
                if (!string.IsNullOrWhiteSpace(newProductImgUrl))
                {
                    _db.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = newProductImgUrl.Trim(),
                        IsPrimary = true
                    });
                }
                else
                {
                    _db.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=600&q=80",
                        IsPrimary = true
                    });
                }

                // Add variants
                var sizes = selectedSizesString.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
                var colors = selectedColorsString.Split(',').Select(c => c.Trim()).Where(c => !string.IsNullOrEmpty(c));

                int skuCounter = DateTime.Now.Millisecond;
                foreach (var size in sizes)
                {
                    foreach (var color in colors)
                    {
                        skuCounter++;
                        var skuName = product.Name.Length >= 3 ? product.Name.Substring(0, 3).ToUpper() : "PRO";
                        _db.ProductVariants.Add(new ProductVariant
                        {
                            ProductId = product.ProductId,
                            Size = size,
                            Color = color,
                            Sku = $"{skuName}-{size.ToUpper()}-{color.Replace(" ", "").ToUpper()}-{skuCounter}",
                            StockQuantity = 20,
                            PriceModifier = 0.00m
                        });
                    }
                }

                // Log activity
                _db.StaffActivityLogs.Add(new StaffActivityLog
                {
                    StaffId = Session.CurrentUser!.UserId,
                    TargetTable = "products",
                    TargetId = product.ProductId,
                    ActionType = "create",
                    Description = $"Created new product: '{product.Name}' with variants."
                });

                await _db.SaveChangesAsync();
                successMessage = $"Product '{product.Name}' created successfully.";
                ResetProductForm();
                await LoadData();
            }
            catch (Exception ex)
            {
                errorMessage = "Error creating product: " + ex.Message;
            }
        }
        

        // Dropdown ကနေ တစ်ခုခုရွေးလိုက်ရင် ဒီကောင် အလုပ်လုပ်ပါမယ်
        private void OnCategoryChanged(int selectedId)
        {
            newProductCategoryId = selectedId; // ရွေးလိုက်တဲ့ ID (int) ကို ထည့်လိုက်ခြင်း

            // တကယ်လို့ သင့်မှာ ပြင်ဆင်နေတဲ့ Product Object (ဥပမာ- editingProduct) ရှိရင်
            if (editingProduct != null)
            {
                editingProduct.CategoryId = selectedId; // Model ထဲကိုပါ တစ်ခါတည်း ထည့်ပေးလိုက်ပါ
                editingProduct.Category = allCategories.FirstOrDefault(c => c.CategoryId == selectedId);
            }
        }
        private void EditProduct(Product prod)
        {
            editingProduct = prod;
            newProductName = prod.Name;
            newProductDesc = prod.Description ?? "";
            newProductBasePrice = prod.BasePrice;
            newProductCategoryId = prod.CategoryId;
            newProductImgUrl = prod.ProductImages.FirstOrDefault(i => (bool)i.IsPrimary)?.ImageUrl ?? "";
        }

        private async Task HandleUpdateProduct()
        {
            errorMessage = "";
            successMessage = "";

            if (editingProduct == null) return;

            try
            {

                var dbProd = await _db.Products.FirstOrDefaultAsync(p => p.ProductId == editingProduct.ProductId);
                if (dbProd != null)
                {
                    dbProd.Name = newProductName.Trim();
                    dbProd.Description = newProductDesc.Trim();
                    dbProd.BasePrice = newProductBasePrice;
                    dbProd.CategoryId = newProductCategoryId;
                    _db.Products.Update(dbProd);
                    // Update Image
                    var primaryImg = await _db.ProductImages.FirstOrDefaultAsync(i => i.ProductId == dbProd.ProductId && (bool)i.IsPrimary);
                    if (primaryImg != null)
                    {
                        primaryImg.ImageUrl = newProductImgUrl.Trim();
                    }
                    else if (!string.IsNullOrWhiteSpace(newProductImgUrl))
                    {
                        _db.ProductImages.Add(new ProductImage
                        {
                            ProductId = dbProd.ProductId,
                            ImageUrl = newProductImgUrl.Trim(),
                            IsPrimary = true
                        });
                    }

                    // Log activity
                    //_db.StaffActivityLogs.Add(new StaffActivityLog
                    //{
                    //    StaffId = Session.CurrentUser!.UserId,
                    //    TargetTable = "products",
                    //    TargetId = dbProd.ProductId,
                    //    ActionType = "update",
                    //    Description = $"Updated product info for: '{dbProd.Name}'."
                    //});
                    
                    await _db.SaveChangesAsync();
                    successMessage = $"Product '{dbProd.Name}' updated successfully.";
                    ResetProductForm();
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Error updating product: " + ex.Message;
            }
        }

        private async Task DeleteProduct(int productId)
        {
            try
            {
                var product = await _db.Products
                    .Include(p => p.ProductVariants)
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product != null)
                {
                    var variantIds = product.ProductVariants.Select(v => v.VariantId).ToList();
                    var relatedOrderItems = _db.OrderItems.Where(oi => variantIds.Contains(oi.VariantId));
                    _db.OrderItems.RemoveRange(relatedOrderItems);
                    // Remove primary records
                    _db.ProductImages.RemoveRange(product.ProductImages);
                    _db.ProductVariants.RemoveRange(product.ProductVariants);
                    _db.Products.Remove(product);

                    // Log activity
                    //_db.StaffActivityLogs.Add(new StaffActivityLog
                    //{
                    //    StaffId = Session.CurrentUser!.UserId,
                    //    TargetTable = "products",
                    //    TargetId = productId,
                    //    ActionType = "delete",
                    //    Description = $"Deleted product ID: {productId}."
                    //});

                    await _db.SaveChangesAsync();
                    successMessage = "Product deleted successfully.";
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Error deleting product (it might have active order items): " + ex.Message;
            }
        }

        private async Task TriggerDailyReport()
        {
            try
            {
                // Trigger daily report summaries calculation
                var today = DateOnly.FromDateTime(DateTime.Today);
                var todaySummary = await _db.StoreSalesDailies.FirstOrDefaultAsync(s => s.ReportDate == today);
                
                var todaysOrders = allOrders.Where(o => o.CreatedAt.HasValue && DateOnly.FromDateTime(o.CreatedAt.Value) == today && (o.OrderStatus.ToLower() == "completed" || o.OrderStatus.ToLower() == "processing" || o.OrderStatus.ToLower() == "delivered")).ToList();
                decimal todayRev = todaysOrders.Sum(o => o.TotalAmount);
                int todaySold = todaysOrders.Count; // Simplification

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
                await LoadData();
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
