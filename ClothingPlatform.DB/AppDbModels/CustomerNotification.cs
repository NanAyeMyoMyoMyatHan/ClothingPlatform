using System;

namespace ClothingPlatform.DB.AppDbModels;

public partial class CustomerNotification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public int? OrderId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
