using ClothingPlatform.DB.AppDbModels;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatform.Api.Features.Permission
{
    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _db;

        public PermissionService(AppDbContext db)
        {
            _db = db;
        }

        // ── Returns all roles except "admin" ──────────────────────────────────────
        public async Task<List<RoleDto>> GetManageableRolesAsync()
        {
            return await _db.Roles
                .Where(r => r.RoleName != "admin")
                .OrderBy(r => r.RoleName)
                .Select(r => new RoleDto
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Description = r.Description
                })
                .AsNoTracking()
                .ToListAsync();
        }

        // ── Full permission × role matrix ─────────────────────────────────────────
        public async Task<PermissionMatrixDto> GetPermissionMatrixAsync()
        {
            var roles = await GetManageableRolesAsync();

            var permissions = await _db.Permissions
                .OrderBy(p => p.PermissionName)
                .Select(p => new PermissionDto
                {
                    PermissionId = p.PermissionId,
                    PermissionName = p.PermissionName,
                    Description = p.Description
                })
                .AsNoTracking()
                .ToListAsync();

            // Load all existing grants for the manageable roles
            var roleIds = roles.Select(r => r.RoleId).ToList();
            var grantedPairs = await _db.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => new { rp.RoleId, rp.PermissionId })
                .AsNoTracking()
                .ToListAsync();

            var grantSet = grantedPairs
                .Select(g => (g.PermissionId, g.RoleId))
                .ToHashSet();

            // Build flat grant list
            var grants = new List<GrantEntryDto>();
            foreach (var perm in permissions)
            {
                foreach (var role in roles)
                {
                    grants.Add(new GrantEntryDto
                    {
                        PermissionId = perm.PermissionId,
                        RoleId = role.RoleId,
                        Granted = grantSet.Contains((perm.PermissionId, role.RoleId))
                    });
                }
            }

            return new PermissionMatrixDto
            {
                Roles = roles,
                Permissions = permissions,
                Grants = grants
            };
        }

        // ── Atomically replace a role's permission set ────────────────────────────
        public async Task<bool> UpdateRolePermissionsAsync(int roleId, List<int> permissionIds)
        {
            // Validate the role exists and is not admin
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role == null)
            {
                return false;
            }

            if (role.RoleName == "admin")
            {
                // Admin permissions are not managed through this page
                return false;
            }

            // Validate all supplied permission IDs actually exist
            var validPermIds = await _db.Permissions
                .Where(p => permissionIds.Contains(p.PermissionId))
                .Select(p => p.PermissionId)
                .ToListAsync();

            // Remove all existing grants for this role
            var existing = await _db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            _db.RolePermissions.RemoveRange(existing);

            // Add the new grants
            var now = DateTime.Now;
            foreach (var permId in validPermIds)
            {
                _db.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permId,
                    CreatedAt = now
                });
            }

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
