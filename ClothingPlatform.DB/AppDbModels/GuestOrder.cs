using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class GuestOrder
{
    public int GuestOrderId { get; set; }

    public string CustomerName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string ShippingAddress { get; set; } = null!;

    public int TotalQuantity { get; set; }

    public decimal TotalAmount { get; set; }

    public string OrderStatus { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<GuestOrderItem> GuestOrderItems { get; set; } = new List<GuestOrderItem>();
}
