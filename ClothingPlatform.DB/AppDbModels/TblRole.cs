using System;
using System.Collections.Generic;

namespace ClothingPlatform.DB.AppDbModels;

public partial class TblRole
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<TblRolePermission> TblRolePermissions { get; set; } = new List<TblRolePermission>();

    public virtual ICollection<TblUser> TblUsers { get; set; } = new List<TblUser>();
}
