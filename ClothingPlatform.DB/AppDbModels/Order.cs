using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public string ShippingAddress { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<StaffFulfillmentLog> StaffFulfillmentLogs { get; set; } = new List<StaffFulfillmentLog>();

    public virtual ICollection<StaffSalesLog> StaffSalesLogs { get; set; } = new List<StaffSalesLog>();

    public virtual User User { get; set; } = null!;
}
