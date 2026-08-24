using ClothingPlatform.Api.Models.Cart;
using ClothingPlatform.Api.Models.Notifications;
using ClothingPlatform.Api.Models.Order;
using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Web.Components.Partial;
using ClothingPlatform.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingPlatform.Web.Components.Pages
{
    public partial class CustomerView
    {
        [Inject]
        public AppDbContext _db { get; set; }

        [Inject]
        public IDbContextFactory<AppDbContext> DbFactory { get; set; }

        [Inject]
        public NavigationManager Nav { get; set; }

        [Inject]
        public CustomerSessionState CustomerSession { get; set; }

        [Inject]
        public HttpClientServices httpClientServices { get; set; }

        [Inject]
        public IWebHostEnvironment WebHostEnvironment { get; set; }

        [Inject]
        public ClothingPlatform.Web.Services.ServerCookieService ServerCookies { get; set; }

        [Inject]
        public IConfiguration Configuration { get; set; }

        private string _apiBaseUrl => (Configuration["ApiUrl"] ?? "https://localhost:7065").TrimEnd('/');

        // State variables
        private string activeTab = "home";
        private List<Product> allProducts = new();
        private List<Product> filteredProducts = new();
        private List<ProductDto> filteredProduct = new();
        private List<Category> allCategories = new();
        private List<Order> userOrders = new();
        private List<OrderReturn> userReturns = new();
        private User? currentUser;
        private bool initializedFromStorage;
        private HubConnection? notificationHub;
        private List<CustomerNotificationDto> notifications = new();
        private bool showNotificationsDropdown = false;
        private ConfirmModal?confirmModal;
        private int? expandedOrderId;
        private CustomerNotificationDto? selectedNotification;
        private bool showReceiptInDrawer = false;
        private string previousTabBeforeNoti = "home";

        // Promotions slide and pages state variables
        private int currentPromoSlideIndex = 0;
        private bool isPromoSliderRunning = false;
        private List<Promotion> promotionsList = new();
        private List<Promotion> couponsList = new();
        private Promotion? selectedPromotionDetail;

        private string enteredPromoCode = "";
        private List<string> appliedPromoCodes = new();
        private decimal appliedPromoDiscount => cart.Sum(item => item.CouponDiscountAmount);
        private decimal CartSubtotal => cart.Sum(item => (item.Price * item.Qty) - item.DiscountAmount);
        private decimal TotalSaved => cart.Sum(item => item.DiscountAmount) + appliedPromoDiscount;

        private decimal GetCustomerLoyaltyDiscountPercent()
        {
            if (currentUser == null) return 0m;
            if (loyaltyPoints >= 2000) return 20m;     // Ruby VIP (20%)
            if (loyaltyPoints >= 1000) return 15m;     // Diamond (15%)
            if (loyaltyPoints >= 500)  return 12m;     // Platinum (12%)
            if (loyaltyPoints >= 200)  return 10m;     // Gold (10%)
            if (loyaltyPoints >= 50)   return 5m;      // Silver Member (5%)
            return 0m;                                 // Normal (< 50 pts, 0%)
        }

        private async Task RecalculateCartDiscountsAsync()
        {
            foreach (var item in cart)
            {
                item.DiscountAmount = 0;
                item.DiscountPercent = 0;
                item.CouponDiscountAmount = 0;
            }

            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                var today = DateTime.Today;

                // 1. Get all active campaign banners (which are NOT coupons)
                var activeBanners = await db.Promotions
                    .AsNoTracking()
                    .Where(p => p.Enabled && !p.IsCoupon && 
                                (!p.StartDate.HasValue || today >= p.StartDate.Value) && 
                                (!p.EndDate.HasValue || today <= p.EndDate.Value))
                    .ToListAsync();

                // 2. Get all applied coupon codes
                var activeCoupons = new List<Promotion>();
                if (appliedPromoCodes.Any())
                {
                    activeCoupons = await db.Promotions
                        .AsNoTracking()
                        .Where(p => p.Enabled && p.IsCoupon && 
                                   appliedPromoCodes.Contains(p.PromoCode) && 
                                   (!p.StartDate.HasValue || today >= p.StartDate.Value) && 
                                   (!p.EndDate.HasValue || today <= p.EndDate.Value))
                        .ToListAsync();
                }

                // Upfront fetch for variants and products to solve N+1 queries
                var cartVariantIds = cart.Select(item => item.VariantId).Distinct().ToList();
                var variants = await db.ProductVariants.AsNoTracking()
                    .Where(v => cartVariantIds.Contains(v.VariantId))
                    .ToListAsync();
                var variantMap = variants.ToDictionary(v => v.VariantId);

                var productIds = variants.Select(v => v.ProductId).Distinct().ToList();
                var products = await db.Products.AsNoTracking()
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToListAsync();
                var productMap = products.ToDictionary(p => p.ProductId);

                // 3. Apply percent discounts (campaign + coupons + member tier discount)
                decimal loyaltyPct = GetCustomerLoyaltyDiscountPercent();

                foreach (var item in cart)
                {
                    var variant = variantMap.TryGetValue(item.VariantId, out var v) ? v : null;
                    if (variant == null) continue;

                    var product = productMap.TryGetValue(variant.ProductId, out var p) ? p : null;
                    if (product == null) continue;

                    decimal campaignPct = 0;
                    if (product.PromoId.HasValue)
                    {
                        var banner = activeBanners.FirstOrDefault(b => b.PromoId == product.PromoId.Value);
                        if (banner != null)
                        {
                            campaignPct = banner.DiscountPercent;
                        }
                    }

                    decimal couponPct = 0;
                    foreach (var coupon in activeCoupons.Where(c => string.IsNullOrEmpty(c.PromoType) || c.PromoType == "Percent"))
                    {
                        decimal val = coupon.DiscountPercent;
                        couponPct += val;
                    }
                    
                    decimal totalAddPct = Math.Min(couponPct + loyaltyPct, 100);

                    item.DiscountPercent = campaignPct;
                    item.DiscountAmount = (item.Price * item.Qty) * (item.DiscountPercent / 100);
                    item.CouponDiscountAmount = (item.Price * item.Qty) * (totalAddPct / 100);
                }

                // 4. Apply fixed discount coupons
                var fixedCoupons = activeCoupons.Where(c => c.PromoType == "Fixed").ToList();
                foreach (var coupon in fixedCoupons)
                {
                    decimal totalFixed = coupon.DiscountValue;
                    if (totalFixed <= 0) continue;

                    var eligibleItems = new List<(CartItemModel Item, decimal RemainingVal)>();
                    foreach (var item in cart)
                    {
                        var variant = variantMap.TryGetValue(item.VariantId, out var v) ? v : null;
                        if (variant == null) continue;

                        var product = productMap.TryGetValue(variant.ProductId, out var p) ? p : null;
                        if (product == null) continue;

                        decimal remaining = (item.Price * item.Qty) - item.DiscountAmount - item.CouponDiscountAmount;
                        if (remaining > 0)
                        {
                            eligibleItems.Add((item, remaining));
                        }
                    }

                    decimal eligibleRemainingSum = eligibleItems.Sum(x => x.RemainingVal);
                    if (eligibleRemainingSum > 0)
                    {
                        decimal allocated = 0;
                        for (int i = 0; i < eligibleItems.Count; i++)
                        {
                            var entry = eligibleItems[i];
                            if (i == eligibleItems.Count - 1)
                            {
                                decimal finalAlloc = Math.Min(entry.RemainingVal, totalFixed - allocated);
                                entry.Item.CouponDiscountAmount += finalAlloc;
                            }
                            else
                            {
                                decimal alloc = Math.Round((entry.RemainingVal / eligibleRemainingSum) * totalFixed, 2);
                                decimal finalAlloc = Math.Min(entry.RemainingVal, alloc);
                                entry.Item.CouponDiscountAmount += finalAlloc;
                                allocated += finalAlloc;
                            }
                        }
                    }
                }
            }
            catch
            {
                foreach (var item in cart)
                {
                    item.DiscountAmount = 0;
                    item.DiscountPercent = 0;
                    item.CouponDiscountAmount = 0;
                }
            }
        }
        private decimal GrandTotal => CartSubtotal - appliedPromoDiscount;
        private string promoCodeMessage = "";
        private bool promoCodeSuccess = false;

        private List<ProductDto> allProduct = new();
        private List<BestSellerDto> allBestSellers = new();
        private List<NewCreationDto> allNewCreations = new();

        


        // Search & filter
        private int selectedCategoryId = 0;
        private string searchQuery = "";
        private int PageNoB = 1;
        private int PageNoC = 1;
        private int PageSize = 10;
        private int TotalPageCountB;
        private int TotalPageCountC;
        private int TotalPagesB;
        private int TotalPagesC;

        private int PageNo = 1;
        private int TotalPageCount;
        private int TotalPages;
        private int pageSize = 10;
        // Quick View Modal
        private Product? selectedProduct;
        private ProductDto? selectedProductDto;
        private string selectedSize = "";
        private string selectedColor = "";
        private int selectedQuantity = 1;
        private string modalErrorMessage = "";
        private bool isModalOpen = false;
        private bool isLoggedIn = false;
        private string debugError = "";
        // Shopping Bag drawer
        private bool isCartOpen = false;
        private string selectedPromoFilter = "All";
        private List<CartItemModel> cart = new();
        private decimal CartTotal => cart.Sum(i => i.Price * i.Qty);
        private int CartCount => cart.Sum(i => i.Qty);

        // Checkout inputs
        private string coName = "";
        private string coPhone = "";
        private string coAddress = "";
        private string coCity = "";
        private string selectedPayment = ""; // "kpay", "wave_money", "cod"
        private string paymentReference = "";
        private bool slipUploaded = false;
        private string slipFileName = "";
        private string slipPreviewDataUrl = "";
        private string slipUploadError = "";
        private byte[]? selectedSlipBytes;
        private string selectedSlipContentType = "";
        private string selectedSlipExtension = "";
        private const long MaxSlipFileSizeBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedSlipExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };
        private static readonly HashSet<string> AllowedSlipContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };
        private int pointsEarnedInOrder = 0;
        private string confirmedOrderId = "";
        private bool isSuccessOpen = false;
        private bool isAddingToBag = false;
        private bool isPlacingOrder = false;
        private bool isContinuingAfterOrder = false;
        private bool isSavingCustomerProfile = false;
        private bool isCustomerLoggingOut = false;
        private readonly HashSet<int> cartItemActionIds = new();

        // Profile inputs
        private string profFirstName = "";
        private string profLastName = "";
        private string profEmail = "";
        private string profPhone = "";
        private string profAddress = "";
        private string profCity = "Yangon";
        private int loyaltyPoints = 0;
        private string profileAvatar = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=120&q=80";

        private List<BestSellerDto> bestSeller = new() ;
        private List<NewCreationDto> newCreation = new();
        // Toast notifications
        private class ToastMessage
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Text { get; set; } = "";
        }
        private List<ToastMessage> toasts = new();

        protected override async Task OnInitializedAsync()
        {
            currentUser = CustomerSession.CurrentUser;
            LoadProfileFields();
            await LoadData();         // loads categories, products, orders
            await LoadNewCreation();  // loads new creation page 1
            await LoadBestSeller();   // loads best seller page 1
            await LoadCollection();
            if (currentUser != null)
            {
                await LoadCartAsync();
                await LoadNotificationsAsync();
            }
            _ = StartPromoSlider();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || initializedFromStorage) return;
            initializedFromStorage = true;

            try
            {
                // Clean up legacy localStorage items to prevent confusion and sync to cookie
                try
                {
                    await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "customerId");
                    await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
                }
                catch {}

                // Read customerId from HttpOnly cookie server-side
                var customerId = ServerCookies.GetCustomerId();
                if (customerId.HasValue && currentUser == null)
                {
                    await using var db = await DbFactory.CreateDbContextAsync();
                    var dbUser = await db.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.UserId == customerId.Value && u.Role.RoleName == "customer");
                    if (dbUser != null)
                    {
                        CustomerSession.Login(dbUser);
                        currentUser = dbUser;
                        LoadProfileFields();
                        await LoadData();
                        await LoadCartAsync();
                        await LoadNotificationsAsync();
                        await ConnectNotificationHubAsync();
                        StateHasChanged();
                    }
                }
                else if (currentUser != null)
                {
                    await ConnectNotificationHubAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error in OnAfterRenderAsync: {ex}");
                debugError = ex.ToString();
                StateHasChanged();
            }
            try { await JSRuntime.InvokeVoidAsync("moveNavIndicator"); } catch { }
        }

        private void LoadProfileFields()
        {
            if (currentUser != null)
            {
                profFirstName = currentUser.FirstName;
                profLastName = currentUser.LastName;
                profEmail = currentUser.Email;
                profAddress = currentUser.Address ?? "";
                profCity = "Yangon"; // Default city

                // Pre-fill checkout details
                coName = $"{currentUser.FirstName} {currentUser.LastName}";
                coAddress = currentUser.Address ?? "";
                coPhone = currentUser.PhoneNumber;
                coCity = profCity;
            }
            else
            {
                return;
            }

        }

        private async Task LoadCollection(string search = "", int categoryId = 0)
        {
            var query = $"api/product/allcollection?page={PageNo}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) query += $"&search={Uri.EscapeDataString(search)}";
            if (categoryId > 0) query += $"&categoryId={categoryId}";

            var res = await httpClientServices.ExecuteAsync<PagedResult<ProductDto>>(
                query, null, EnumHttpMethod.Get);

            if (res != null)
            {
                allProduct = res.Items;
                filteredProduct = allProduct.ToList();
                TotalPageCount = res.TotalCount;
                TotalPages = (int)Math.Ceiling((double)TotalPageCount / pageSize);
            }
        }

        // Separate method just for new creation pagination
        private async Task LoadNewCreation(string search = "", int categoryId = 0)
        {
            var query = $"api/product/newCreation?page={PageNoC}&pageSize={PageSize}";
            if (!string.IsNullOrWhiteSpace(search)) query += $"&search={Uri.EscapeDataString(search)}";
            if (categoryId > 0) query += $"&categoryId={categoryId}";
            var results =  await httpClientServices.ExecuteAsync<PagedResult<NewCreationDto>>(
                query, null, EnumHttpMethod.Get);

            if (results != null)
            {
                allNewCreations = results.Items;
                newCreation = allNewCreations.ToList();
                TotalPageCountC = results.TotalCount;
                TotalPagesC = (int)Math.Ceiling((double)TotalPageCountC / PageSize);
            }
        }

        // Separate method just for best seller pagination
        private async Task LoadBestSeller(string search = "", int categoryId = 0)
        {
            var query = $"api/product/bestSeller?page={PageNoB}&pageSize={PageSize}";
            if (!string.IsNullOrWhiteSpace(search)) query += $"&search={Uri.EscapeDataString(search)}";
            if (categoryId > 0) query += $"&categoryId={categoryId}";
            var result = await httpClientServices.ExecuteAsync<PagedResult<BestSellerDto>>(
                query,null,  EnumHttpMethod.Get);

            if (result != null)
            {
                allBestSellers = result.Items;
                bestSeller = allBestSellers.ToList();
                TotalPageCountB = result.TotalCount;
                TotalPagesB = (int)Math.Ceiling((double)result.TotalCount / PageSize);
            }
        }

        // Update ChangePage to only reload what's needed
        private async Task ChangePage(int newPage)
        {
            PageNoC = newPage;
            await LoadNewCreation();
            StateHasChanged();
        }

        private async Task ChangePageAll(int newPage)
        {
            PageNo = newPage;
            await LoadCollection();
            StateHasChanged();
        }

        private async Task ChangeBPage(int newPage)
        {
            PageNoB = newPage;
            await LoadBestSeller();
            StateHasChanged(); // ⚠️ you were also missing this!
        }

        private async Task LoadData()
        {
            try
            {
                allCategories = await _db.Categories.AsNoTracking().ToListAsync();
                var today = DateTime.Today;
                promotionsList = await _db.Promotions
                    .AsNoTracking()
                    .Where(p => p.Enabled && !p.IsCoupon && (!p.StartDate.HasValue || today >= p.StartDate.Value) && (!p.EndDate.HasValue || today <= p.EndDate.Value))
                    .ToListAsync();
                
                couponsList = await _db.Promotions
                    .AsNoTracking()
                    .Where(p => p.Enabled && p.IsCoupon && (!p.StartDate.HasValue || today >= p.StartDate.Value) && (!p.EndDate.HasValue || today <= p.EndDate.Value))
                    .ToListAsync();
                
                allProducts = await _db.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .AsNoTracking()
                    .ToListAsync();

                await ApplyProductFilters();

                if (currentUser != null)
                {
                    // Load customer order history
                    userOrders = await _db.Orders
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.Variant)
                                .ThenInclude(v => v.Product)
                                    .ThenInclude(p => p.ProductImages)
                        .Include(o => o.Payments)
                        .Include(o => o.StaffFulfillmentLogs)
                        .Where(o => o.UserId == currentUser.UserId)
                        .OrderByDescending(o => o.OrderId)
                        .ToListAsync();

                    var orderIds = userOrders.Select(o => o.OrderId).ToList();
                    userReturns = await _db.OrderReturns
                        .Where(r => orderIds.Contains(r.OrderId))
                        .ToListAsync();

                    // Calculate loyalty points for all valid non-cancelled orders (1 pt per 5,000 MMK).
                    var totalSpent = userOrders
                        .Where(o => !OrderWorkflow.IsCancelled(o.OrderStatus))
                        .Sum(o => o.TotalAmount);
                    loyaltyPoints = (int)(totalSpent / 5000);
                    await RecalculateCartDiscountsAsync();
                }
            }
            catch (Exception ex)
            {
                ShowToast(UiMessages.CustomerShop.CatalogLoadFailed(ex.Message));
            }
        }
        private string _searchQuery = "";


        private async Task ApplyProductFilters()
        {
            // Reset to page 1 when search changes
            PageNo = 1;
            PageNoB = 1;
            PageNoC = 1;

            await Task.WhenAll(
                LoadCollection(searchQuery, selectedCategoryId),
                LoadNewCreation(searchQuery, selectedCategoryId),
                LoadBestSeller(searchQuery, selectedCategoryId)
            );

            StateHasChanged();
        }
        private CancellationTokenSource? _searchCts;

        private async Task OnSearchInput(ChangeEventArgs e)
        {
            searchQuery = e.Value?.ToString() ?? "";

            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            try
            {
                await Task.Delay(350, token);
                await ApplyProductFilters();
            }
            catch (TaskCanceledException) { }
        }

        private void Navigate(string tab)
        {
            activeTab = tab;
            if (tab == "profile")
            {
                LoadProfileFields();
            }
            if (tab == "checkout")
            {
                AutofillCheckoutFromProfile();
            }
            TriggerNavIndicatorUpdate();
        }

        private void TriggerNavIndicatorUpdate()
        {
            _ = InvokeAsync(async () =>
            {
                await Task.Delay(50);
                try { await JSRuntime.InvokeVoidAsync("moveNavIndicator"); } catch { }
            });
        }


        private void SelectCategory(int categoryId)
        {
            selectedCategoryId = categoryId;
            ApplyProductFilters();
        }

        // Quick View Modal methods
       
       

        // 🟢 အမှန်ပြင်ဆင်ရန်ပုံစံ (Type ကို BestSellerDto သို့ ပြောင်းလဲလိုက်ပါပြီ):
        private BestSellerDto? selectedProducts;

        
        private NewCreationDto? selectedProductss;

        private void OpenQuickView(ProductDto prod)
        {
            modalProduct = new ModalProductDto
            {
                Name = prod.Name,
                CategoryName = prod.CategoryName,
                SalePrice = prod.SalePrice,
                Description = prod.Description,
                ImageDto = prod.ImageDto,
                VariantsDto = prod.VariantsDto,
                AddToBagMethod = "collection"
            };
            selectedSize = "";
            selectedColor = "";
            selectedQuantity = 1;
            modalErrorMessage = "";
            isModalOpen = true;
        }

        private void OpenQuickViews(BestSellerDto prod)
        {
            modalProduct = new ModalProductDto
            {
                Name = prod.Name,
                CategoryName = prod.CategoryName,
                SalePrice = prod.SalePrice,
                Description = prod.Description,
                ImageDto = prod.ImageDto,
                VariantsDto = prod.VariantsDto ?? new List<VariantDto>(), // ✅ null guard
                AddToBagMethod = "bestseller"
            };
            selectedSize = "";
            selectedColor = "";
            selectedQuantity = 1;
            modalErrorMessage = "";
            isModalOpen = true;

        }

        private void OpenQuickViewss(NewCreationDto prod)
        {
            modalProduct = new ModalProductDto
            {
                Name = prod.Name,
                CategoryName = prod.CategoryName,
                SalePrice = prod.SalePrice,
                Description = prod.Description,
                ImageDto = prod.ImageDto,
                VariantsDto = prod.VariantsDto ?? new List<VariantDto>(), // ✅ null guard
                AddToBagMethod = "newcreation"
            };

            selectedSize = "";
            selectedColor = "";
            selectedQuantity = 1;
            modalErrorMessage = "";
            isModalOpen = true;
        }



        private void CloseQuickView()
        {
            isModalOpen = false;
            modalProduct = null;
        }

        private async Task AddToBagUnified()
        {
            if (isAddingToBag)
            {
                return;
            }

            isAddingToBag = true;
            StateHasChanged();

            try
            {
                await AddToBagAsync();
            }
            finally
            {
                isAddingToBag = false;
                StateHasChanged();
            }
        }

        private void SelectSize(string size)
        {
            selectedSize = size;
            modalErrorMessage = "";
        }

        private void SelectColor(string color)
        {
            selectedColor = color;
            modalErrorMessage = "";
        }

        // Cart Drawer methods
        private void ToggleCart()
        {
            isCartOpen = !isCartOpen;
        }


        private async Task AddToBagAsync()
        {
            if (currentUser == null)
            {
                var wantsToSignIn = await confirmModal.ShowAsync(title:
                    "Sign In Required",
                    message: UiMessages.CustomerShop.AddToBagSignInConfirm, confirmText: "Sign In"); 
                if (wantsToSignIn) { Nav.NavigateTo("portal-login?returnUrl=" + Uri.EscapeDataString(Nav.Uri)); }
                return;
            }
                if (modalProduct == null) return;

            if (string.IsNullOrEmpty(selectedSize) || string.IsNullOrEmpty(selectedColor))
            {
                modalErrorMessage = UiMessages.CustomerShop.SelectSizeAndColor;
                return;
            }

            var variant = modalProduct.VariantsDto
                .FirstOrDefault(v => v.Size == selectedSize && v.Color == selectedColor);

            if (variant == null)
            {
                modalErrorMessage = UiMessages.CustomerShop.VariantUnavailable;
                return;
            }

            if (variant.StockQuantity <= 0)
            {
                modalErrorMessage = UiMessages.CustomerShop.VariantOutOfStock;
                return;
            }

            if (selectedQuantity < 1)
            {
                selectedQuantity = 1;
            }

            if (selectedQuantity > variant.StockQuantity)
            {
                modalErrorMessage = UiMessages.CustomerShop.ModalStockExceeded;
                return;
            }

            await httpClientServices.ExecuteAsync<CartDto>(
                "api/cart/add",
                new AddToCartRequest
                {
                    UserId = currentUser.UserId,
                    VariantId = variant.VariantId,
                    Quantity = selectedQuantity
                },
                EnumHttpMethod.Post);

            await LoadCartAsync();

            ShowToast(UiMessages.CustomerShop.AddedToBag(modalProduct.Name));
            CloseQuickView();
            isCartOpen = true;
        }

        private async Task LoadCartAsync()
        {
            if (currentUser == null)
            {
                cart.Clear();
                return;
            }

            var result = await httpClientServices.ExecuteAsync<CartDto>($"api/cart/user/{currentUser.UserId}");
            cart = result?.Items.Select(i => new CartItemModel
            {
                CartItemId = i.CartItemId,
                VariantId = i.VariantId,
                Name = i.ProductName,
                Size = i.Size,
                Color = i.Color,
                Price = i.UnitPrice,
                Qty = i.Quantity,
                ImgUrl = NormalizeImageUrl(i.ImageUrl)
            }).ToList() ?? new List<CartItemModel>();

            await RecalculateCartDiscountsAsync();
        }

        private async Task UpdateQty(CartItemModel item, int change)
        {
            if (!cartItemActionIds.Add(item.CartItemId))
            {
                return;
            }

            // Verify stock
            try
            {
                var dbVariant = _db.ProductVariants.FirstOrDefault(v => v.VariantId == item.VariantId);
                if (dbVariant != null)
                {
                    if (change > 0 && item.Qty + change > dbVariant.StockQuantity)
                    {
                        ShowToast(UiMessages.CustomerShop.CartStockExceeded);
                        return;
                    }
                }

                var nextQuantity = item.Qty + change;
                if (nextQuantity <= 0)
                {
                    await RemoveItem(item);
                    return;
                }

                await httpClientServices.ExecuteAsync<CartDto>(
                    $"api/cart/item/{item.CartItemId}",
                    new UpdateCartItemRequest { Quantity = nextQuantity },
                    EnumHttpMethod.Put);

                await LoadCartAsync();
            }
            finally
            {
                cartItemActionIds.Remove(item.CartItemId);
                StateHasChanged();
            }
        }

        private async Task RemoveItem(CartItemModel item)
        {
            var ownsAction = cartItemActionIds.Add(item.CartItemId);
            if (!ownsAction && item.Qty > 1)
            {
                return;
            }

            try
            {
                await httpClientServices.ExecuteAsync<string>($"api/cart/item/{item.CartItemId}", null, EnumHttpMethod.Delete);
                await LoadCartAsync();
                ShowToast(UiMessages.CustomerShop.CartItemRemoved);
            }
            finally
            {
                if (ownsAction)
                {
                    cartItemActionIds.Remove(item.CartItemId);
                }

                StateHasChanged();
            }
        }

        private void ChangeSelectedQuantity(int change)
        {
            selectedQuantity = Math.Max(1, selectedQuantity + change);
            modalErrorMessage = "";
        }

        private void GoCheckout()
        {
            if (!cart.Any())
            {
                ShowToast(UiMessages.CustomerShop.CheckoutBagEmpty);
                return;
            }
            isCartOpen = false;
            Navigate("checkout");
        }

        private bool IsCartItemBusy(int cartItemId) => cartItemActionIds.Contains(cartItemId);

        // Checkout & Payment Methods
        private void SelectPaymentMethod(string method)
        {
            selectedPayment = method;
            if (method == "cod")
            {
                slipUploaded = true; // No upload needed for COD
                paymentReference = "COD";
                ClearSlipSelection();
            }
            else
            {
                slipUploaded = false; // Needs real screenshot upload
                paymentReference = "";
                ClearSlipSelection();
            }
        }

        private async Task HandleSlipSelected(InputFileChangeEventArgs e)
        {
            ClearSlipSelection(false);

            var file = e.File;
            if (file == null)
            {
                slipUploadError = UiMessages.CustomerShop.PaymentSlipRequired;
                return;
            }

            var extension = Path.GetExtension(file.Name);
            if (!AllowedSlipExtensions.Contains(extension) ||
                !AllowedSlipContentTypes.Contains(file.ContentType))
            {
                slipUploadError = UiMessages.CustomerShop.PaymentSlipInvalidFormat;
                return;
            }

            if (file.Size > MaxSlipFileSizeBytes)
            {
                slipUploadError = UiMessages.CustomerShop.PaymentSlipTooLarge;
                return;
            }

            try
            {
                await using var stream = file.OpenReadStream(MaxSlipFileSizeBytes);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                selectedSlipBytes = memoryStream.ToArray();
                selectedSlipContentType = file.ContentType;
                selectedSlipExtension = extension.ToLowerInvariant();
                slipFileName = Path.GetFileName(file.Name);
                slipPreviewDataUrl = $"data:{selectedSlipContentType};base64,{Convert.ToBase64String(selectedSlipBytes)}";
                slipUploaded = true;
                slipUploadError = "";
                ShowToast(UiMessages.CustomerShop.PaymentSlipUploaded);
            }
            catch (Exception ex)
            {
                ClearSlipSelection(false);
                slipUploadError = UiMessages.CustomerShop.PaymentSlipReadFailed(ex.Message);
            }
        }

        private void ClearSlipSelection(bool clearError = true)
        {
            slipUploaded = selectedPayment == "cod";
            slipFileName = "";
            slipPreviewDataUrl = "";
            selectedSlipBytes = null;
            selectedSlipContentType = "";
            selectedSlipExtension = "";
            if (clearError)
            {
                slipUploadError = "";
            }
        }

        private async Task<(string Url, string PhysicalPath)> SaveSelectedSlipAsync()
        {
            if (selectedSlipBytes == null || selectedSlipBytes.Length == 0)
            {
                throw new InvalidOperationException(UiMessages.CustomerShop.PaymentSlipRequired);
            }

            var webRootPath = WebHostEnvironment.WebRootPath
                ?? Path.Combine(WebHostEnvironment.ContentRootPath, "wwwroot");
            var folder = Path.Combine(webRootPath, "images", "payment-slips");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}{selectedSlipExtension}";
            var physicalPath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(physicalPath, selectedSlipBytes);

            return ($"/images/payment-slips/{fileName}", physicalPath);
        }

        private async Task PlaceOrder()
        {
            if (isPlacingOrder)
            {
                return;
            }

            isPlacingOrder = true;
            StateHasChanged();

            string? savedSlipPhysicalPath = null;
            var dbCommitted = false;

            try
            {
                var isConfirm = await confirmModal.ShowAsync(title: "Confirm Purchase", message: UiMessages.CustomerShop.PlaceOrderConfirm, confirmText: "Confirm"); if (!isConfirm) return;

                if (string.IsNullOrWhiteSpace(coName) || string.IsNullOrWhiteSpace(coPhone) ||
                    string.IsNullOrWhiteSpace(coAddress) || string.IsNullOrWhiteSpace(coCity))
                {
                    ShowToast(UiMessages.CustomerShop.DeliveryDetailsRequired);
                    return;
                }

                var cleanCoPhone = (coPhone ?? "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Trim();
                if (!System.Text.RegularExpressions.Regex.IsMatch(cleanCoPhone, @"^(?:09\d{9}|\+959\d{9}|959\d{9})$"))
                {
                    ShowToast("Phone number must start with 09 or +959 (11 digits)");
                    return;
                }

                if (string.IsNullOrEmpty(selectedPayment))
                {
                    ShowToast(UiMessages.CustomerShop.PaymentMethodRequired);
                    return;
                }

                if (selectedPayment != "cod" && !slipUploaded)
                {
                    ShowToast(UiMessages.CustomerShop.PaymentSlipRequired);
                    return;
                }

                if (selectedPayment != "cod" && selectedSlipBytes == null)
                {
                    ShowToast(UiMessages.CustomerShop.PaymentSlipRequired);
                    return;
                }

                if (selectedPayment != "cod" && string.IsNullOrWhiteSpace(paymentReference))
                {
                    ShowToast(UiMessages.CustomerShop.PaymentReferenceRequired);
                    return;
                }

                if (!cart.Any())
                {
                    ShowToast(UiMessages.CustomerShop.SubmitBagEmpty);
                    return;
                }

                if (currentUser == null)
                {
                    ShowToast(UiMessages.CustomerShop.CheckoutSignInRequired);
                    return;
                }

                string? savedSlipUrl = null;
                if (selectedPayment != "cod")
                {
                    var savedSlip = await SaveSelectedSlipAsync();
                    savedSlipUrl = savedSlip.Url;
                    savedSlipPhysicalPath = savedSlip.PhysicalPath;
                }

                await using var transaction = await _db.Database.BeginTransactionAsync();

                var total = GrandTotal;

                var order = new Order
                {
                    UserId = currentUser.UserId,
                    TotalAmount = total,
                    OrderStatus = OrderWorkflow.Pending,
                    PaymentStatus = "unpaid",
                    ShippingAddress = $"{coAddress}, {coCity} (Phone: {coPhone})",
                    CreatedAt = DateTime.Now,
                    AppliedPromo = appliedPromoCodes.Any() ? string.Join(",", appliedPromoCodes) : null
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync(); // generate OrderId

                // Add OrderItems and deduct stock
                foreach (var item in cart)
                {
                    _db.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        VariantId = item.VariantId,
                        Quantity = item.Qty,
                        PriceAtPurchase = item.Price
                    });

                    var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.VariantId == item.VariantId);
                    if (variant != null)
                    {
                        variant.StockQuantity = Math.Max(0, variant.StockQuantity - item.Qty);
                    }
                }

                _db.Payments.Add(new Payment
                {
                    OrderId = order.OrderId,
                    PaymentMethod = selectedPayment,
                    PaymentStatus = "pending",
                    Amount = total,
                    TransactionId = selectedPayment == "cod" ? "COD" : paymentReference.Trim(),
                    SlipImageUrl = selectedPayment == "cod" ? null : savedSlipUrl,
                    CreatedAt = DateTime.Now
                });

                // Increment promotion Redeemed count for all applied promo codes
                foreach (var code in appliedPromoCodes)
                {
                    var appliedPromo = await _db.Promotions.FirstOrDefaultAsync(p => p.PromoCode == code);
                    if (appliedPromo != null)
                    {
                        appliedPromo.Redeemed += 1;
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                dbCommitted = true;

                pointsEarnedInOrder = (int)(total / 1000) * (appliedPromoCodes.Contains("LOYAL2X") ? 2 : 1);
                confirmedOrderId = $"ORD-{order.OrderId:D4}";
                isSuccessOpen = true;

                try
                {
                    await httpClientServices.ExecuteAsync<string>($"api/cart/user/{currentUser.UserId}/clear", null, EnumHttpMethod.Delete);
                }
                catch (Exception ex)
                {
                    ShowToast(UiMessages.CustomerShop.CartClearAfterOrderFailed(ex.Message));
                }
            }
            catch (Exception ex)
            {
                if (!dbCommitted && !string.IsNullOrWhiteSpace(savedSlipPhysicalPath) && File.Exists(savedSlipPhysicalPath))
                {
                    try
                    {
                        File.Delete(savedSlipPhysicalPath);
                    }
                    catch
                    {
                        // The failed checkout should still surface the original error.
                    }
                }

                ShowToast(UiMessages.CustomerShop.PlaceOrderFailed(ex.Message));
            }
            finally
            {
                isPlacingOrder = false;
                StateHasChanged();
            }
        }
        private async Task AfterOrder()
        {
            if (isContinuingAfterOrder)
            {
                return;
            }

            isContinuingAfterOrder = true;
            StateHasChanged();

            try
            {
                isSuccessOpen = false;
                cart.Clear();
                selectedPayment = "";
                paymentReference = "";
                ClearSlipSelection();

                appliedPromoCodes.Clear();
                enteredPromoCode = "";
                promoCodeMessage = "";
                promoCodeSuccess = false;

                await LoadData(); // reload history
                Navigate("history");
            }
            finally
            {
                isContinuingAfterOrder = false;
                StateHasChanged();
            }
        }

        // Customer Profile methods
        private async Task SaveProfile()
        {
            if (isSavingCustomerProfile)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(profFirstName) || string.IsNullOrWhiteSpace(profLastName) || 
                string.IsNullOrWhiteSpace(profEmail) || string.IsNullOrWhiteSpace(profAddress))
            {
                ShowToast(UiMessages.CustomerShop.ProfileDetailsRequired);
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(profEmail.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            {
                ShowToast("Email not format");
                return;
            }

            var cleanPhone = (coPhone ?? "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Trim();
            if (string.IsNullOrWhiteSpace(coPhone) || !System.Text.RegularExpressions.Regex.IsMatch(cleanPhone, @"^(?:09\d{9}|\+959\d{9}|959\d{9})$"))
            {
                ShowToast("Phone number must start with 09 or +959 (11 digits)");
                return;
            }

            isSavingCustomerProfile = true;
            StateHasChanged();

            try
            {
                var dbUser = await _db.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == currentUser!.UserId && u.Role.RoleName == "customer");
                if (dbUser != null)
                {
                    dbUser.FirstName = profFirstName.Trim();
                    dbUser.LastName = profLastName.Trim();
                    dbUser.Email = profEmail.Trim();
                    dbUser.Address = profAddress.Trim();
                    dbUser.PhoneNumber = coPhone.Trim();

                    await _db.SaveChangesAsync();
                    
                    // Sync session
                    CustomerSession.Login(dbUser);
                    currentUser = dbUser;

                    ShowToast(UiMessages.CustomerShop.ProfileUpdated);
                    Navigate("profile");
                }
            }
            catch (Exception ex)
            {
                ShowToast(UiMessages.CustomerShop.ProfileUpdateFailed(ex.Message));
            }
            finally
            {
                isSavingCustomerProfile = false;
                StateHasChanged();
            }
        }

        private void AutofillCheckoutFromProfile()
        {
            if (currentUser != null)
            {
                coName = string.IsNullOrWhiteSpace(coName) ? $"{currentUser.FirstName} {currentUser.LastName}" : coName;
                coAddress = string.IsNullOrWhiteSpace(coAddress) ? currentUser.Address : coAddress;
                coPhone = string.IsNullOrWhiteSpace(coPhone) ? currentUser.PhoneNumber : coPhone;
                coCity = string.IsNullOrWhiteSpace(coCity) ? profCity : coCity;
            }
        }

        // Custom simulated toast notifications
        private void ShowToast(string message)
        {
            var msg = new ToastMessage { Text = message };
            InvokeAsync(() =>
            {
                toasts.Add(msg);
                StateHasChanged();
            });
            
            Task.Delay(3000).ContinueWith(_ =>
            {
                InvokeAsync(() =>
                {
                    toasts.Remove(msg);
                    StateHasChanged();
                });
            });
        }

        private async Task LoadNotificationsAsync()
        {
            if (currentUser == null) return;

            var result = await httpClientServices.ExecuteAsync<List<CustomerNotificationDto>>(
                $"api/notifications/user/{currentUser.UserId}");
            notifications = result ?? new List<CustomerNotificationDto>();
        }

        private async Task ConnectNotificationHubAsync()
        {
            if (currentUser == null || notificationHub != null) return;

            notificationHub = new HubConnectionBuilder()
                .WithUrl("https://localhost:7065/hubs/customer-notifications")
                .WithAutomaticReconnect()
                .Build();

            notificationHub.On<CustomerNotificationDto>("CustomerNotification", notification =>
            {
                notifications.Insert(0, notification);
                ShowToast(notification.Message);
                InvokeAsync(StateHasChanged);
            });

            await notificationHub.StartAsync();
            await notificationHub.InvokeAsync("JoinCustomerGroup", currentUser.UserId);
        }

        private const string ProductImageFallbackUrl = "/images/products/no-image.svg";

        private string NormalizeImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return ProductImageFallbackUrl;

            var trimmedUrl = imageUrl.Trim();

            // External URLs and data URIs — return as-is
            if (trimmedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                trimmedUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedUrl;
            }

            // Already an absolute rooted path — prefix with API base URL
            if (trimmedUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return $"{_apiBaseUrl}{trimmedUrl}";
            }

            var normalizedPath = trimmedUrl.Replace('\\', '/').TrimStart('/');

            // Handle filenames prepended with a GUID and underscore (e.g. "abc123_filename.jpg")
            if (normalizedPath.Contains('_'))
            {
                var parts = normalizedPath.Split('_');
                // Only strip the prefix when it looks like a GUID (32+ hex chars)
                if (parts[0].Length >= 32 && parts[0].All(c => char.IsLetterOrDigit(c)))
                {
                    normalizedPath = string.Join("_", parts, 1, parts.Length - 1);
                }
            }

            // Sub-folder paths — preserve them and route through API
            if (normalizedPath.StartsWith("images/", System.StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith("returns/", System.StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith("payment-slips/", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"{_apiBaseUrl}/{normalizedPath}";
            }

            // Plain filename — serve from API's images/products/ folder
            return $"{_apiBaseUrl}/images/products/{normalizedPath}";
        }
        private bool showLogoutConfirm = false;
        private void GotoLogin()
        {
            Nav.NavigateTo("/portal-login");
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
            CustomerSession.Logout();
            currentUser = null;
            cart.Clear();
            // Clear HttpOnly cookies server-side
            try
            {
                await JSRuntime.InvokeVoidAsync("authCookieHelper.clear");
            }
            catch {}
            ServerCookies.ClearAuthCookies();
            try
            {
                await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "customerId");
                await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            }
            catch {}
            Nav.NavigateTo("/portal-login");
        }

        private void ToggleNotificationsDropdown()
        {
            showNotificationsDropdown = !showNotificationsDropdown;
        }

        private async Task MarkAllNotificationsAsRead()
        {
            if (currentUser == null) return;
            foreach (var noti in notifications.Where(n => !n.IsRead))
            {
                try
                {
                    await httpClientServices.ExecuteAsync<object>($"api/notifications/{noti.NotificationId}/read", null, EnumHttpMethod.Post);
                    noti.IsRead = true;
                }
                catch { }
            }
            StateHasChanged();
        }

        private string GetTimeAgo(DateTime dt)
        {
            var span = DateTime.Now - dt;
            if (span.TotalDays > 365) return $"{(int)(span.TotalDays / 365)} years ago";
            if (span.TotalDays > 30) return $"{(int)(span.TotalDays / 30)} months ago";
            if (span.TotalDays > 7) return $"{(int)(span.TotalDays / 7)} weeks ago";
            if (span.TotalDays >= 1) return $"{(int)span.TotalDays} day{((int)span.TotalDays > 1 ? "s" : "")} ago";
            if (span.TotalHours >= 1) return $"{(int)span.TotalHours} hour{((int)span.TotalHours > 1 ? "s" : "")} ago";
            if (span.TotalMinutes >= 1) return $"{(int)span.TotalMinutes} minute{((int)span.TotalMinutes > 1 ? "s" : "")} ago";
            return "Just now";
        }

        private async Task MarkAsRead(CustomerNotificationDto noti)
        {
            try
            {
                await httpClientServices.ExecuteAsync<object>($"api/notifications/{noti.NotificationId}/read", null, EnumHttpMethod.Post);
            }
            catch { }
            StateHasChanged();
        }

        private async Task HandleNotificationClick(CustomerNotificationDto noti)
        {
            if (!noti.IsRead)
            {
                try
                {
                    await httpClientServices.ExecuteAsync<object>($"api/notifications/{noti.NotificationId}/read", null, EnumHttpMethod.Post);
                    noti.IsRead = true;
                }
                catch { }
            }
            showNotificationsDropdown = false;
            selectedNotification = noti;
            StateHasChanged();
        }

        private void GoBackFromNotification()
        {
            activeTab = string.IsNullOrEmpty(previousTabBeforeNoti) || previousTabBeforeNoti == "notification-detail" ? "home" : previousTabBeforeNoti;
            selectedNotification = null;
            StateHasChanged();
        }

        private void ViewReceiptFromNotification()
        {
            if (selectedNotification?.OrderId != null)
            {
                showReceiptInDrawer = true;
            }
        }

        private void CloseNotificationDrawer()
        {
            selectedNotification = null;
            showReceiptInDrawer = false;
        }

        private void ViewPurchasesFromNotification()
        {
            activeTab = "history";
            selectedNotification = null;
            StateHasChanged();
        }

        private void ToggleOrderDetails(int orderId)
        {
            if (expandedOrderId == orderId)
            {
                expandedOrderId = null;
            }
            else
            {
                expandedOrderId = orderId;
            }
        }

        private async Task OpenProductFromOrder(int productId)
        {
            var prod = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (prod != null)
            {
                var primaryImage = prod.ProductImages.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl;
                modalProduct = new ModalProductDto
                {
                    Name = prod.Name,
                    CategoryName = prod.Category?.Name ?? "Collection",
                    SalePrice = prod.ProductVariants.FirstOrDefault()?.SalePrice ?? 0,
                    Description = prod.Description ?? "",
                    ImageDto = primaryImage,
                    VariantsDto = prod.ProductVariants.Select(v => new VariantDto
                    {
                        VariantId = v.VariantId,
                        Size = v.Size ?? "",
                        Color = v.Color ?? "",
                        StockQuantity = v.StockQuantity,
                        Sku = v.Sku ?? "",
                        SalePrice = v.SalePrice ?? 0,
                        PurchasePrice = v.PurchasePrice
                    }).ToList(),
                    AddToBagMethod = "collection"
                };
                selectedSize = "";
                selectedColor = "";
                selectedQuantity = 1;
                modalErrorMessage = "";
                isModalOpen = true;
            }
        }

        private bool isCancelModalOpen = false;
        private string cancelReason = "";
        private int cancelOrderId = 0;
        private string cancelModalErrorMessage = "";

        private void OpenCancelModal(int orderId)
        {
            cancelOrderId = orderId;
            cancelReason = "";
            cancelModalErrorMessage = "";
            isCancelModalOpen = true;
        }

        private void CloseCancelModal()
        {
            isCancelModalOpen = false;
        }

        private async Task SubmitCancelOrder()
        {
            var reason = string.IsNullOrWhiteSpace(cancelReason) ? "No reason provided" : cancelReason.Trim();
            isCancelModalOpen = false;
            await CancelCustomerOrder(cancelOrderId, reason);
        }

        private async Task CancelCustomerOrder(int orderId, string reason)
        {
            try
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();

                var order = await _db.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    ShowToast("Order not found.");
                    return;
                }

                var normalizedStatus = OrderWorkflow.Normalize(order.OrderStatus);
                if (normalizedStatus == OrderWorkflow.Confirm || OrderWorkflow.IsCancelled(normalizedStatus))
                {
                    ShowToast("This order cannot be cancelled as it is already delivered or cancelled.");
                    return;
                }

                order.OrderStatus = OrderWorkflow.CancelledByCustomer;
                order.CancelReason = reason;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                ShowToast($"Cancellation request for Order ORD-{orderId:D4} has been submitted for approval.");
                
                // Reload data to reflect changes
                await LoadData();
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to cancel order: {ex.Message}");
            }
        }

        // Client model for memory cart
        public class CartItemModel
        {
            public int CartItemId { get; set; }
            public int VariantId { get; set; }
            public string Name { get; set; } = "";
            public string Size { get; set; } = "";
            public string Color { get; set; } = "";
            public decimal Price { get; set; }
            public int Qty { get; set; }
            public string ImgUrl { get; set; } = "";
            public decimal DiscountAmount { get; set; }
            public decimal DiscountPercent { get; set; }
            public decimal CouponDiscountAmount { get; set; }
        }
        // Unified modal model
        private class ModalProductDto
        {
            public string Name { get; set; } = "";
            public string CategoryName { get; set; } = "";
            public decimal SalePrice { get; set; }
            public string Description { get; set; } = "";
            public string? ImageDto { get; set; }
            public List<VariantDto> VariantsDto { get; set; } = new();
            public string AddToBagMethod { get; set; } = ""; // "collection", "bestseller", "newcreation"
        }

        private ModalProductDto? modalProduct = null;

        // Order Return / Exchange Tab State
        private Order? selectedReturnOrder;
        private OrderItem? selectedReturnItem;
        private int selectedReturnItemId;
        
        private string returnReceiptFileName = "";
        private byte[]? returnReceiptBytes;
        private string returnReceiptContentType = "";
        private string returnReceiptExtension = "";
        private string returnReceiptPreviewUrl = "";
        private string returnReceiptError = "";
        
        private bool retReasonColorMismatch;
        private bool retReasonDamage;
        private bool retReasonSizeIssue;
        private bool retReasonExchangeRequest;
        
        private string returnCustomMessage = "";
        private bool isSubmittingReturn = false;
        private ProductVariant? selectedExchangeVariant;
        private bool showExchangePicker = false;

        private string returnOption = "Refund"; // "Refund" or "Exchange"
        
        // -- Promo page computed stats --------------------------------------
        private int TotalCodesRedeemed => promotionsList.Sum(p => p.Redeemed);
        private int NewArrivalsCount   => allProducts.Count(p => p.CreatedAt.HasValue && p.CreatedAt.Value >= DateTime.Today.AddDays(-7));

        private bool HasBeenReturned(int orderId, int variantId)
        {
            return userReturns.Any(r => r.OrderId == orderId && r.VariantId == variantId && !string.Equals(r.Status, "Rejected", StringComparison.OrdinalIgnoreCase));
        }

        private bool HasBeenReturned(int orderId, int variantId, out string option)
        {
            var ret = userReturns.FirstOrDefault(r => r.OrderId == orderId && r.VariantId == variantId && !string.Equals(r.Status, "Rejected", StringComparison.OrdinalIgnoreCase));
            option = ret?.ReturnOption ?? "";
            return ret != null;
        }

        private OrderReturn? GetOrderReturn(int orderId, int variantId)
        {
            return userReturns.FirstOrDefault(r => r.OrderId == orderId && r.VariantId == variantId);
        }

        private bool CanSelectForReturn(Order order)
        {
            if (order == null) return false;
            if (CanRequestReturn(order)) return true;
            return userReturns.Any(r => r.OrderId == order.OrderId);
        }

        private bool CanRequestReturn(Order order)
        {
            if (order == null || OrderWorkflow.Normalize(order.OrderStatus) != OrderWorkflow.Confirm)
            {
                return false;
            }

            // Must have at least one item that hasn't been returned yet
            if (order.OrderItems.All(oi => HasBeenReturned(order.OrderId, oi.VariantId)))
            {
                return false;
            }

            // The delivery (confirmation) date is retrieved from the StaffFulfillmentLogs where ActionTaken is Confirm
            var confirmLog = order.StaffFulfillmentLogs?
                .FirstOrDefault(l => string.Equals(l.ActionTaken, "Confirm", StringComparison.OrdinalIgnoreCase));

            DateTime deliveryDate = confirmLog?.ActionAt ?? order.CreatedAt ?? DateTime.Now;

            // Customer can only return the order during 7 days after delivered
            return (DateTime.Now - deliveryDate).TotalDays <= 7;
        }

        private void InitiateReturn(Order order)
        {
            selectedReturnOrder = order;
            selectedReturnItem = order.OrderItems.FirstOrDefault(oi => !HasBeenReturned(order.OrderId, oi.VariantId));
            selectedReturnItemId = selectedReturnItem?.OrderItemId ?? 0;
            
            returnReceiptFileName = "";
            returnReceiptBytes = null;
            returnReceiptContentType = "";
            returnReceiptExtension = "";
            returnReceiptPreviewUrl = "";
            returnReceiptError = "";
            
            retReasonColorMismatch = false;
            retReasonDamage = false;
            retReasonSizeIssue = false;
            retReasonExchangeRequest = false;
            
            returnCustomMessage = "";
            returnOption = "Refund";
            isSubmittingReturn = false;

            selectedExchangeVariant = null;
            showExchangePicker = false;
            
            activeTab = "return";
            StateHasChanged();
            TriggerNavIndicatorUpdate();
        }

        private void NavigateToReturnTab()
        {
            if (currentUser != null)
            {
                var latestDelivered = userOrders
                    .FirstOrDefault(o => CanSelectForReturn(o));
                if (latestDelivered != null)
                {
                    InitiateReturn(latestDelivered);
                    return;
                }
            }
            activeTab = "return";
            StateHasChanged();
            TriggerNavIndicatorUpdate();
        }

        private void OnReturnOrderChanged(ChangeEventArgs e)
        {
            if (e.Value != null && int.TryParse(e.Value.ToString(), out var orderId))
            {
                var order = userOrders.FirstOrDefault(o => o.OrderId == orderId);
                if (order != null)
                {
                    selectedReturnOrder = order;
                    selectedReturnItem = order.OrderItems.FirstOrDefault(oi => !HasBeenReturned(order.OrderId, oi.VariantId));
                    selectedReturnItemId = selectedReturnItem?.OrderItemId ?? 0;
                    selectedExchangeVariant = null;
                    StateHasChanged();
                }
            }
        }

        private void OnReturnItemChanged(ChangeEventArgs e)
        {
            if (e.Value != null && int.TryParse(e.Value.ToString(), out var itemId))
            {
                selectedReturnItemId = itemId;
                selectedReturnItem = selectedReturnOrder?.OrderItems.FirstOrDefault(oi => oi.OrderItemId == itemId);
                StateHasChanged();
            }
        }

        private void OnSelectExchangeVariant(Product product, ChangeEventArgs e)
        {
            if (e.Value != null && int.TryParse(e.Value.ToString(), out var variantId))
            {
                var variant = product.ProductVariants.FirstOrDefault(v => v.VariantId == variantId);
                if (variant != null)
                {
                    variant.Product = product;
                    selectedExchangeVariant = variant;
                    showExchangePicker = false;
                    StateHasChanged();
                }
            }
        }

        private async Task HandleReturnReceiptSelected(InputFileChangeEventArgs e)
        {
            returnReceiptError = "";
            var file = e.File;
            if (file == null)
            {
                returnReceiptError = "Receipt image is required.";
                return;
            }

            var extension = Path.GetExtension(file.Name);
            if (!AllowedSlipExtensions.Contains(extension) ||
                !AllowedSlipContentTypes.Contains(file.ContentType))
            {
                returnReceiptError = "Please select a valid JPG, PNG, or WEBP image.";
                return;
            }

            if (file.Size > MaxSlipFileSizeBytes)
            {
                returnReceiptError = "Image file size exceeds the 5MB limit.";
                return;
            }

            try
            {
                await using var stream = file.OpenReadStream(MaxSlipFileSizeBytes);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                returnReceiptBytes = memoryStream.ToArray();
                returnReceiptContentType = file.ContentType;
                returnReceiptExtension = extension.ToLowerInvariant();
                returnReceiptFileName = Path.GetFileName(file.Name);
                returnReceiptPreviewUrl = $"data:{returnReceiptContentType};base64,{Convert.ToBase64String(returnReceiptBytes)}";
                ShowToast("Receipt uploaded successfully.");
            }
            catch (Exception ex)
            {
                returnReceiptBytes = null;
                returnReceiptError = $"Failed to read file: {ex.Message}";
            }
        }

        private async Task SubmitReturnRequest()
        {
            if (selectedReturnOrder == null || selectedReturnItem == null)
            {
                ShowToast("Please select a valid order and item to return.");
                return;
            }

            // Validation check: Either checkbox is checked OR text message box is filled
            bool hasCheckboxSelected = retReasonColorMismatch || retReasonDamage || retReasonSizeIssue || retReasonExchangeRequest;
            bool hasCustomMessage = !string.IsNullOrWhiteSpace(returnCustomMessage);

            if (!hasCheckboxSelected && !hasCustomMessage)
            {
                ShowToast("Please select at least one predefined reason checkbox or write details in the message box.");
                return;
            }

            if (returnReceiptBytes == null)
            {
                ShowToast("Please upload a receipt/bill image.");
                return;
            }

            if (returnOption == "Exchange" && selectedExchangeVariant == null)
            {
                ShowToast("Please select a replacement item for the exchange.");
                return;
            }

            bool confirmed = await confirmModal.ShowAsync(
                title: "Submit Return / Exchange",
                message: "Are you sure you want to submit this return/exchange request?",
                confirmText: "Submit");
            if (!confirmed)
            {
                return;
            }

            if (isSubmittingReturn) return;
            isSubmittingReturn = true;
            StateHasChanged();

            try
            {
                if (selectedReturnOrder == null || !CanRequestReturn(selectedReturnOrder))
                {
                    ShowToast("Error: This order is no longer eligible for returns (exceeded 7 days after delivery).");
                    isSubmittingReturn = false;
                    StateHasChanged();
                    return;
                }

                // Save receipt image
                var webRootPath = WebHostEnvironment.WebRootPath
                    ?? Path.Combine(WebHostEnvironment.ContentRootPath, "wwwroot");
                var folder = Path.Combine(webRootPath, "images", "returns");
                Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid():N}{returnReceiptExtension}";
                var physicalPath = Path.Combine(folder, fileName);
                await File.WriteAllBytesAsync(physicalPath, returnReceiptBytes);
                var savedReceiptUrl = $"/images/returns/{fileName}";

                // Gather checkbox reasons
                var reasonsList = new List<string>();
                if (retReasonColorMismatch) reasonsList.Add("Color didn't match");
                if (retReasonDamage) reasonsList.Add("Damage order");
                if (retReasonSizeIssue) reasonsList.Add("Size issue");
                if (retReasonExchangeRequest) reasonsList.Add("Want to exchange with other item");
                var reasonCheckboxStr = string.Join(", ", reasonsList);

                // Save to database
                var customMessage = returnCustomMessage.Trim();
                if (returnOption == "Exchange" && selectedExchangeVariant != null)
                {
                    var exchangeDetail = $"[EXCHANGE REQUEST: {selectedExchangeVariant.Product?.Name ?? "Product"} (Variant ID: {selectedExchangeVariant.VariantId}, Size: {selectedExchangeVariant.Size}, Color: {selectedExchangeVariant.Color})]";
                    customMessage = string.IsNullOrWhiteSpace(customMessage) 
                        ? exchangeDetail 
                        : $"{exchangeDetail}\n\nAdditional comments:\n{customMessage}";
                }

                var newReturn = new OrderReturn
                {
                    OrderId = selectedReturnOrder.OrderId,
                    VariantId = selectedReturnItem.VariantId,
                    Quantity = selectedReturnItem.Quantity,
                    ReasonCheckbox = reasonCheckboxStr,
                    ReasonText = customMessage,
                    ReceiptImageUrl = savedReceiptUrl,
                    ReturnOption = returnOption,
                    CreatedAt = DateTime.Now
                };

                _db.OrderReturns.Add(newReturn);
                await _db.SaveChangesAsync();

                var orderIds = userOrders.Select(o => o.OrderId).ToList();
                userReturns = await _db.OrderReturns
                    .Where(r => orderIds.Contains(r.OrderId))
                    .ToListAsync();

                ShowToast("Return/Exchange request submitted successfully!");
                // Reset the return form after successful submission
                selectedReturnOrder       = null;
                selectedReturnItem        = null;
                selectedReturnItemId      = 0;
                returnReceiptBytes        = null;
                returnReceiptPreviewUrl   = string.Empty;
                returnReceiptFileName     = string.Empty;
                returnReceiptExtension    = string.Empty;
                returnReceiptContentType  = string.Empty;
                returnReceiptError        = string.Empty;
                retReasonColorMismatch    = false;
                retReasonDamage           = false;
                retReasonSizeIssue        = false;
                retReasonExchangeRequest  = false;
                returnCustomMessage       = string.Empty;
                returnOption              = "Refund";
                selectedExchangeVariant   = null;
                showExchangePicker        = false;
                activeTab = "history";
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to submit return request: {ex.Message}");
            }
            finally
            {
                isSubmittingReturn = false;
                StateHasChanged();
            }
        }

        private string contactFullName = "";
        private string contactEmail = "";
        private string contactMessage = "";

        private async Task SubmitContactMessage()
        {
            if (string.IsNullOrWhiteSpace(contactFullName))
            {
                ShowToast("Please enter your name.");
                return;
            }
            if (string.IsNullOrWhiteSpace(contactEmail))
            {
                ShowToast("Please enter your email.");
                return;
            }
            if (string.IsNullOrWhiteSpace(contactMessage))
            {
                ShowToast("Please enter your message.");
                return;
            }

            try
            {
                var newMsg = new ContactMessage
                {
                    FullName = contactFullName.Trim(),
                    Email = contactEmail.Trim(),
                    Message = contactMessage.Trim(),
                    CreatedAt = DateTime.Now
                };

                _db.ContactMessages.Add(newMsg);
                await _db.SaveChangesAsync();

                ShowToast("Thank you for your message! Our team will get back to you shortly.");
                contactFullName = "";
                contactEmail = "";
                contactMessage = "";
            }
            catch (Exception ex)
            {
                ShowToast($"Failed to send message: {ex.Message}");
            }
            
            StateHasChanged();
        }

        private async Task StartPromoSlider()
        {
            if (isPromoSliderRunning) return;
            isPromoSliderRunning = true;
            while (isPromoSliderRunning)
            {
                try
                {
                    await Task.Delay(5000);
                    if (!isPromoSliderRunning) break;
                    if (promotionsList.Any())
                    {
                        currentPromoSlideIndex = (currentPromoSlideIndex + 1) % promotionsList.Count;
                        await InvokeAsync(StateHasChanged);
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        private void NextPromoSlide()
        {
            if (promotionsList.Any())
            {
                currentPromoSlideIndex = (currentPromoSlideIndex + 1) % promotionsList.Count;
                StateHasChanged();
            }
        }

        private void PrevPromoSlide()
        {
            if (promotionsList.Any())
            {
                currentPromoSlideIndex = (currentPromoSlideIndex - 1 + promotionsList.Count) % promotionsList.Count;
                StateHasChanged();
            }
        }

        private void SetPromoSlide(int index)
        {
            currentPromoSlideIndex = index;
            StateHasChanged();
        }

        private async Task ClickPromoSlide(Promotion promo)
        {
            selectedPromotionDetail = promo;
            if (!string.IsNullOrEmpty(promo.PromoCode))
            {
                enteredPromoCode = promo.PromoCode;
                await ApplyPromoCodeCheckoutSilent();
            }
            Navigate("promotions");
        }

        private async Task SelectPromotion(Promotion promo)
        {
            selectedPromotionDetail = promo;
            if (!string.IsNullOrEmpty(promo.PromoCode))
            {
                enteredPromoCode = promo.PromoCode;
                await ApplyPromoCodeCheckoutSilent();
            }
            StateHasChanged();
        }

        private void OpenQuickViewFromProduct(Product prod)
        {
            if (prod == null) return;
            var primaryImage = prod.ProductImages?.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl;
            
            modalProduct = new ModalProductDto
            {
                Name = prod.Name,
                CategoryName = prod.Category?.Name ?? "Collection",
                SalePrice = prod.ProductVariants?.FirstOrDefault()?.SalePrice ?? 0,
                Description = prod.Description ?? "",
                ImageDto = primaryImage,
                VariantsDto = prod.ProductVariants?.Select(v => new VariantDto
                {
                    VariantId = v.VariantId,
                    Size = v.Size ?? "",
                    Color = v.Color ?? "",
                    StockQuantity = v.StockQuantity,
                    Sku = v.Sku ?? "",
                    SalePrice = v.SalePrice ?? 0,
                    PurchasePrice = v.PurchasePrice
                }).ToList() ?? new List<VariantDto>(),
                AddToBagMethod = "collection"
            };
            selectedSize = "";
            selectedColor = "";
            selectedQuantity = 1;
            modalErrorMessage = "";
            isModalOpen = true;
            StateHasChanged();
        }

        private async Task CopyPromoCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return;
            try
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", code);
                ShowToast($"Promo code '{code}' copied to clipboard!");
            }
            catch
            {
                ShowToast($"Promo Code: {code}");
            }
        }

        private async Task ApplyPromoAndGoToShop(Promotion promo)
        {
            if (!string.IsNullOrEmpty(promo.PromoCode))
            {
                enteredPromoCode = promo.PromoCode;
                await ApplyPromoCodeCheckoutSilent();
            }
            Navigate("shop");
        }

        private async Task ApplyPromoCodeCheckoutSilent()
        {
            if (string.IsNullOrWhiteSpace(enteredPromoCode)) return;

            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                var today = DateTime.Today;
                var matchingPromo = await db.Promotions.FirstOrDefaultAsync(p => 
                    p.Enabled && 
                    p.PromoCode.Trim() == enteredPromoCode.Trim() &&
                    (!p.StartDate.HasValue || today >= p.StartDate.Value) &&
                    (!p.EndDate.HasValue || today <= p.EndDate.Value));

                if (matchingPromo != null)
                {
                    if (appliedPromoCodes.Contains(matchingPromo.PromoCode))
                    {
                        return;
                    }

                    if (matchingPromo.UsageLimit > 0 && matchingPromo.Redeemed >= matchingPromo.UsageLimit)
                    {
                        promoCodeMessage = "This promo code has reached its usage limit.";
                        promoCodeSuccess = false;
                        return;
                    }

                    if (matchingPromo.UserLimit > 0 && currentUser != null)
                    {
                        int userRedeemed = await db.Orders.CountAsync(o => o.UserId == currentUser.UserId && o.AppliedPromo != null && o.AppliedPromo.Contains(matchingPromo.PromoCode));
                        if (userRedeemed >= matchingPromo.UserLimit)
                        {
                            promoCodeMessage = $"You have already used the promo code '{matchingPromo.PromoCode}'.";
                            promoCodeSuccess = false;
                            return;
                        }
                    }

                    if (matchingPromo.NewMemberOnly && currentUser != null)
                    {
                        int previousOrdersCount = await db.Orders.CountAsync(o => o.UserId == currentUser.UserId && !OrderWorkflow.IsCancelled(o.OrderStatus));
                        if (previousOrdersCount > 0)
                        {
                            promoCodeMessage = "This promo code is only available for new members.";
                            promoCodeSuccess = false;
                            return;
                        }
                    }

                    decimal tempDiscount = 0;
                    var promoType = matchingPromo.PromoType ?? "Percent";
                    decimal val = matchingPromo.DiscountValue > 0 ? matchingPromo.DiscountValue : matchingPromo.DiscountPercent;

                    if (promoType == "Percent")
                    {
                        if (matchingPromo.ApplyAll)
                        {
                            tempDiscount = CartTotal * (val / 100);
                        }
                        else
                        {
                            foreach (var item in cart)
                            {
                                var variant = await db.ProductVariants.FirstOrDefaultAsync(v => v.VariantId == item.VariantId);
                                if (variant != null)
                                {
                                    var product = await db.Products.FirstOrDefaultAsync(p => p.ProductId == variant.ProductId);
                                    if (product != null && product.PromoId == matchingPromo.PromoId)
                                    {
                                        tempDiscount += (item.Price * item.Qty) * (val / 100);
                                    }
                                }
                            }
                        }
                    }
                    else if (promoType == "Fixed")
                    {
                        if (matchingPromo.ApplyAll)
                        {
                            tempDiscount = val;
                        }
                        else
                        {
                            foreach (var item in cart)
                            {
                                var variant = await db.ProductVariants.FirstOrDefaultAsync(v => v.VariantId == item.VariantId);
                                if (variant != null)
                                {
                                    var product = await db.Products.FirstOrDefaultAsync(p => p.ProductId == variant.ProductId);
                                    if (product != null && product.PromoId == matchingPromo.PromoId)
                                    {
                                        tempDiscount += (item.Price * item.Qty);
                                    }
                                }
                            }
                            tempDiscount = Math.Min(tempDiscount, val);
                        }
                    }
                    else if (promoType == "Shipping")
                    {
                        tempDiscount = 0; // free shipping, shipping is already 0
                    }

                    if (promoType == "Percent" && val > 0 && tempDiscount == 0)
                    {
                        promoCodeMessage = "No eligible items in your bag for this promo code.";
                        promoCodeSuccess = false;
                        await RecalculateCartDiscountsAsync();
                    }
                    else if (promoType == "Fixed" && val > 0 && tempDiscount == 0)
                    {
                        promoCodeMessage = "No eligible items in your bag for this promo code.";
                        promoCodeSuccess = false;
                        await RecalculateCartDiscountsAsync();
                    }
                    else
                    {
                        appliedPromoCodes.Add(matchingPromo.PromoCode);
                        string displaySuccessMsg = promoType switch
                        {
                            "Percent" => $"Promo '{matchingPromo.PromoCode}' applied successfully! ({val}% Off)",
                            "Fixed" => $"Promo '{matchingPromo.PromoCode}' applied successfully! ({val:N0} ks Off)",
                            "Shipping" => $"Promo '{matchingPromo.PromoCode}' applied successfully! (Free Shipping)",
                            _ => $"Promo '{matchingPromo.PromoCode}' applied successfully!"
                        };
                        promoCodeMessage = displaySuccessMsg;
                        promoCodeSuccess = true;
                        await RecalculateCartDiscountsAsync();
                    }
                }
            }
            catch {}
        }

        private async Task ApplyPromoCodeCheckout()
        {
            if (string.IsNullOrWhiteSpace(enteredPromoCode))
            {
                promoCodeMessage = "Please enter a promo code.";
                promoCodeSuccess = false;
                return;
            }

            try
            {
                await using var db = await DbFactory.CreateDbContextAsync();
                var today = DateTime.Today;
                var matchingPromo = await db.Promotions.FirstOrDefaultAsync(p => 
                    p.Enabled && 
                    p.PromoCode.Trim() == enteredPromoCode.Trim() &&
                    (!p.StartDate.HasValue || today >= p.StartDate.Value) &&
                    (!p.EndDate.HasValue || today <= p.EndDate.Value));

                if (matchingPromo != null)
                {
                    if (appliedPromoCodes.Contains(matchingPromo.PromoCode))
                    {
                        promoCodeMessage = $"Promo code '{matchingPromo.PromoCode}' is already applied.";
                        promoCodeSuccess = false;
                        ShowToast("Promo code already applied.");
                        return;
                    }

                    if (matchingPromo.UsageLimit > 0 && matchingPromo.Redeemed >= matchingPromo.UsageLimit)
                    {
                        promoCodeMessage = "This promo code has reached its usage limit.";
                        promoCodeSuccess = false;
                        ShowToast("Promo code usage limit reached.");
                        return;
                    }

                    if (matchingPromo.UserLimit > 0 && currentUser != null)
                    {
                        int userRedeemed = await db.Orders.CountAsync(o => o.UserId == currentUser.UserId && o.AppliedPromo != null && o.AppliedPromo.Contains(matchingPromo.PromoCode));
                        if (userRedeemed >= matchingPromo.UserLimit)
                        {
                            promoCodeMessage = $"You have already used the promo code '{matchingPromo.PromoCode}'.";
                            promoCodeSuccess = false;
                            ShowToast("You have already used this promo code.");
                            return;
                        }
                    }

                    if (matchingPromo.NewMemberOnly && currentUser != null)
                    {
                        int previousOrdersCount = await db.Orders.CountAsync(o => o.UserId == currentUser.UserId && !OrderWorkflow.IsCancelled(o.OrderStatus));
                        if (previousOrdersCount > 0)
                        {
                            promoCodeMessage = "This promo code is only available for new members.";
                            promoCodeSuccess = false;
                            ShowToast("This promo code is only for new members.");
                            return;
                        }
                    }

                    decimal tempDiscount = 0;
                    var promoType = matchingPromo.PromoType ?? "Percent";
                    decimal val = matchingPromo.DiscountValue > 0 ? matchingPromo.DiscountValue : matchingPromo.DiscountPercent;

                    if (promoType == "Percent")
                    {
                        tempDiscount = CartTotal * (val / 100);
                    }
                    else if (promoType == "Fixed")
                    {
                        tempDiscount = val;
                    }
                    else if (promoType == "Shipping")
                    {
                        tempDiscount = 0;
                    }

                    if (promoType == "Percent" && val > 0 && tempDiscount == 0)
                    {
                        promoCodeMessage = "No eligible items in your bag for this promo code.";
                        promoCodeSuccess = false;
                        ShowToast("No eligible items for this promo.");
                        await RecalculateCartDiscountsAsync();
                    }
                    else if (promoType == "Fixed" && val > 0 && tempDiscount == 0)
                    {
                        promoCodeMessage = "No eligible items in your bag for this promo code.";
                        promoCodeSuccess = false;
                        ShowToast("No eligible items for this promo.");
                        await RecalculateCartDiscountsAsync();
                    }
                    else
                    {
                        appliedPromoCodes.Add(matchingPromo.PromoCode);
                        string displaySuccessMsg = promoType switch
                        {
                            "Percent" => $"Promo '{matchingPromo.PromoCode}' applied successfully! ({val}% Off)",
                            "Fixed" => $"Promo '{matchingPromo.PromoCode}' applied successfully! ({val:N0} ks Off)",
                            "Shipping" => $"Promo '{matchingPromo.PromoCode}' applied successfully! (Free Shipping)",
                            _ => $"Promo '{matchingPromo.PromoCode}' applied successfully!"
                        };
                        promoCodeMessage = displaySuccessMsg;
                        promoCodeSuccess = true;
                        ShowToast($"Discount code '{matchingPromo.PromoCode}' applied!");
                        await RecalculateCartDiscountsAsync();
                    }
                }
                else
                {
                    promoCodeMessage = "Invalid promo code.";
                    promoCodeSuccess = false;
                    ShowToast("Failed to apply promo code.");
                    await RecalculateCartDiscountsAsync();
                }
            }
            catch (Exception ex)
            {
                promoCodeMessage = $"Error applying promo code: {ex.Message}";
                promoCodeSuccess = false;
                ShowToast("Failed to apply promo code.");
                await RecalculateCartDiscountsAsync();
            }
            StateHasChanged();
        }

        public void Dispose()
        {
            isPromoSliderRunning = false;
        }

    }
}
