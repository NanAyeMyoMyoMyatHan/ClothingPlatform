using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.BlazorFroent.Services;
using ClothingPlatformProject.Models.Order;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
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
        public SessionState Session { get; set; }

        [Inject]
        public HttpClientServices httpClientServices { get; set; }

        // State variables
        private string activeTab = "home";
        private List<Product> allProducts = new();
        private List<Product> filteredProducts = new();
        private List<Category> allCategories = new();
        private List<Order> userOrders = new();
        private User? currentUser;

        // Search & filter
        private int selectedCategoryId = 0;
        private string searchQuery = "";
        private int PageNo = 1;
        private int PageSize = 5;
        private int TotalPageCount;
        private int TotalPages => (int)Math.Ceiling((double)TotalPageCount / PageSize);
        // Quick View Modal
        private Product? selectedProduct;
        private string selectedSize = "";
        private string selectedColor = "";
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
        private string selectedPayment = ""; // "kbz", "wave", "cod"
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
            try
            {
                // API မှ Best Sellers လှမ်းဆွဲခြင်း
                var bestResult = await httpClientServices.ExecuteAsync<List<BestSellerDto>>("api/product/bestsellers", null, EnumHttpMethod.Get);
                if (bestResult != null)
                {
                    bestSeller = bestResult;
                }

                // API မှ New Creations လှမ်းဆွဲခြင်း
                var newResult = await httpClientServices.ExecuteAsync<List<NewCreationDto>>("api/product/new-creations", null, EnumHttpMethod.Get);
                if (newResult != null)
                {
                    newCreation = newResult;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading UI Data: {ex.Message}");
            }



            // Authentication check
            if (!Session.IsLoggedIn)
            {

            }

            currentUser = Session.CurrentUser;
            LoadProfileFields();
            await LoadData();
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
                coPhone = profPhone;
                coCity = profCity;
            }

        }

        private async Task ChangePage(int newPage)
        {
            PageNo = newPage;
            await LoadData(); // စာမျက်နှာပြောင်းရင် ဒေတာ ပြန်မောင်းတင်မယ်
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

                ApplyProductFilters();

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

                   

                    // Calculate loyalty points: 1 point per 100 MMK spent on completed/delivered orders
                    var totalSpent = userOrders
                        .Where(o => o.OrderStatus.ToLower() == "completed" || o.OrderStatus.ToLower() == "processing" || o.OrderStatus.ToLower() == "delivered")
                        .Sum(o => o.TotalAmount);
                    loyaltyPoints = (int)(totalSpent / 100);
                }
            }
            catch (Exception ex)
            {
                ShowToast("Error loading catalog: " + ex.Message);
            }
        }

        private void ApplyProductFilters()
        {
            var query = allProducts.AsEnumerable();
           
            if (selectedCategoryId > 0)
            {
                query = query.Where(p => p.CategoryId == selectedCategoryId);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var q = searchQuery.ToLower().Trim();
                query = query.Where(p => p.Name.ToLower().Contains(q) || (p.Description != null && p.Description.ToLower().Contains(q)));
            }

            filteredProducts = query.ToList();
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
        private void OpenQuickView(Product product)
        {
            selectedProduct = product;
            selectedSize = "";
            selectedColor = "";
            modalErrorMessage = "";
            isModalOpen = true;
        }

       

        // 🟢 အမှန်ပြင်ဆင်ရန်ပုံစံ (Type ကို BestSellerDto သို့ ပြောင်းလဲလိုက်ပါပြီ):
        private BestSellerDto? selectedProducts;

        private void OpenQuickViews(BestSellerDto product)
        {
            selectedProducts = product; // 🤝 အမျိုးအစား တူသွားပြီဖြစ်လို့ အနီလိုင်း ချက်ချင်း ပျောက်သွားပါလိမ့်မည်
            selectedSize = "";
            selectedColor = "";
            modalErrorMessage = "";
            isModalOpen = true;
        }
        private NewCreationDto? selectedProductss;

        private void OpenQuickViewss(NewCreationDto product)
        {
            selectedProductss = product; // 🤝 အမျိုးအစား တူသွားပြီဖြစ်လို့ အနီလိုင်း ချက်ချင်း ပျောက်သွားပါလိမ့်မည်
            selectedSize = "";
            selectedColor = "";
            modalErrorMessage = "";
            isModalOpen = true;
        }

        private void CloseQuickView()
        {
            isModalOpen = false;
            selectedProduct = null;
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
            if (Session.CurrentUser== null)
            {
                string message = $"You are not logging in.Please login to make order";
                var isComfirm = await JSRuntime.InvokeAsync<bool>("confirm", message);
                if (isComfirm)
                {
                    Nav.NavigateTo("login?returnUrl=" + Uri.EscapeDataString(Nav.Uri));
                    return;
                }

               
            }
            else
            {


                if (selectedProduct == null) return;

                if (string.IsNullOrEmpty(selectedSize) || string.IsNullOrEmpty(selectedColor))
                {
                    modalErrorMessage = "Please select both a size and color before adding to bag.";
                    return;
                }

                var variant = selectedProduct.ProductVariants
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

                var cartItem = cart.FirstOrDefault(i => i.VariantId == variant.VariantId);
                if (cartItem != null)
                {
                    if (cartItem.Qty + 1 > variant.StockQuantity)
                    {
                        modalErrorMessage = $"Cannot add more items. Only {variant.StockQuantity} items in stock.";
                        return;
                    }
                    cartItem.Qty++;
                }
                else
                {
                    var primaryImg = selectedProduct.ProductImages.FirstOrDefault(i => (bool)i.IsPrimary)?.ImageUrl
                        ?? "https://images.unsplash.com/photo-1515886657613-9f3515b0c78f?w=600&q=80";

                    cart.Add(new CartItemModel
                    {
                        VariantId = variant.VariantId,
                        Name = selectedProduct.Name,
                        Size = selectedSize,
                        Color = selectedColor,
                        Price = selectedProduct.BasePrice + (variant.PriceModifier ?? 0.00m),
                        Qty = 1,
                        ImgUrl = primaryImg
                    });
                }

                ShowToast($"Added {selectedProduct.Name} to bag!");
                CloseQuickView();
                isCartOpen = true;
            }
        }

        private void UpdateQty(CartItemModel item, int change)
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

            item.Qty += change;
            if (item.Qty <= 0)
            {
                cart.Remove(item);
            }
        }

        private void RemoveItem(CartItemModel item)
        {
            cart.Remove(item);
            ShowToast("Item removed from bag.");
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
            }
            else
            {
                slipUploaded = false; // Needs upload simulation
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
                    OrderStatus = "pending",
                    PaymentStatus = selectedPayment == "cod" ? "unpaid" : "pending",
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
                    PaymentMethod = selectedPayment.ToUpper(),
                    PaymentStatus = selectedPayment == "cod" ? "pending" : "completed",
                    Amount = total,
                    TransactionId = selectedPayment == "cod" ? "COD" : "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                    CreatedAt = DateTime.Now
                });

                await _db.SaveChangesAsync();

                pointsEarnedInOrder = (int)(total / 100);
                confirmedOrderId = $"ORD-{order.OrderId:D4}";
                isSuccessOpen = true;
            }
            catch (Exception ex)
            {
                ShowToast("Error placing order: " + ex.Message);
            }
        }

        private async Task AfterOrder()
        {
            isSuccessOpen = false;
            cart.Clear();
            selectedPayment = "";
            slipUploaded = false;
            slipFileName = "";
            
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
                var dbUser = await _db.Users.FirstOrDefaultAsync(u => u.UserId == currentUser!.UserId);
                if (dbUser != null)
                {
                    dbUser.FirstName = profFirstName.Trim();
                    dbUser.LastName = profLastName.Trim();
                    dbUser.Email = profEmail.Trim();
                    dbUser.Address = profAddress.Trim();

                    await _db.SaveChangesAsync();
                    
                    // Sync session
                    Session.Login(dbUser);
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
                coPhone = string.IsNullOrWhiteSpace(coPhone) ? profPhone : coPhone;
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

        private void Logout()
        {
            Session.Logout();
            Nav.NavigateTo("/login");
        }

        // Client model for memory cart
        public class CartItemModel
        {
            public int VariantId { get; set; }
            public string Name { get; set; } = "";
            public string Size { get; set; } = "";
            public string Color { get; set; } = "";
            public decimal Price { get; set; }
            public int Qty { get; set; }
            public string ImgUrl { get; set; } = "";
        }

        private async Task BestSeller()
        {
            var result = await httpClientServices.ExecuteAsync<List<BestSellerDto>>("api/order", EnumHttpMethod.Get);
        }
    }
}
