using System;

namespace ClothingPlatform.DB.AppDbModels;

public partial class ContactMessage
{
    public int ContactMessageId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
