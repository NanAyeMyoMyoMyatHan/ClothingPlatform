using ClothingPlatform.DB.AppDbModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClothingPlatform.Api.Filters
{
    public class PermissionAttribute : TypeFilterAttribute
    {
        public PermissionAttribute(string permission,bool checkFromDb= false) : base(typeof(PermissionFilter))
        {
            Arguments = new object[] { permission, checkFromDb };
        }
       
    }
    public class PermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permission;
        private readonly bool _checkFromDb;
        private readonly AppDbContext _context;

        public PermissionFilter(string permission,bool checkFromDb, AppDbContext context)
        {
            _permission = permission;
            _checkFromDb = checkFromDb;
            _context = context;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userPrincipal = context.HttpContext.User;
            if (userPrincipal?.Identity == null || !userPrincipal.Identity.IsAuthenticated)
            {
                context.Result = new ForbidResult();
                return;
            }

            // 1. Check if user is admin (admins have full permission access)
            var role = userPrincipal.FindFirst(c =>
                c.Type == ClaimTypes.Role ||
                string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase) ||
                c.Type.EndsWith("/role", StringComparison.OrdinalIgnoreCase))?.Value;

            if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) || userPrincipal.IsInRole("admin"))
            {
                return;
            }

            // 2. Check token claims directly for the required permission
            bool hasTokenPermission = userPrincipal.Claims.Any(c =>
                (string.Equals(c.Type, "permission", StringComparison.OrdinalIgnoreCase) ||
                 c.Type.EndsWith("/permission", StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(c.Value, _permission, StringComparison.OrdinalIgnoreCase));

            if (hasTokenPermission && !_checkFromDb)
            {
                return;
            }

            // 3. Check DB permissions
            var userIdString = userPrincipal.FindFirst(c =>
                c.Type == ClaimTypes.NameIdentifier ||
                string.Equals(c.Type, "nameid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Type, "sub", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.Type, "userId", StringComparison.OrdinalIgnoreCase) ||
                c.Type.EndsWith("/nameidentifier", StringComparison.OrdinalIgnoreCase))?.Value;

            if (int.TryParse(userIdString, out int userId))
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user != null)
                {
                    if (string.Equals(user.Role?.RoleName, "admin", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    var hasDbPermission = await (from rp in _context.RolePermissions
                                           join p in _context.Permissions on rp.PermissionId equals p.PermissionId
                                           where rp.RoleId == user.RoleId &&
                                                 p.PermissionName.ToLower() == _permission.ToLower()
                                           select p.PermissionId).AnyAsync();

                    if (hasDbPermission)
                    {
                        return;
                    }
                }
            }

            // Fallback: If DB check didn't pass or userId wasn't in DB, allow if token has permission claim
            if (hasTokenPermission)
            {
                return;
            }

            context.Result = new ForbidResult();
        }
    }
}
