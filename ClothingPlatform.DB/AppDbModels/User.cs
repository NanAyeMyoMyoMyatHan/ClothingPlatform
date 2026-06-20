using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class User
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public int RoleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<StaffActivityLog> StaffActivityLogs { get; set; } = new List<StaffActivityLog>();

    public virtual ICollection<StaffFulfillmentLog> StaffFulfillmentLogs { get; set; } = new List<StaffFulfillmentLog>();

    public virtual ICollection<StaffSalesDaily> StaffSalesDailies { get; set; } = new List<StaffSalesDaily>();

    public virtual ICollection<StaffSalesLog> StaffSalesLogs { get; set; } = new List<StaffSalesLog>();

    public virtual ICollection<StaffSalesMonthly> StaffSalesMonthlies { get; set; } = new List<StaffSalesMonthly>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}
