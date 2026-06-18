using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class TblRolePermission
{
    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual TblPermission Permission { get; set; } = null!;

    public virtual TblRole Role { get; set; } = null!;
}
