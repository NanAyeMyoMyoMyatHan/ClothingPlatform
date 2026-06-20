using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Web.Services;
using Microsoft.AspNetCore.Components;

namespace ClothingPlatform.Web.Components.Pages.Staff
{
    public abstract class StaffPortalPageBase : ComponentBase
    {
        [Inject]
        protected StaffPortalState State { get; set; } = default!;

        protected string staffFirstName { get => State.StaffFirstName; set => State.StaffFirstName = value; }
        protected string staffLastName { get => State.StaffLastName; set => State.StaffLastName = value; }
        protected string staffEmail { get => State.StaffEmail; set => State.StaffEmail = value; }
        protected string staffPhone { get => State.StaffPhone; set => State.StaffPhone = value; }
        protected string staffRole => State.StaffRole;
        protected string staffInitials => State.StaffInitials;
        protected bool profileSaved { get => State.ProfileSaved; set => State.ProfileSaved = value; }

        protected int totalOrdersCount => State.TotalOrdersCount;
        protected decimal totalRevenue => State.TotalRevenue;
        protected int totalSkusCount => State.TotalSkusCount;
        protected int lowStockCount => State.LowStockCount;
        protected int pendingCount => State.PendingCount;
        protected int processingCount => State.ProcessingCount;
        protected int confirmCount => State.ConfirmCount;

        protected List<Order> allOrders => State.AllOrders;
        protected List<Order> filteredOrders => State.FilteredOrders;
        protected List<GuestOrder> allGuestOrders => State.AllGuestOrders;
        protected List<GuestOrder> filteredGuestOrders => State.FilteredGuestOrders;
        protected List<ProductVariant> inventoryVariants => State.InventoryVariants;

        protected string ordersTab { get => State.OrdersTab; set => State.OrdersTab = value; }
        protected string currentFilter => State.CurrentFilter;
        protected string regularOrderSearch { get => State.RegularOrderSearch; set => State.RegularOrderSearch = value; }
        protected string guestOrderSearch { get => State.GuestOrderSearch; set => State.GuestOrderSearch = value; }
        protected string guestPaymentFilter { get => State.GuestPaymentFilter; set => State.GuestPaymentFilter = value; }

        protected string guestCustomerName { get => State.GuestCustomerName; set => State.GuestCustomerName = value; }
        protected string guestPhoneNumber { get => State.GuestPhoneNumber; set => State.GuestPhoneNumber = value; }
        protected string newOrderShippingAddress { get => State.NewOrderShippingAddress; set => State.NewOrderShippingAddress = value; }
        protected string guestPaymentMethod { get => State.GuestPaymentMethod; set => State.GuestPaymentMethod = value; }
        protected string guestPaymentStatus { get => State.GuestPaymentStatus; set => State.GuestPaymentStatus = value; }
        protected string productSearch { get => State.ProductSearch; set => State.ProductSearch = value; }
        protected string createOrderError => State.CreateOrderError;
        protected List<StaffPortalState.OrderLineDraft> orderLines => State.OrderLines;
        protected decimal OrderLinesTotal => State.OrderLinesTotal;
        protected int selectedLineCount => State.SelectedLineCount;
        protected int selectedItemCount => State.SelectedItemCount;
        protected bool canSubmitPhoneOrder => State.CanSubmitPhoneOrder;

        protected int orderPage => State.OrderPage;
        protected int OrderTotalPages => State.OrderTotalPages;
        protected List<Order> Pagedorders => State.PagedOrders;

        protected int guestOrderPage => State.GuestOrderPage;
        protected int GuestOrderTotalPages => State.GuestOrderTotalPages;
        protected List<GuestOrder> PagedGuestOrders => State.PagedGuestOrders;

        protected int inventoryPage => State.InventoryPage;
        protected int inventoryPageSize => State.InventoryPageSize;
        protected int InventoryTotalPages => State.InventoryTotalPages;
        protected List<ProductVariant> PagedInventoryVariants => State.PagedInventoryVariants;

        protected int recentOrderPage => State.RecentOrderPage;
        protected int RecentOrderTotalPages => State.RecentOrderTotalPages;
        protected List<Order> PagedRecentOrders => State.PagedRecentOrders;

        protected Task ChangeOrderPage(int newPage) => State.ChangeOrderPage(newPage);
        protected Task ChangeGuestOrderPage(int newPage) => State.ChangeGuestOrderPage(newPage);
        protected Task ChangeInventoryPage(int newPage) => State.ChangeInventoryPage(newPage);
        protected Task ChangeRecentOrderPage(int newPage) => State.ChangeRecentOrderPage(newPage);

        protected void FilterOrders(string status) => State.FilterOrders(status);
        protected Task UpdateOrderStatus(Order order, string? newStatus) => State.UpdateOrderStatus(order, newStatus);
        protected Task UpdateGuestOrderStatus(GuestOrder guestOrder, string? newStatus) => State.UpdateGuestOrderStatus(guestOrder, newStatus);
        protected Task AdjustStock(ProductVariant variant, int adjustment) => State.AdjustStock(variant, adjustment);

        protected decimal GetVariantUnitPrice(int variantId) => State.GetVariantUnitPrice(variantId);
        protected decimal GetLineTotal(StaffPortalState.OrderLineDraft line) => State.GetLineTotal(line);
        protected IEnumerable<ProductVariant> GetSelectableVariants(StaffPortalState.OrderLineDraft line) => State.GetSelectableVariants(line);
        protected void OnLineVariantChanged(StaffPortalState.OrderLineDraft line, string? value) => State.OnLineVariantChanged(line, value);
        protected void OnLineQtyChanged(StaffPortalState.OrderLineDraft line, string? value) => State.OnLineQtyChanged(line, value);
        protected void AdjustLineQuantity(StaffPortalState.OrderLineDraft line, int adjustment) => State.AdjustLineQuantity(line, adjustment);
        protected void AddOrderLine() => State.AddOrderLine();
        protected void RemoveOrderLine(StaffPortalState.OrderLineDraft line) => State.RemoveOrderLine(line);
        protected void ResetCreateOrderForm() => State.ResetCreateOrderForm();
        protected Task SubmitPhoneOrder() => State.SubmitPhoneOrder();
        protected Task SaveProfile() => State.SaveProfile();

        protected static string NormalizeStatus(string? status) => StaffPortalState.NormalizeStatus(status);
        protected static bool IsFinalStatus(string? status) => StaffPortalState.IsFinalStatus(status);
        protected MarkupString StatusBadge(string status) => State.StatusBadge(status);
    }
}
