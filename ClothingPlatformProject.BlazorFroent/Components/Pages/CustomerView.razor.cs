using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.BlazorFroent.Services;
using ClothingPlatformProject.Models.Cart;
using ClothingPlatformProject.Models.Notifications;
using ClothingPlatformProject.Models.Order;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingPlatformProject.BlazorFroent.Components.Pages
{
    public partial class CustomerView
    {
        [Inject]
        public AppDbContext _db { get; set; }

        [Inject]
        public NavigationManager Nav { get; set; }

        [Inject]
        public CustomerSessionState CustomerSession { get; set; }

        [Inject]
        public HttpClientServices httpClientServices { get; set; }

        // State variables
        private string activeTab = "home";
        private List<Product> allProducts = new();
        private List<Product> filteredProducts = new();
        private List<ProductDto> filteredProduct = new();
        private List<Category> allCategories = new();
        private List<Order> userOrders = new();
        private User? currentUser;
        private bool initializedFromStorage;
        private HubConnection? notificationHub;
        private List<CustomerNotificationDto> notifications = new();


        private List<ProductDto> allProduct = new();
        private List<BestSellerDto> allBestSellers = new();
        private List<NewCreationDto> allNewCreations = new();

        


        // Search & filter
        private int selectedCategoryId = 0;
        private string searchQuery = "";
        private int PageNoB = 1;
        private int PageNoC = 1;
        private int PageSize = 5;
        private int TotalPageCountB;
        private int TotalPageCountC;
        private int TotalPagesB;
        private int TotalPagesC;

        private int PageNo = 1;
        private int TotalPageCount;
        private int TotalPages;
        private int pageSize = 5;
        // Quick View Modal
        private Product? selectedProduct;
        private ProductDto? selectedProductDto;
        private string selectedSize = "";
        private string selectedColor = "";
        private int selectedQuantity = 1;
        private string modalErrorMessage = "";
        private bool isModalOpen = false;
        private bool isLoggedIn = false;
        // Shopping Bag drawer
        private bool isCartOpen = false;
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
        private int pointsEarnedInOrder = 0;
        private string confirmedOrderId = "";
        private bool isSuccessOpen = false;

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
            // Seed sample products/categories if empty
            DbSeeder.Seed(_db);
            

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
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || initializedFromStorage) return;
            initializedFromStorage = true;

            try
            {
                var customerIdValue = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "customerId");
                if (int.TryParse(customerIdValue, out var customerId) && currentUser == null)
                {
                    var dbUser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == customerId && u.Role == "customer");
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
            catch
            {
                // Local storage and SignalR are unavailable during prerendering.
            }
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
                        .Include(o => o.Payments)
                        .Where(o => o.UserId == currentUser.UserId)
                        .OrderByDescending(o => o.OrderId)
                        .ToListAsync();

                    // Calculate loyalty points for confirmed orders.
                    var totalSpent = userOrders
                        .Where(o => OrderWorkflow.Normalize(o.OrderStatus) == OrderWorkflow.Confirm)
                        .Sum(o => o.TotalAmount);
                    loyaltyPoints = (int)(totalSpent / 5000);
                }
            }
            catch (Exception ex)
            {
                ShowToast("Error loading catalog: " + ex.Message);
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
                BasePrice = prod.BasePrice,
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
                BasePrice = prod.BasePrice,
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
                BasePrice = prod.BasePrice,
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
            
                    await AddToBagAsync();
       
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
                string message = "Please sign in to add items to your bag.";
                var isConfirm = await JSRuntime.InvokeAsync<bool>("confirm", message);
                if (isConfirm)
                {
                    Nav.NavigateTo("customer-login?returnUrl=" + Uri.EscapeDataString(Nav.Uri));
                }
                return;
            }

            if (modalProduct == null) return;

            if (string.IsNullOrEmpty(selectedSize) || string.IsNullOrEmpty(selectedColor))
            {
                modalErrorMessage = "Please select both a size and color before adding to bag.";
                return;
            }

            var variant = modalProduct.VariantsDto
                .FirstOrDefault(v => v.Size == selectedSize && v.Color == selectedColor);

            if (variant == null)
            {
                modalErrorMessage = "Selected combination is currently unavailable.";
                return;
            }

            if (variant.StockQuantity <= 0)
            {
                modalErrorMessage = "This variant is currently out of stock.";
                return;
            }

            if (selectedQuantity < 1)
            {
                selectedQuantity = 1;
            }

            if (selectedQuantity > variant.StockQuantity)
            {
                modalErrorMessage = "Cannot exceed available stock.";
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

            ShowToast($"Added {modalProduct.Name} to bag!");
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
        }

        private async Task UpdateQty(CartItemModel item, int change)
        {
            // Verify stock
            var dbVariant = _db.ProductVariants.FirstOrDefault(v => v.VariantId == item.VariantId);
            if (dbVariant != null)
            {
                if (change > 0 && item.Qty + change > dbVariant.StockQuantity)
                {
                    ShowToast("Cannot exceed available stock.");
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

        private async Task RemoveItem(CartItemModel item)
        {
            await httpClientServices.ExecuteAsync<string>($"api/cart/item/{item.CartItemId}", null, EnumHttpMethod.Delete);
            await LoadCartAsync();
            ShowToast("Item removed from bag.");
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
                ShowToast("Your bag is empty!");
                return;
            }
            isCartOpen = false;
            Navigate("checkout");
        }

        // Checkout & Payment Methods
        private void SelectPaymentMethod(string method)
        {
            selectedPayment = method;
            if (method == "cod")
            {
                slipUploaded = true; // No upload needed for COD
                paymentReference = "COD";
            }
            else
            {
                slipUploaded = false; // Needs upload simulation
                paymentReference = "";
            }
        }

        private void SimulateSlipUpload()
        {
            slipFileName = "screenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            slipUploaded = true;
            ShowToast("Screenshot uploaded successfully!");
        }

        private async Task PlaceOrder()
        {
            string message = "Are you sure you want to comfirm you purchase??";
            var isConfirm = await JSRuntime.InvokeAsync<bool>("confirm", message);
            if (isConfirm)
            {




                if (string.IsNullOrWhiteSpace(coName) || string.IsNullOrWhiteSpace(coPhone) ||
                    string.IsNullOrWhiteSpace(coAddress) || string.IsNullOrWhiteSpace(coCity))
                {
                    ShowToast("Please fill in all delivery details");
                    return;
                }

                if (string.IsNullOrEmpty(selectedPayment))
                {
                    ShowToast("Please select a payment method");
                    return;
                }

                if (!slipUploaded)
                {
                    ShowToast("Please upload your payment screenshot");
                    return;
                }

                if (selectedPayment != "cod" && string.IsNullOrWhiteSpace(paymentReference))
                {
                    ShowToast("Please enter your payment transaction/reference number");
                    return;
                }

                if (!cart.Any())
                {
                    ShowToast("Your bag is empty");
                    return;
                }

                try
                {
                    // Create checkout order via transactions
                    var total = CartTotal;

                    var order = new Order
                    {
                        UserId = currentUser!.UserId,
                        TotalAmount = total,
                        OrderStatus = OrderWorkflow.Pending,
                        PaymentStatus = "unpaid",
                        ShippingAddress = $"{coAddress}, {coCity} (Phone: {coPhone})",
                        CreatedAt = DateTime.Now
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

                        // Decrement variant stock
                        var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.VariantId == item.VariantId);
                        if (variant != null)
                        {
                            variant.StockQuantity = Math.Max(0, variant.StockQuantity - item.Qty);
                        }
                    }

                    // Add Payment entry
                    _db.Payments.Add(new Payment
                    {
                        OrderId = order.OrderId,
                        PaymentMethod = selectedPayment,
                        PaymentStatus = "pending",
                        Amount = total,
                        TransactionId = selectedPayment == "cod" ? "COD" : paymentReference.Trim(),
                        SlipImageUrl = selectedPayment == "cod" ? null : slipFileName,
                        CreatedAt = DateTime.Now
                    });

                    await _db.SaveChangesAsync();

                    pointsEarnedInOrder = (int)(total / 100);
                    confirmedOrderId = $"ORD-{order.OrderId:D4}";
                    isSuccessOpen = true;
                    await httpClientServices.ExecuteAsync<string>($"api/cart/user/{currentUser.UserId}/clear", null, EnumHttpMethod.Delete);
                }
                catch (Exception ex)
                {
                    ShowToast("Error placing order: " + ex.Message);
                }
                
            }
            else
            {
                return;
            }
        }
        private async Task AfterOrder()
        {
            isSuccessOpen = false;
            cart.Clear();
            selectedPayment = "";
            slipUploaded = false;
            slipFileName = "";
            paymentReference = "";
            
            await LoadData(); // reload history
            Navigate("history");
        }

        // Customer Profile methods
        private async Task SaveProfile()
        {
            if (string.IsNullOrWhiteSpace(profFirstName) || string.IsNullOrWhiteSpace(profLastName) || 
                string.IsNullOrWhiteSpace(profEmail) || string.IsNullOrWhiteSpace(profAddress))
            {
                ShowToast("Please fill in all profile details");
                return;
            }

            try
            {
                var dbUser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == currentUser!.UserId && u.Role == "customer");
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

                    ShowToast("Profile updated successfully");
                    Navigate("profile");
                }
            }
            catch (Exception ex)
            {
                ShowToast("Error updating profile: " + ex.Message);
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
            toasts.Add(msg);
            StateHasChanged();
            
            Task.Delay(3000).ContinueWith(_ =>
            {
                toasts.Remove(msg);
                InvokeAsync(StateHasChanged);
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

        private static string NormalizeImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return "";
            if (imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                imageUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return imageUrl;
            }

            return $"/images/products/{imageUrl}";
        }
        private bool showLogoutConfirm = false;
        private void GotoLogin()
        {
            Nav.NavigateTo("/customer-login");
        }
        private void RequestLogout() => showLogoutConfirm = true;
        private void CancelLogout() => showLogoutConfirm = false;
        private async Task ConfirmLogout()
        {
            showLogoutConfirm = false;
            await Logout();
        }

        private async Task Logout()
        {
            CustomerSession.Logout();
            currentUser = null;
            cart.Clear();
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "customerId");
            Nav.NavigateTo("/customer-login");
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
        }
        // Unified modal model
        private class ModalProductDto
        {
            public string Name { get; set; } = "";
            public string CategoryName { get; set; } = "";
            public decimal BasePrice { get; set; }
            public string Description { get; set; } = "";
            public string? ImageDto { get; set; }
            public List<VariantDto> VariantsDto { get; set; } = new();
            public string AddToBagMethod { get; set; } = ""; // "collection", "bestseller", "newcreation"
        }

        private ModalProductDto? modalProduct = null;

    }
}
