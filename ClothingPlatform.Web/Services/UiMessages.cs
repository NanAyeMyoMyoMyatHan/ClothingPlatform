namespace ClothingPlatform.Web.Services
{
    internal static class UiMessages
    {
        internal static class Admin
        {
            public static string PreparePortalFailed(string detail) => $"Shared portal could not finish startup. {detail}";
            public static string LoadDataFailed(string detail) => $"Shared portal data could not be loaded. {detail}";
            public const string OrderStatusForwardOnly = "Admin order status can only move forward in the fulfillment flow.";
            public static string OrderStatusUpdated(int orderId, string status) => $"Admin order #{orderId} moved to {status}.";
            public static string OrderStatusUpdateFailed(string detail) => $"Admin order status update failed. {detail}";
            public static string DeleteOrderConfirm(int orderId) => $"Delete customer order ORD-{orderId:D4} and send that customer an immediate notice?";
            public static string OrderDeleted(int orderId) => $"Order ORD-{orderId:D4} was removed and the customer notification was sent.";
            public static string OrderDeleteFailed(string detail) => $"Admin order deletion failed. {detail}";
            public const string CreateProductCategoryRequired = "Choose a category before creating this product.";
            public const string CreateProductRequiredFields = "New product needs a name and a base price above zero.";
            public static string ProductCreated(string name) => $"New product '{name}' was added to the catalog.";
            public const string ProductCreateFailed = "Product creation did not complete.";
            public static string ProductCreateError(string detail) => $"Product creation error: {detail}";
            public static string ProductImageSelected(string name) => $"Product image file selected: {name}";
            public const string UpdateProductIdMissing = "Product update cannot continue because the edit form is missing its product ID.";
            public const string UpdateProductCategoryRequired = "Choose a category before saving product changes.";
            public const string UpdateProductRequiredFields = "Product update needs a name and a base price above zero.";
            public static string ProductUpdated(string name) => $"Product '{name}' was updated successfully.";
            public const string ProductUpdateFailed = "Product update did not complete.";
            public static string ProductUpdateError(string detail) => $"Product update error: {detail}";
            public const string DeleteProductConfirm = "Delete this product from the catalog permanently?";
            public const string ProductDeleteFailed = "Product deletion did not complete.";
            public static string ProductDeleteError(string detail) => $"Product deletion error: {detail}";
            public const string DailyReportAggregated = "Daily admin sales summary was recalculated.";
            public static string DailyReportFailed(string detail) => $"Daily admin summary calculation failed. {detail}";
            public const string StaffFirstNameRequired = "Enter the staff member's first name.";
            public const string StaffLastNameRequired = "Enter the staff member's last name.";
            public const string StaffEmailRequired = "Enter the staff member's email address.";
            public const string StaffPasswordRequired = "Staff password must contain at least 6 characters.";
            public const string StaffEmailDuplicate = "Another account already uses this staff email address.";
            public static string StaffCreated(string firstName, string lastName) => $"Staff account for {firstName} {lastName} was created.";
            public static string StaffCreateFailed(string detail) => $"Staff account creation failed. {detail}";
            public const string DeleteStaffConfirm = "Delete this staff account and remove its portal access permanently?";
            public const string StaffNotFound = "The selected staff account was not found.";
            public static string StaffDeleted(string firstName, string lastName) => $"Staff account for {firstName} {lastName} was removed.";
            public static string StaffDeleteFailed(string detail) => $"Staff account deletion failed. {detail}";
            public static string ReportLoadFailed(string detail) => $"Admin report could not be loaded. {detail}";
            public static string ReportDownloadFailed(string detail) => $"Admin CSV report download failed. {detail}";
            public const string ProfileFirstNameRequired = "Enter your first name before saving your profile.";
            public const string ProfileLastNameRequired = "Enter your last name before saving your profile.";
            public const string ProfileEmailRequired = "Enter a valid email address before saving your profile.";
            public const string ProfileEmailDuplicate = "Another account already uses that email address.";
            public const string ProfileSaved = "Your shared portal profile was updated.";
            public static string ProfileSaveFailed(string detail) => $"Profile update failed. {detail}";
        }

        internal static class PortalLogin
        {
            public const string LoginEmailRequired = "Portal email address is required.";
            public const string LoginEmailInvalid = "Enter a valid portal email address.";
            public const string LoginPasswordRequired = "Portal password is required.";
            public const string StaffOnly = "This sign-in page is only for admin and staff accounts.";
            public const string UserLoadFailed = "Portal user details could not be loaded from the database.";
            public const string InvalidCredentials = "Portal email or password is incorrect.";
            public const string CustomerMustUseCustomerLogin = "Customer accounts must use the Customer Login page.";
            public static string LoginFailed(string detail) => $"Portal login error: {detail}";
            public const string RegisterEmailRequired = "Membership email address is required.";
            public const string RegisterEmailInvalid = "Enter a valid membership email address.";
            public const string RegisterEmailDuplicate = "A boutique membership already uses this email.";
            public const string RegisterAddressRequired = "Membership address is required.";
            public const string RegisterPasswordRequired = "Membership password is required.";
            public const string RegisterPasswordLength = "Membership password must be at least 8 characters.";
            public const string RegisterConfirmPasswordRequired = "Repeat the membership password to continue.";
            public const string RegisterPasswordsMismatch = "Membership password fields do not match.";
            public const string RegisterUnavailable = "Membership registration is currently unavailable. Please try again later.";
            public static string RegisterSuccess(string firstName) => $"Welcome to The Boutique, <strong>{firstName}</strong>! Your membership has been created. Please sign in.";
            public const string ToastTitle = "Portal Authentication Complete";
            public const string ToastText = "Opening the shared portal.";
        }

        internal static class CustomerAuth
        {
            public const string LoginRequired = "Enter your customer email and password.";
            public const string InvalidCredentials = "Customer email or password is incorrect.";
            public const string RegisterRequiredFields = "Complete all required customer registration fields.";
            public static string LoginFailed(string detail) => $"Customer login could not be completed. {detail}";
            public static string RegisterFailed(string detail) => $"Customer registration could not be completed. {detail}";
        }

        internal static class CustomerShop
        {
            public static string CatalogLoadFailed(string detail) => $"Customer catalog could not be loaded. {detail}";
            public const string AddToBagSignInConfirm = "Sign in to your customer account before adding this item to your bag?";
            public const string SelectSizeAndColor = "Choose both a size and a color for this item.";
            public const string VariantUnavailable = "That size and color combination is unavailable.";
            public const string VariantOutOfStock = "This selected variant is out of stock.";
            public const string ModalStockExceeded = "Selected quantity is higher than this variant's stock.";
            public static string AddedToBag(string productName) => $"Added {productName} to your shopping bag.";
            public const string CartStockExceeded = "Cart quantity cannot exceed current stock.";
            public const string CartItemRemoved = "Item removed from your shopping bag.";
            public const string CheckoutBagEmpty = "Add at least one item before checkout.";
            public const string PaymentSlipUploaded = "Payment screenshot was attached to checkout.";
            public const string PaymentSlipInvalidFormat = "Payment screenshot must be a JPG, PNG, or WEBP image.";
            public const string PaymentSlipTooLarge = "Payment screenshot must be 5 MB or smaller.";
            public static string PaymentSlipReadFailed(string detail) => $"Payment screenshot could not be read. {detail}";
            public const string PlaceOrderConfirm = "Submit this checkout order for boutique processing?";
            public const string DeliveryDetailsRequired = "Enter all delivery details before submitting the order.";
            public const string PaymentMethodRequired = "Select a payment method for this order.";
            public const string PaymentSlipRequired = "Upload a payment screenshot for this method.";
            public const string PaymentReferenceRequired = "Enter the payment transaction or reference number.";
            public const string SubmitBagEmpty = "Your bag is empty, so no order can be submitted.";
            public const string CheckoutSignInRequired = "Sign in before submitting this order.";
            public static string CartClearAfterOrderFailed(string detail) => $"Order was placed, but your shopping bag could not be cleared automatically. {detail}";
            public static string PlaceOrderFailed(string detail) => $"Checkout order could not be placed. {detail}";
            public const string ProfileDetailsRequired = "Complete all customer profile fields before saving.";
            public const string ProfileUpdated = "Customer profile changes were saved.";
            public static string ProfileUpdateFailed(string detail) => $"Customer profile update failed. {detail}";
        }

        internal static class StaffPortal
        {
            public static string PrepareFailed(string detail) => $"Staff portal could not finish startup. {detail}";
            public static string DashboardLoadFailed(string detail) => $"Staff dashboard data could not be loaded. {detail}";
            public const string RegularOrderForwardOnly = "Regular order status can only advance to the next fulfillment stage.";
            public const string GuestOrderForwardOnly = "Phone order status can only advance to the next fulfillment stage.";
            public static string RegularOrderUpdated(int orderId, string status) => $"Regular order #ORD-{orderId:D4} advanced to <strong>{status}</strong>.";
            public static string RegularOrderUpdateFailed(string detail) => $"Regular order status update failed. {detail}";
            public static string GuestOrderUpdated(int orderId, string status) => $"Phone order #GORD-{orderId:D4} advanced to <strong>{status}</strong>.";
            public static string GuestOrderUpdateFailed(string detail) => $"Phone order status update failed. {detail}";
            public static string StockAdjusted(string sku) => $"Inventory stock adjusted for SKU <strong>{sku}</strong>.";
            public static string StockAdjustFailed(string detail) => $"Inventory stock adjustment failed. {detail}";
            public const string PhoneOrderCustomerRequired = "Enter the phone customer's name and phone number.";
            public const string PhoneOrderAddressRequired = "Enter the delivery address for this phone order.";
            public const string PhoneOrderItemRequired = "Add at least one product line to this phone order.";
            public static string PhoneOrderCreated(string customerName) => $"Phone order created for <strong>{customerName}</strong>.";
            public const string PhoneOrderSaveFailed = "Phone order could not be saved. Check stock and item selections.";
            public static string PhoneOrderSubmitFailed(string detail) => $"Phone order submission failed. {detail}";
            public const string ProfileSavedToast = "Staff profile saved from the portal.";
            public const string ProfileSavedInline = "Profile form changes are now saved.";
            public static string ProfileSaveFailed(string detail) => $"Staff profile save failed. {detail}";
        }

        internal static class StaffLogout
        {
            public const string Title = "Leave Shared Portal?";
            public const string Text = "End this shared portal session and return to sign-in?";
            public const string ConfirmButton = "Sign Out";
        }

        internal static class CustomerLogout
        {
            public const string Title = "Leave Customer Account?";
            public const string Text = "Sign out of your customer account on this device?";
            public const string ConfirmButton = "Sign Out Customer";
        }
    }
}
