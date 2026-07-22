namespace ClothingPlatform.Api.Features.Permission
{
    public interface IPermissionService
    {
        /// <summary>Returns all manageable roles (admin excluded).</summary>
        Task<List<RoleDto>> GetManageableRolesAsync();

        /// <summary>Returns the full permission × role matrix.</summary>
        Task<PermissionMatrixDto> GetPermissionMatrixAsync();

        /// <summary>
        /// Atomically replaces the permission set for the given role.
        /// Any permissionId NOT in <paramref name="permissionIds"/> will be revoked.
        /// </summary>
        Task<bool> UpdateRolePermissionsAsync(int roleId, List<int> permissionIds);
    }
}
