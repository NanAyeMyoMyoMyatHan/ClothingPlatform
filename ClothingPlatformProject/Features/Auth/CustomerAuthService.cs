using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatformProject.Features.Auth
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
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Role == "customer");
            if (user == null) return null;

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

            var user = new ClothingPlatform.DB.AppDbModels.User
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber.Trim(),
                Address = request.Address.Trim(),
                Role = "customer",
                CreatedAt = DateTime.Now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return ToResponse(user);
        }

        private static CustomerAuthResponse ToResponse(ClothingPlatform.DB.AppDbModels.User user)
        {
            return new CustomerAuthResponse
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Role = user.Role
            };
        }
    }
}
