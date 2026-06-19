using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ClothingPlatformProject.Features.Auth
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
        public async Task<AuthResponse> LoginAsync(AuthRequest request)
        {
            await EnsurePortalSeedDataAsync();

            // ၁။ 🔍 Email တစ်ခုတည်းနဲ့ အရင်ဆုံး User ကို ရှာပါ (Password ကို ဒီမှာ တွဲမစစ်ပါနဲ့တော့)
            var user = await _db.TblUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == request.Email);

            // ၂။ 🔐 [အဓိက အပြောင်းအလဲ] User မရှိရင်ဖြစ်ဖြစ်၊ ရှိပြီး Password မကိုက်ညီရင်ဖြစ်ဖြစ် Login ပယ်ချပါမယ်
            if (user == null || !PasswordMatches(request.Password, user.PasswordHash.Trim()))
            {
                return null; // Invalid Email သို့မဟုတ် Password
            }

            // --- အောက်က ကုဒ်တွေက မိတ်ဆွေရေးထားတဲ့အတိုင်း Token ထုတ်တဲ့အပိုင်း ပုံမှန်အတိုင်း ဆက်လုပ်ပါမယ် ---
            var role = await _db.TblRoles.FirstOrDefaultAsync(r => r.RoleId == user.RoleId);
            var roleName = role?.RoleName ?? string.Empty;
            if (!string.Equals(roleName, "admin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(roleName, "staff", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var permissions = await (from rp in _db.TblRolePermissions
                                     join p in _db.TblPermissions on rp.PermissionId equals p.PermissionId
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
            var adminRole = await EnsureRoleAsync("admin", "Full administrator access");
            var staffRole = await EnsureRoleAsync("staff", "Staff operations access");

            var reportPermission = await EnsurePermissionAsync("Reports.Generate", "Generate and export admin reports");

            if (!await _db.TblRolePermissions.AnyAsync(rp => rp.RoleId == adminRole.RoleId && rp.PermissionId == reportPermission.PermissionId))
            {
                _db.TblRolePermissions.Add(new TblRolePermission
                {
                    RoleId = adminRole.RoleId,
                    PermissionId = reportPermission.PermissionId,
                    CreatedAt = DateTime.Now
                });
            }

            if (!await _db.TblUsers.AnyAsync(u => u.Email == "admin@boutique.com"))
            {
                _db.TblUsers.Add(new TblUser
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Email = "admin@boutique.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    PhoneNumber = "09252522525",
                    Address = "Chic Boutique HQ",
                    RoleId = adminRole.RoleId,
                    CreatedAt = DateTime.Now
                });
            }

            if (!await _db.TblUsers.AnyAsync(u => u.Email == "staff@boutique.com"))
            {
                _db.TblUsers.Add(new TblUser
                {
                    FirstName = "Thiri",
                    LastName = "San",
                    Email = "staff@boutique.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("staff123"),
                    PhoneNumber = "09222333444",
                    Address = "No. 456, Atelier Rd, Yangon",
                    RoleId = staffRole.RoleId,
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();
        }

        private async Task<TblRole> EnsureRoleAsync(string roleName, string description)
        {
            var role = await _db.TblRoles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role != null) return role;

            role = new TblRole
            {
                RoleName = roleName,
                Description = description,
                CreatedAt = DateTime.Now
            };
            _db.TblRoles.Add(role);
            await _db.SaveChangesAsync();
            return role;
        }

        private async Task<TblPermission> EnsurePermissionAsync(string permissionName, string description)
        {
            var permission = await _db.TblPermissions.FirstOrDefaultAsync(p => p.PermissionName == permissionName);
            if (permission != null) return permission;

            permission = new TblPermission
            {
                PermissionName = permissionName,
                Description = description,
                CreatedAt = DateTime.Now
            };
            _db.TblPermissions.Add(permission);
            await _db.SaveChangesAsync();
            return permission;
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
