using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class StaffActivityLog
{
    public int LogId { get; set; }

    public int StaffId { get; set; }

    public string TargetTable { get; set; } = null!;

    public int TargetId { get; set; }

    public string ActionType { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Staff { get; set; } = null!;
}
