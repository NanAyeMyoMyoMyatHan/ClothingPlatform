using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Api.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatform.Api.Features.Auth
{
    public class CustomerAuthService : ICustomerAuthService
    {
        private readonly AppDbContext _db;

        public CustomerAuthService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<CustomerAuthResponse?> LoginAsync(CustomerLoginRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null) return null;

            if (!string.Equals(user.Role?.RoleName, "customer", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Admin and staff accounts must use the shared portal login page.");
            }

            var stored = user.PasswordHash?.Trim() ?? string.Empty;
            var matches = stored.StartsWith("$2", StringComparison.Ordinal)
                ? BCrypt.Net.BCrypt.Verify(request.Password, stored)
                : string.Equals(stored, request.Password, StringComparison.Ordinal);

            if (!matches) return null;

            if (!stored.StartsWith("$2", StringComparison.Ordinal))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                await _db.SaveChangesAsync();
            }

            return ToResponse(user);
        }

        public async Task<CustomerAuthResponse> RegisterAsync(CustomerRegisterRequest request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            var customerRole = await EnsureCustomerRoleAsync();
            var user = new ClothingPlatform.DB.AppDbModels.User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber.Trim(),
                Address = request.Address.Trim(),
                RoleId = customerRole.RoleId,
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            user.Role = customerRole;

            return ToResponse(user);
        }

        private async Task<Role> EnsureCustomerRoleAsync()
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "customer");
            if (role != null) return role;

            role = new Role
            {
                RoleName = "customer",
                Description = "Customer shopping account",
                CreatedAt = DateTime.Now
            };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
            return role;
        }

        private static CustomerAuthResponse ToResponse(ClothingPlatform.DB.AppDbModels.User user)
        {
            return new CustomerAuthResponse
            {
                UserId = user.UserId,
                RoleId = user.RoleId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Role = user.Role?.RoleName ?? "customer"
            };
        }
    }
}
