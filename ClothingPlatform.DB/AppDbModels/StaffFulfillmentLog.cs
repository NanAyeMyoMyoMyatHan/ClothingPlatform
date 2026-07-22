using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class StaffFulfillmentLog
{
    public int LogId { get; set; }

    public int OrderId { get; set; }

    public int StaffId { get; set; }

    public string ActionTaken { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime? ActionAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual User Staff { get; set; } = null!;
}
