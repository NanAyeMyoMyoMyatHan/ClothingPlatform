
using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Api.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClothingPlatform.Api.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }
        public async Task<AuthResponse?> LoginAsync(AuthRequest request)
        {
            await EnsurePortalSeedDataAsync();

            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(x => x.Email.ToLower() == email);

            if (user == null || !PasswordMatches(request.Password, user.PasswordHash.Trim()))
            {
                return null;
            }

            var roleName = user.Role?.RoleName ?? string.Empty;

            if (!user.PasswordHash.Trim().StartsWith("$2", StringComparison.Ordinal))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                await _db.SaveChangesAsync();
            }

            var permissions = await (from rp in _db.RolePermissions
                                     join p in _db.Permissions on rp.PermissionId equals p.PermissionId
                                     where rp.RoleId == user.RoleId
                                     select p.PermissionName).ToListAsync();

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, roleName),
    };

            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])),
                signingCredentials: credentials);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthResponse
            {
                AccessToken = jwt,
                Role = roleName,
                Permissions = permissions
            };
        }

        private async Task EnsurePortalSeedDataAsync()
        {
            // Clean up old PascalCase permissions if they exist
            var oldPermissionNames = new List<string> { "Dashboard.View", "Users.Manage", "Products.Manage", "Orders.Manage", "Customers.View", "Reports.Generate", "Settings.Manage", "Permissions.Manage", "Logs.View", "Staff.Manage" };
            var oldPerms = await _db.Permissions.Where(p => oldPermissionNames.Contains(p.PermissionName)).ToListAsync();
            if (oldPerms.Any())
            {
                var oldPermIds = oldPerms.Select(p => p.PermissionId).ToList();
                var oldRolePerms = await _db.RolePermissions.Where(rp => oldPermIds.Contains(rp.PermissionId)).ToListAsync();
                _db.RolePermissions.RemoveRange(oldRolePerms);
                _db.Permissions.RemoveRange(oldPerms);
                await _db.SaveChangesAsync();
            }

            var adminRole = await EnsureRoleAsync("admin", "Full administrator access");
            var staffRole = await EnsureRoleAsync("staff", "Staff operations access");
            await EnsureRoleAsync("customer", "Customer shopping account");

            var reportPermission      = await EnsurePermissionAsync("reports.generate",     "Generate and export admin reports");
            var productsPermission    = await EnsurePermissionAsync("products.manage",      "Create, update, and delete catalog records");
            var staffPermission       = await EnsurePermissionAsync("staff.manage",         "View and manage staff accounts");
            var customersPermission   = await EnsurePermissionAsync("customers.view",       "View customer records inside the shared portal");
            var ordersCreatePermission = await EnsurePermissionAsync("orders.create",        "Can place new orders");
            var ordersViewPermission   = await EnsurePermissionAsync("orders.view",          "Can view order history and lists");
            var ordersUpdatePermission = await EnsurePermissionAsync("orders.update",        "Can update order details and status");
            var ordersDeletePermission = await EnsurePermissionAsync("orders.delete",        "Can delete orders");
            var dashboardPermission   = await EnsurePermissionAsync("dashboard.view",       "Access the staff dashboard");
            var permsPermission       = await EnsurePermissionAsync("permissions.manage",   "Manage role permissions");


            // Admin gets all permissions
            await EnsureRolePermissionAsync(adminRole.RoleId, reportPermission.PermissionId);
            await EnsureRolePermissionAsync(adminRole.RoleId, productsPermission.PermissionId);
            await EnsureRolePermissionAsync(adminRole.RoleId, staffPermission.PermissionId);
            await EnsureRolePermissionAsync(adminRole.RoleId, customersPermission.PermissionId);
            await EnsureRolePermissionAsync(adminRole.RoleId, ordersCreatePermission.PermissionId);
            await EnsureRolePermissionAsync(adminRole.RoleId, ordersViewPermission.PermissionId);
            await EnsureRolePermissionAsync(adminRole.RoleId, ordersUpdatePermission.PermissionId);
            await EnsureRolePermissionAsync(adminRole.RoleId, ordersDeletePermission.PermissionId);
            await EnsureRolePermissionAsync(adminRole.RoleId, dashboardPermission.PermissionId);
            await EnsureRolePermissionAsync(adminRole.RoleId, permsPermission.PermissionId);

            // Staff gets operational permissions
            await EnsureRolePermissionAsync(staffRole.RoleId, ordersCreatePermission.PermissionId);
            await EnsureRolePermissionAsync(staffRole.RoleId, ordersViewPermission.PermissionId);
            await EnsureRolePermissionAsync(staffRole.RoleId, ordersUpdatePermission.PermissionId);
            await EnsureRolePermissionAsync(staffRole.RoleId, ordersDeletePermission.PermissionId);
            await EnsureRolePermissionAsync(staffRole.RoleId, dashboardPermission.PermissionId);
            await EnsureRolePermissionAsync(staffRole.RoleId, productsPermission.PermissionId);

            await EnsureSeedUserAsync("Admin", "User", "admin@boutique.com", "admin123", "09252522525", "Chic Boutique HQ", adminRole.RoleId);
            await EnsureSeedUserAsync("Thiri", "San", "staff@boutique.com", "staff123", "09222333444", "No. 456, Boutique Rd, Yangon", staffRole.RoleId);

            await _db.SaveChangesAsync();
        }

        private async Task<Role> EnsureRoleAsync(string roleName, string description)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role != null) return role;

            role = new Role
            {
                RoleName = roleName,
                Description = description,
                CreatedAt = DateTime.Now
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
            return role;
        }

        private async Task<ClothingPlatform.DB.AppDbModels.Permission> EnsurePermissionAsync(string permissionName, string description)
        {
            var permission = await _db.Permissions.FirstOrDefaultAsync(p => p.PermissionName == permissionName);
            if (permission != null) return permission;

            permission = new ClothingPlatform.DB.AppDbModels.Permission
            {
                PermissionName = permissionName,
                Description = description,
                CreatedAt = DateTime.Now
            };
            _db.Permissions.Add(permission);
            await _db.SaveChangesAsync();
            return permission;
        }

        private async Task EnsureRolePermissionAsync(int roleId, int permissionId)
        {
            if (await _db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId))
            {
                return;
            }

            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                CreatedAt = DateTime.Now
            });
        }

        private async Task EnsureSeedUserAsync(
            string firstName,
            string lastName,
            string email,
            string password,
            string phone,
            string address,
            int roleId)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null)
            {
                _db.Users.Add(new ClothingPlatform.DB.AppDbModels.User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = normalizedEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    PhoneNumber = phone,
                    Address = address,
                    RoleId = roleId,
                    CreatedAt = DateTime.Now
                });
                return;
            }

            if (user.RoleId != roleId)
            {
                user.RoleId = roleId;
            }
        }

        private static bool PasswordMatches(string password, string storedHash)
        {
            if (storedHash.StartsWith("$2", StringComparison.Ordinal))
            {
                return BCrypt.Net.BCrypt.Verify(password, storedHash);
            }

            return string.Equals(storedHash, password, StringComparison.Ordinal);
        }

    }
}
