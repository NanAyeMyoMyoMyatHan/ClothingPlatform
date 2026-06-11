using ClothingPlatform.DB.AppDbModels;
using ClothingPlatformProject.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace ClothingPlatformProject.Features.Auth
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;

        public AuthService(AppDbContext db)
        {
            _db = db;
        }
        public async Task<AuthResponse> loginuser(LoginRequest loginRequest)
        {
            await Task.Delay(500);
            var user = _db.Users
                .AsNoTracking()
                .FirstOrDefault(x =>
                x.Email == loginRequest.Email &&
                x.PasswordHash == loginRequest.Password); 
            if(user != null)
            {
                return new AuthResponse
                {
                    IsSuccess = true,
                   // Token = "fake-jwt-toke-xyz123",
                    ErrorMessage = null
                };
            }
            return new AuthResponse
            {
                IsSuccess = false,
                ErrorMessage="Invalid email or passowrd."
            };
        }
    }
}
