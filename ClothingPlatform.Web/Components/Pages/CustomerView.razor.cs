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
        public NavigationManager Nav { get; set; }

        [Inject]
        public CustomerSessionState CustomerSession { get; set; }

        [Inject]
        public HttpClientServices httpClientServices { get; set; }

        [Inject]
        public IWebHostEnvironment WebHostEnvironment { get; set; }

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
        private ConfirmModal confirmModal = default!;

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
                    var dbUser = await _db.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.UserId == customerId && u.Role.RoleName == "customer");
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
                if (wantsToSignIn) { Nav.NavigateTo("customer-login?returnUrl=" + Uri.EscapeDataString(Nav.Uri)); }
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

                var total = CartTotal;

                var order = new Order
                {
                    UserId = currentUser.UserId,
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

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                dbCommitted = true;

                pointsEarnedInOrder = (int)(total / 100);
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

        private const string ProductImageFallbackUrl = "/images/products/no-image.svg";

        private static string NormalizeImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return ProductImageFallbackUrl;

            var trimmedUrl = imageUrl.Trim();
            if (trimmedUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                trimmedUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedUrl;
            }

            var normalizedPath = trimmedUrl.Replace('\\', '/').TrimStart('/');
            if (normalizedPath.StartsWith("images/products/", StringComparison.OrdinalIgnoreCase))
            {
                return $"/{normalizedPath}";
            }

            if (trimmedUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return trimmedUrl;
            }

            return $"/images/products/{normalizedPath}";
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
