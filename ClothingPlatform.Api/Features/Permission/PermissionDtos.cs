namespace ClothingPlatform.Api.Features.Permission
{
    /// <summary>A single permission definition.</summary>
    public class PermissionDto
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>A role that can be managed (admin role excluded).</summary>
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Full permission × role matrix returned to the UI.
    /// Grants[permissionIndex][roleIndex] = true means the role has that permission.
    /// </summary>
    public class PermissionMatrixDto
    {
        public List<RoleDto> Roles { get; set; } = new();
        public List<PermissionDto> Permissions { get; set; } = new();

        /// <summary>
        /// Keyed by (PermissionId, RoleId) → granted.
        /// Serialized as flat list for easy JS/Blazor consumption.
        /// </summary>
        public List<GrantEntryDto> Grants { get; set; } = new();
    }

    /// <summary>Single grant state entry.</summary>
    public class GrantEntryDto
    {
        public int PermissionId { get; set; }
        public int RoleId { get; set; }
        public bool Granted { get; set; }
    }

    /// <summary>Request body when Admin saves a role's permission set.</summary>
    public class UpdateRolePermissionsRequest
    {
        /// <summary>The complete list of permissionIds that should be granted to this role.
        /// Permissions NOT in this list will be revoked.</summary>
        public List<int> PermissionIds { get; set; } = new();
    }
}
